using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.patient;
using carestream.core.dtos.visit;
using carestream.core.dtos.checkin;
using carestream.web.Controllers;

namespace carestream.tests.unit.controllers
{
    public class SickReportControllerTests
    {
        private readonly Mock<IPatientService> _mockPatientService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<SickReportController>> _mockLogger;
        private readonly SickReportController _controller;

        public SickReportControllerTests()
        {
            _mockPatientService = new Mock<IPatientService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<SickReportController>>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", "test-admin-sub-id"), // Default "sub" for performing user
                new Claim(ClaimTypes.Role, "PatientAdmin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };

            _controller = new SickReportController(
                _mockPatientService.Object,
                _mockUserRepository.Object,
                _mockLogger.Object
            )
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        [Fact]
        public void Index_ShouldReturnPartialView()
        {
            // Act
            var result = _controller.Index();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Null(partialViewResult.ViewName); // Expects Index.cshtml by convention
        }

        [Fact]
        public async Task LookupPatient_WithValidForceNumber_PatientFound_NoActiveVisit_ShouldReturnIndexPartialWithPatientData()
        {
            // Arrange
            string forceNumber = "P123";
            var patientDetail = new PatientDetailDto { PatientId = 1, ForceNumber = forceNumber, FirstName = "John" };
            _mockPatientService.Setup(s => s.GetPatientByForceNumberAsync(forceNumber)).ReturnsAsync(patientDetail);
            _mockPatientService.Setup(s => s.GetActiveVisitForPatientAsync(patientDetail.PatientId)).ReturnsAsync((ActiveVisitDto?)null);

            // Act
            var result = await _controller.LookupPatient(forceNumber);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_PatientFound", partialViewResult.ViewData["LookupResultPartial"] as string);
            Assert.Same(patientDetail, partialViewResult.ViewData["PatientData"]);
            Assert.Null(partialViewResult.ViewData["ActiveVisitData"]);
        }

        [Fact]
        public async Task LookupPatient_WithValidForceNumber_PatientFound_WithActiveVisit_ShouldReturnIndexPartialWithAllData()
        {
            // Arrange
            string forceNumber = "S456";
            var patientDetail = new PatientDetailDto { PatientId = 2, ForceNumber = forceNumber, FirstName = "Sarah" };
            var activeVisit = new ActiveVisitDto { VisitId = 10, Status = "In Treatment" };
            _mockPatientService.Setup(s => s.GetPatientByForceNumberAsync(forceNumber)).ReturnsAsync(patientDetail);
            _mockPatientService.Setup(s => s.GetActiveVisitForPatientAsync(patientDetail.PatientId)).ReturnsAsync(activeVisit);

            // Act
            var result = await _controller.LookupPatient(forceNumber);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_PatientFound", partialViewResult.ViewData["LookupResultPartial"] as string);
            Assert.Same(patientDetail, partialViewResult.ViewData["PatientData"]);
            Assert.Same(activeVisit, partialViewResult.ViewData["ActiveVisitData"]);
        }


        [Fact]
        public async Task LookupPatient_PatientNotFound_ShouldReturnIndexPartialWithNotFoundData()
        {
            // Arrange
            string forceNumber = "X999";
            _mockPatientService.Setup(s => s.GetPatientByForceNumberAsync(forceNumber)).ReturnsAsync((PatientDetailDto?)null);

            // Act
            var result = await _controller.LookupPatient(forceNumber);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_PatientNotFound", partialViewResult.ViewData["LookupResultPartial"] as string);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LookupPatient_WithInvalidForceNumber_ShouldReturnIndexPartialWithNotFoundData(string? invalidForceNumber)
        {
            // Act
            var result = await _controller.LookupPatient(invalidForceNumber!);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_PatientNotFound", partialViewResult.ViewData["LookupResultPartial"] as string);
            _mockPatientService.Verify(s => s.GetPatientByForceNumberAsync(It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task CreateNewVisitAndCheckin_UserLinked_ServiceSucceeds_ShouldReturnConfirmation()
        {
            // Arrange
            int patientId = 1;
            int performingUserId = 10;
            var expectedConfirmation = new CheckinConfirmationDto { Success = true, PatientName = "Test Patient" };

            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-admin-sub-id", null, null)).ReturnsAsync(performingUserId);
            _mockPatientService.Setup(s => s.CreateNewVisitAndCheckinAsync(patientId, performingUserId, "", "")).ReturnsAsync(expectedConfirmation);

            // Act
            var result = await _controller.CreateNewVisitAndCheckin(new CheckinInputDto
            {
                PatientId = patientId, 
                AdditionalNotes = "",
                BriefReason = "" 
            });

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_CheckinConfirmation", partialViewResult.ViewData["LookupResultPartial"]);
            Assert.Same(expectedConfirmation, partialViewResult.ViewData["ConfirmationData"]);
        }

        [Fact]
        public async Task CreateNewVisitAndCheckin_UserNotLinked_ShouldReturnErrorConfirmation()
        {
            // Arrange
            int patientId = 1;
            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-admin-sub-id", null, null)).ReturnsAsync((int?)null); // Simulate unlinked user

            // Act
            var result = await _controller.CreateNewVisitAndCheckin(new CheckinInputDto
            {
                PatientId = patientId,
                AdditionalNotes = "",
                BriefReason = ""
            });

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_CheckinConfirmation", partialViewResult.ViewData["LookupResultPartial"]);
            var confirmationData = Assert.IsType<CheckinConfirmationDto>(partialViewResult.ViewData["ConfirmationData"]);
            Assert.False(confirmationData.Success);
            Assert.Contains("not fully configured", confirmationData.ErrorMessage);
            _mockPatientService.Verify(s => s.CreateNewVisitAndCheckinAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // Similar tests should be written for ResumeActiveVisit and CloseAndStartNewVisit
        // focusing on mocking the respective _patientService calls and verifying ViewData.

        [Fact]
        public async Task ResumeActiveVisit_UserLinked_ServiceSucceeds_ShouldReturnConfirmation()
        {
            // Arrange
            int visitId = 10;
            int patientId = 1;
            int performingUserId = 10;
            var expectedConfirmation = new CheckinConfirmationDto { Success = true, PatientName = "Test Patient" };

            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-admin-sub-id", null, null)).ReturnsAsync(performingUserId);
            _mockPatientService.Setup(s => s.ResumeActiveVisitAsync(visitId, patientId, performingUserId)).ReturnsAsync(expectedConfirmation);

            // Act
            var result = await _controller.ResumeActiveVisit(visitId, patientId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_CheckinConfirmation", partialViewResult.ViewData["LookupResultPartial"]);
            Assert.Same(expectedConfirmation, partialViewResult.ViewData["ConfirmationData"]);
        }

        [Fact]
        public async Task CloseAndStartNewVisit_UserLinked_ServiceSucceeds_ShouldReturnConfirmation()
        {
            // Arrange
            int oldVisitId = 10;
            int patientId = 1;
            int performingUserId = 10;
            var expectedConfirmation = new CheckinConfirmationDto { Success = true, PatientName = "Test Patient" };

            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-admin-sub-id", null, null)).ReturnsAsync(performingUserId);
            _mockPatientService.Setup(s => s.CloseAndStartNewVisitAsync(oldVisitId, patientId, performingUserId)).ReturnsAsync(expectedConfirmation);

            // Act
            var result = await _controller.CloseAndStartNewVisit(oldVisitId, patientId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("Index", partialViewResult.ViewName);
            Assert.Equal("_CheckinConfirmation", partialViewResult.ViewData["LookupResultPartial"]);
            Assert.Same(expectedConfirmation, partialViewResult.ViewData["ConfirmationData"]);
        }
    }
}
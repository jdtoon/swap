using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using carestream.core.interfaces.services;    // lowercase
using carestream.core.interfaces.repositories; // lowercase
using carestream.core.dtos.vitals;
using carestream.web.Controllers;           // lowercase

namespace carestream.tests.unit.controllers // lowercase
{
    public class VitalsControllerTests
    {
        private readonly Mock<IVitalsService> _mockVitalsService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<VitalsController>> _mockLogger;
        private readonly VitalsController _controller;
        private readonly Mock<INurseDashboardService> _mockNurseDashboardService; // For redirect target view model

        public VitalsControllerTests()
        {
            _mockVitalsService = new Mock<IVitalsService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<VitalsController>>();
            _mockNurseDashboardService = new Mock<INurseDashboardService>(); // Mock for the redirect target

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", "test-nurse-sub-id"),
                new Claim(ClaimTypes.Role, "Nurse")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            // Mock IServiceProvider for GetRequiredService
            var serviceProviderMock = new Mock<System.IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(INurseDashboardService)))
                .Returns(_mockNurseDashboardService.Object);
            httpContext.RequestServices = serviceProviderMock.Object;


            _controller = new VitalsController(
                _mockVitalsService.Object,
                _mockUserRepository.Object,
                _mockLogger.Object
            )
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
            };
        }

        [Fact]
        public async Task StartVitalsCapture_ValidIds_ServiceReturnsModel_ShouldReturnVitalsCaptureFormPartial()
        {
            // Arrange
            int visitId = 1;
            int patientId = 101;
            var expectedModel = new VitalsCaptureInputDto { VisitId = visitId, PatientId = patientId, PatientName = "Test Patient" };
            _mockVitalsService.Setup(s => s.GetVitalsCaptureModelAsync(visitId, patientId)).ReturnsAsync(expectedModel);

            // Act
            var result = await _controller.StartVitalsCapture(visitId, patientId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_VitalsCaptureForm", partialViewResult.ViewName);
            Assert.Same(expectedModel, partialViewResult.Model);
        }

        [Theory]
        [InlineData(0, 101)]
        [InlineData(1, 0)]
        public async Task StartVitalsCapture_InvalidIds_ShouldReturnErrorPartialAndSetTempData(int visitId, int patientId)
        {
            // Act
            var result = await _controller.StartVitalsCapture(visitId, patientId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_VitalsCaptureError", partialViewResult.ViewName);
            Assert.NotNull(_controller.TempData["ErrorMessage"]);
            _mockVitalsService.Verify(s => s.GetVitalsCaptureModelAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task StartVitalsCapture_ServiceReturnsNullModel_ShouldReturnErrorPartialAndSetTempData()
        {
            // Arrange
            int visitId = 1;
            int patientId = 101;
            _mockVitalsService.Setup(s => s.GetVitalsCaptureModelAsync(visitId, patientId)).ReturnsAsync((VitalsCaptureInputDto?)null);

            // Act
            var result = await _controller.StartVitalsCapture(visitId, patientId);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_VitalsCaptureError", partialViewResult.ViewName);
            Assert.NotNull(_controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task SaveVitals_InvalidModelState_ShouldReturnFormPartialWithModel()
        {
            // Arrange
            var vitalsInput = new VitalsCaptureInputDto { VisitId = 1 }; // Missing required fields
            _controller.ModelState.AddModelError("BloodPressureSystolic", "Required");

            // Act
            var result = await _controller.SaveVitals(vitalsInput);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_VitalsCaptureForm", partialViewResult.ViewName);
            Assert.Same(vitalsInput, partialViewResult.Model); // Returns the same invalid model
            _mockVitalsService.Verify(s => s.SaveVitalsAsync(It.IsAny<VitalsCaptureInputDto>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SaveVitals_UserNotLinked_ShouldReturnFormPartialWithModelError()
        {
            // Arrange
            var vitalsInput = new VitalsCaptureInputDto { VisitId = 1, PatientId = 101, /* other required fields */ };
            _mockUserRepository.Setup(r => r.GetUserIdByLogtoSubAsync("test-nurse-sub-id", null, null)).ReturnsAsync((int?)null); // Simulate unlinked user

            // Act
            var result = await _controller.SaveVitals(vitalsInput);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_VitalsCaptureForm", partialViewResult.ViewName);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty)); // Check for model-level error
            Assert.Contains("User account not fully configured", _controller.ModelState[string.Empty]?.Errors[0].ErrorMessage);
            _mockVitalsService.Verify(s => s.SaveVitalsAsync(It.IsAny<VitalsCaptureInputDto>(), It.IsAny<int>()), Times.Never);
        }
    }
}
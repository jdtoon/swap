using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http; // For DefaultHttpContext
using System.Security.Claims;   // For ClaimsPrincipal
using carestream.core.interfaces.services;    // lowercase
using carestream.core.interfaces.repositories; // lowercase
using carestream.core.dtos.dashboard;         // lowercase
using carestream.core.dtos.doctor;            // lowercase
using carestream.core.dtos.vitals;
using carestream.web.Controllers;
using carestream.core.dtos.pharmacy;             // lowercase (assuming this DTO exists)

namespace carestream.tests.unit.controllers // lowercase
{
    public class DashboardControllerTests
    {
        private readonly Mock<IPatientAdminDashboardService> _mockPatientAdminDashboardService;
        private readonly Mock<IDashboardRepository> _mockPatientAdminRepoDirect;
        private readonly Mock<INurseDashboardService> _mockNurseDashboardService;
        private readonly Mock<IDoctorDashboardService> _mockDoctorDashboardService;
        private readonly Mock<IPharmacyService> _mockPharmacyService;
        private readonly Mock<ILogger<DashboardController>> _mockLogger;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _mockPatientAdminDashboardService = new Mock<IPatientAdminDashboardService>();
            _mockPatientAdminRepoDirect = new Mock<IDashboardRepository>();
            _mockNurseDashboardService = new Mock<INurseDashboardService>();
            _mockDoctorDashboardService = new Mock<IDoctorDashboardService>();
            _mockPharmacyService = new Mock<IPharmacyService>();
            _mockLogger = new Mock<ILogger<DashboardController>>();

            _controller = new DashboardController(
                _mockPatientAdminDashboardService.Object,
                _mockPatientAdminRepoDirect.Object,
                _mockNurseDashboardService.Object,
                _mockDoctorDashboardService.Object,
                _mockLogger.Object,
                _mockPharmacyService.Object
            );

            // Setup a default HttpContext with a user for general [Authorize] checks if needed at controller level
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "testuser") }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public void PatientAdminDashboard_ShouldReturnPartialView()
        {
            // Act
            var result = _controller.PatientAdminDashboard();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Null(partialViewResult.ViewName); // Expects PatientAdminDashboard.cshtml by convention
        }

        [Fact]
        public async Task StatsPartial_ShouldCallRepositoryAndReturnPartialViewWithStats()
        {
            // Arrange
            var expectedStats = new DashboardStatsDto { TotalSickBayVisits = 10 };
            _mockPatientAdminRepoDirect.Setup(repo => repo.GetDashboardStatsAsync(null, null)).ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.StatsPartial();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_StatsPartial", partialViewResult.ViewName);
            Assert.Same(expectedStats, partialViewResult.Model);
            _mockPatientAdminRepoDirect.Verify(repo => repo.GetDashboardStatsAsync(null, null), Times.Once);
        }

        [Fact]
        public async Task RecentPatientsPartial_ShouldCallRepositoryAndReturnPartialViewWithPatients()
        {
            // Arrange
            var expectedPatients = new List<RecentPatientDto> { new RecentPatientDto { Name = "Test Patient" } };
            _mockPatientAdminRepoDirect.Setup(repo => repo.GetRecentPatientsAsync(5, null, null)).ReturnsAsync(expectedPatients);

            // Act
            var result = await _controller.RecentPatientsPartial();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_RecentPatientsPartial", partialViewResult.ViewName);
            Assert.Same(expectedPatients, partialViewResult.Model);
            _mockPatientAdminRepoDirect.Verify(repo => repo.GetRecentPatientsAsync(5, null, null), Times.Once);
        }

        [Fact]
        public async Task RecentReportsPartial_ShouldCallRepositoryAndReturnPartialViewWithReports()
        {
            // Arrange
            var expectedReports = new List<RecentStaffReportDto> { new RecentStaffReportDto { Title = "Test Report" } };
            _mockPatientAdminRepoDirect.Setup(repo => repo.GetRecentStaffReportsAsync(5, null, null)).ReturnsAsync(expectedReports);

            // Act
            var result = await _controller.RecentReportsPartial();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_RecentReportsPartial", partialViewResult.ViewName);
            Assert.Same(expectedReports, partialViewResult.Model);
            _mockPatientAdminRepoDirect.Verify(repo => repo.GetRecentStaffReportsAsync(5, null, null), Times.Once);
        }

        [Fact]
        public async Task NurseDashboard_ShouldCallServiceAndReturnPartialViewWithModel()
        {
            // Arrange
            var expectedViewModel = new NurseDashboardViewModel(); // Assuming this DTO exists
            _mockNurseDashboardService.Setup(s => s.GetDashboardViewModelAsync()).ReturnsAsync(expectedViewModel);

            // Act
            var result = await _controller.NurseDashboard();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Null(partialViewResult.ViewName); // Expects NurseDashboard.cshtml by convention
            Assert.Same(expectedViewModel, partialViewResult.Model);
            _mockNurseDashboardService.Verify(s => s.GetDashboardViewModelAsync(), Times.Once);
        }

        [Fact]
        public async Task DoctorDashboard_ShouldCallServiceAndReturnPartialViewWithModel()
        {
            // Arrange
            var expectedViewModel = new DoctorDashboardViewModel();
            _mockDoctorDashboardService.Setup(s => s.GetDashboardViewModelAsync()).ReturnsAsync(expectedViewModel);

            // Act
            var result = await _controller.DoctorDashboard();

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Null(partialViewResult.ViewName); // Expects DoctorDashboard.cshtml by convention
            Assert.Same(expectedViewModel, partialViewResult.Model);
            _mockDoctorDashboardService.Verify(s => s.GetDashboardViewModelAsync(), Times.Once);
        }

        [Fact]
        public async Task PharmacistDashboard_ShouldCallServiceAndReturnPartialViewWithModel()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 15;
            var expectedViewModel = new PharmacistDashboardViewModel { Stats = new PharmacistDashboardStatsDto() };
            _mockPharmacyService
                .Setup(s => s.GetDashboardViewModelAsync(pageNumber, pageSize))
                .ReturnsAsync(expectedViewModel);

            // Act
            // Call the async version of the action method
            var result = await _controller.PharmacistDashboard(pageNumber, pageSize);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Null(partialViewResult.ViewName); // Expects PharmacistDashboard.cshtml by convention
            Assert.Same(expectedViewModel, partialViewResult.Model); // Assert the model from the service is passed
            _mockPharmacyService.Verify(s => s.GetDashboardViewModelAsync(pageNumber, pageSize), Times.Once);
        }
    }
}
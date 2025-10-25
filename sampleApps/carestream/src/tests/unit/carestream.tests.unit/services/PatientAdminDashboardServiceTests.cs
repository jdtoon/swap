using Moq;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.services;
using carestream.core.dtos.dashboard;
using System.Data;
using Microsoft.Extensions.Logging;

namespace carestream.tests.unit.services
{
    public class PatientAdminDashboardServiceTests
    {
        private readonly Mock<IDashboardRepository> _mockDashboardRepository; // Mock the repository
        private readonly IPatientAdminDashboardService _dashboardService; // Service under test
        private readonly Mock<ILogger<PatientAdminDashboardService>> _mockLogger;

        public PatientAdminDashboardServiceTests()
        {
            // Arrange: Set up the mock repository before each test
            _mockDashboardRepository = new Mock<IDashboardRepository>();
            _mockLogger = new Mock<ILogger<PatientAdminDashboardService>>();

            _dashboardService = new PatientAdminDashboardService(_mockDashboardRepository.Object, _mockLogger.Object); // Inject the mock object
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldReturnViewModelWithDataFromRepository()
        {
            // Arrange: Configure the mock repository methods to return specific test data
            var testStats = new DashboardStatsDto { TotalSickBayVisits = 10, CurrentlyInTreatment = 3, PendingCheckin = 2 };
            var testPatients = new List<RecentPatientDto>
            {
                new RecentPatientDto { Name = "Test Patient 1", Status = "In Treatment", VisitTimestamp = DateTime.UtcNow.AddHours(-1) },
                new RecentPatientDto { Name = "Test Patient 2", Status = "Discharged", VisitTimestamp = DateTime.UtcNow.AddHours(-2) }
            };
            var testReports = new List<RecentStaffReportDto>
            {
                new RecentStaffReportDto { Title = "Test Report 1", Author = "Author 1", Timestamp = DateTime.UtcNow.AddDays(-1) },
                new RecentStaffReportDto { Title = "Test Report 2", Author = "Author 2", Timestamp = DateTime.UtcNow.AddDays(-2) }
            };

            // Setup the mock methods to return the arranged data when called
            _mockDashboardRepository.Setup(repo => repo.GetDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                    .ReturnsAsync(testStats); // Use ReturnsAsync for Task<T>

            _mockDashboardRepository.Setup(repo => repo.GetRecentPatientsAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())) // Match any limit parameter
                                    .ReturnsAsync(testPatients);

            _mockDashboardRepository.Setup(repo => repo.GetRecentStaffReportsAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                    .ReturnsAsync(testReports);

            // Act: Call the service method we want to test
            var resultViewModel = await _dashboardService.GetDashboardViewModelAsync();

            // Assert: Verify the results
            Assert.NotNull(resultViewModel); // Check if the view model is not null

            // Assert Stats
            Assert.NotNull(resultViewModel.Stats);
            Assert.Equal(testStats.TotalSickBayVisits, resultViewModel.Stats.TotalSickBayVisits);
            Assert.Equal(testStats.CurrentlyInTreatment, resultViewModel.Stats.CurrentlyInTreatment);
            Assert.Equal(testStats.PendingCheckin, resultViewModel.Stats.PendingCheckin);

            // Assert Recent Patients
            Assert.NotNull(resultViewModel.RecentPatients);
            Assert.Equal(testPatients.Count, resultViewModel.RecentPatients.Count);
            Assert.Equal(testPatients[0].Name, resultViewModel.RecentPatients[0].Name); // Basic check on content
            Assert.Equal(testPatients[1].Name, resultViewModel.RecentPatients[1].Name);

            // Assert Recent Staff Reports
            Assert.NotNull(resultViewModel.RecentStaffReports);
            Assert.Equal(testReports.Count, resultViewModel.RecentStaffReports.Count);
            Assert.Equal(testReports[0].Title, resultViewModel.RecentStaffReports[0].Title);
            Assert.Equal(testReports[1].Title, resultViewModel.RecentStaffReports[1].Title);

            // Verify that the repository methods were called exactly once
            _mockDashboardRepository.Verify(repo => repo.GetDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockDashboardRepository.Verify(repo => repo.GetRecentPatientsAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockDashboardRepository.Verify(repo => repo.GetRecentStaffReportsAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldHandleEmptyRepositoryResults()
        {
            // Arrange: Configure mocks to return empty results
            var testStats = new DashboardStatsDto { TotalSickBayVisits = 0, CurrentlyInTreatment = 0, PendingCheckin = 0 }; // Or null if repository could return null
            var emptyPatients = new List<RecentPatientDto>();
            var emptyReports = new List<RecentStaffReportDto>();

            _mockDashboardRepository.Setup(repo => repo.GetDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(testStats);
            _mockDashboardRepository.Setup(repo => repo.GetRecentPatientsAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(emptyPatients);
            _mockDashboardRepository.Setup(repo => repo.GetRecentStaffReportsAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(emptyReports);

            // Act
            var resultViewModel = await _dashboardService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(resultViewModel);
            Assert.NotNull(resultViewModel.Stats);
            Assert.Equal(0, resultViewModel.Stats.CurrentlyInTreatment); // Check stats correctly reflect empty data
            Assert.NotNull(resultViewModel.RecentPatients);
            Assert.Empty(resultViewModel.RecentPatients); // Check list is empty
            Assert.NotNull(resultViewModel.RecentStaffReports);
            Assert.Empty(resultViewModel.RecentStaffReports); // Check list is empty
        }
    }
}
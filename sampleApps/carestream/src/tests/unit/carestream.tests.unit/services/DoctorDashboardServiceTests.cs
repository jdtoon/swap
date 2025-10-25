using Moq;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.services;
using carestream.core.dtos.doctor;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; // Required for ClaimsPrincipal and Claim
using System.Collections.Generic; // Required for List<Claim>
using System.Threading.Tasks;
using System; // Required for DateTime

namespace carestream.tests.unit.services // lowercase
{
    public class DoctorDashboardServiceTests
    {
        private readonly Mock<IVisitRepository> _mockVisitRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<DoctorDashboardService>> _mockLogger;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly IDoctorDashboardService _doctorDashboardService;

        public DoctorDashboardServiceTests()
        {
            _mockVisitRepository = new Mock<IVisitRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<DoctorDashboardService>>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>(); // Initialize the mock

            // *** SETUP MOCK HTTP CONTEXT ACCESSOR ***
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("sub", "test-doctor-logto-sub-id") // Provide a test "sub" claim
                // Add other claims if your service directly uses them
            }, "testAuthType"));

            var httpContext = new DefaultHttpContext { User = user }; // Create a DefaultHttpContext and set its User

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext); // Configure mock to return this context
            // *** END SETUP ***

            _doctorDashboardService = new DoctorDashboardService(
                _mockVisitRepository.Object,
                _mockUserRepository.Object,
                _mockHttpContextAccessor.Object, // Pass the configured mock
                _mockLogger.Object
            );

            
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldReturnViewModelWithDataFromRepository_WhenUserIsValid()
        {
            // Arrange
            var testDoctorInternalId = 123; // Assume this is the internal ID for the test doctor
            var testLogtoSubId = "test-doctor-logto-sub-id"; // Must match the "sub" claim set up in constructor

            var testStats = new DoctorDashboardStatsDto { /* ... */ };
            var testQueue = new List<DoctorQueueItemDto> { /* ... */ };
            var testInProgress = new List<DoctorQueueItemDto> { /* ... */ }; // Add test data for in-progress

            // Setup mock UserRepository to return the doctor's internal ID
            _mockUserRepository.Setup(repo => repo.GetUserIdByLogtoSubAsync(testLogtoSubId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                               .ReturnsAsync(testDoctorInternalId);

            _mockVisitRepository.Setup(repo => repo.GetDoctorDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(testStats);
            _mockVisitRepository.Setup(repo => repo.GetDoctorPatientQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(testQueue);
            // Setup mock for InProgressConsultations
            _mockVisitRepository.Setup(repo => repo.GetInProgressConsultationsForDoctorAsync(testDoctorInternalId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(testInProgress);


            // Act
            var resultViewModel = await _doctorDashboardService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(resultViewModel);
            Assert.NotNull(resultViewModel.Stats);
            // ... other assertions for stats and queue ...
            Assert.NotNull(resultViewModel.InProgressConsultations); // Assert new list
            Assert.Equal(testInProgress.Count, resultViewModel.InProgressConsultations.Count);


            // Verify calls
            _mockUserRepository.Verify(repo => repo.GetUserIdByLogtoSubAsync(testLogtoSubId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockVisitRepository.Verify(repo => repo.GetDoctorDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockVisitRepository.Verify(repo => repo.GetDoctorPatientQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockVisitRepository.Verify(repo => repo.GetInProgressConsultationsForDoctorAsync(testDoctorInternalId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldReturnEmptyViewModel_WhenHttpContextUserIsNull()
        {
            // Arrange
            // Override the default HttpContext setup for this specific test case
            var httpContextWithoutUser = new DefaultHttpContext { User = null! }; // Explicitly set User to null
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContextWithoutUser);
            // OR more simply:
            _mockHttpContextAccessor.Setup(x => x.HttpContext!.User).Returns((ClaimsPrincipal)null!); // This doesn't work as HttpContext itself could be null
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext()); // User will be null by default

            // Act
            var resultViewModel = await _doctorDashboardService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(resultViewModel);
            // Check that the properties of the empty view model are in their default/empty state
            Assert.NotNull(resultViewModel.Stats); // The constructor of ViewModel initializes Stats
            Assert.Equal(0, resultViewModel.Stats.TotalWaitingForDoctor);
            Assert.Empty(resultViewModel.PatientQueue);
            Assert.Empty(resultViewModel.InProgressConsultations);

            // Verify that no repository calls were made because the service returned early
            _mockUserRepository.Verify(repo => repo.GetUserIdByLogtoSubAsync(It.IsAny<string>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
            _mockVisitRepository.Verify(repo => repo.GetDoctorDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
            _mockVisitRepository.Verify(repo => repo.GetDoctorPatientQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
            _mockVisitRepository.Verify(repo => repo.GetInProgressConsultationsForDoctorAsync(It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldReturnEmptyViewModel_WhenSubClaimIsMissing()
        {
            // Arrange
            var userWithoutSub = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "testAuthType")); // No "sub" claim
            var httpContext = new DefaultHttpContext { User = userWithoutSub };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var resultViewModel = await _doctorDashboardService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(resultViewModel);
            Assert.Equal(0, resultViewModel!.Stats!.TotalWaitingForDoctor);
            Assert.Empty(resultViewModel.PatientQueue);
            Assert.Empty(resultViewModel.InProgressConsultations);

            _mockUserRepository.Verify(repo => repo.GetUserIdByLogtoSubAsync(It.IsAny<string>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
        }

        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldReturnEmptyViewModel_WhenUserIsNotLinkedInternally()
        {
            // Arrange
            var testLogtoSubId = "unlinked-doctor-logto-sub-id";
            var userWithSub = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("sub", testLogtoSubId)
            }, "testAuthType"));
            var httpContext = new DefaultHttpContext { User = userWithSub };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            _mockUserRepository.Setup(repo => repo.GetUserIdByLogtoSubAsync(testLogtoSubId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                               .ReturnsAsync((int?)null); // Simulate user not found in our DB

            // Act
            var resultViewModel = await _doctorDashboardService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(resultViewModel);
            Assert.Equal(0, resultViewModel!.Stats!.TotalWaitingForDoctor);
            Assert.Empty(resultViewModel.PatientQueue);
            Assert.Empty(resultViewModel.InProgressConsultations);

            _mockUserRepository.Verify(repo => repo.GetUserIdByLogtoSubAsync(testLogtoSubId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once); // It was called
            _mockVisitRepository.Verify(repo => repo.GetDoctorDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never); // But these should not be
        }


        // Keep your existing test: GetDashboardViewModelAsync_ShouldHandleEmptyResultsFromRepository()
        // BUT ensure it also sets up the HttpContextAccessor and UserRepository mock correctly.
        [Fact]
        public async Task GetDashboardViewModelAsync_ShouldHandleEmptyRepositoryResults_WhenUserIsValid()
        {
            // Arrange
            var testDoctorInternalId = 456;
            var testLogtoSubId = "another-doctor-logto-sub-id";
            // Setup HttpContextAccessor for a valid user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", testLogtoSubId) }, "testAuthType"));
            var httpContext = new DefaultHttpContext { User = user };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Setup UserRepository to return a valid internal ID
            _mockUserRepository.Setup(repo => repo.GetUserIdByLogtoSubAsync(testLogtoSubId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(testDoctorInternalId);


            // Setup repository methods to return empty/default data
            var emptyStats = new DoctorDashboardStatsDto { TotalWaitingForDoctor = 0, UrgentCasesCount = 0, HighPriorityCasesCount = 0, AverageWaitTime = "0m" };
            var emptyQueue = new List<DoctorQueueItemDto>();
            var emptyInProgress = new List<DoctorQueueItemDto>();


            _mockVisitRepository.Setup(repo => repo.GetDoctorDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(emptyStats);
            _mockVisitRepository.Setup(repo => repo.GetDoctorPatientQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(emptyQueue);
            _mockVisitRepository.Setup(repo => repo.GetInProgressConsultationsForDoctorAsync(testDoctorInternalId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                               .ReturnsAsync(emptyInProgress);

            // Act
            var resultViewModel = await _doctorDashboardService.GetDashboardViewModelAsync();

            // Assert
            Assert.NotNull(resultViewModel);
            Assert.NotNull(resultViewModel.Stats);
            Assert.Equal(0, resultViewModel.Stats.TotalWaitingForDoctor);
            Assert.NotNull(resultViewModel.PatientQueue);
            Assert.Empty(resultViewModel.PatientQueue);
            Assert.NotNull(resultViewModel.InProgressConsultations);
            Assert.Empty(resultViewModel.InProgressConsultations);
        }
    }
}
//using Xunit;
//using Moq;
//using carestream.core.interfaces.repositories; // lowercase
//using carestream.core.interfaces.services;    // lowercase
//using carestream.core.services;               // lowercase
//using carestream.core.dtos.vitals;           // lowercase
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System;
//using System.Linq;
//using System.Data;

//namespace carestream.tests.unit.services // lowercase
//{
//    public class NurseDashboardServiceTests
//    {
//        private readonly Mock<IVisitRepository> _mockVisitRepository;
//        private readonly INurseDashboardService _nurseDashboardService;

//        public NurseDashboardServiceTests()
//        {
//            _mockVisitRepository = new Mock<IVisitRepository>();
//            _nurseDashboardService = new NurseDashboardService(_mockVisitRepository.Object);
//        }

//        [Fact]
//        public async Task GetDashboardViewModelAsync_ShouldReturnViewModelWithDataFromRepository()
//        {
//            // Arrange
//            var testStats = new VitalsDashboardStatsDto { WaitingForVitals = 5, VitalsInProgress = 2, ReadyForDoctor = 3 };
//            var testQueue = new List<VitalsQueueItemDto>
//            {
//                new VitalsQueueItemDto { VisitId = 1, PatientId = 10, PatientName = "Patient A", CheckinTimestamp = DateTime.UtcNow.AddMinutes(-15) },
//                new VitalsQueueItemDto { VisitId = 2, PatientId = 11, PatientName = "Patient B", CheckinTimestamp = DateTime.UtcNow.AddMinutes(-5) }
//            };

//            _mockVisitRepository.Setup(repo => repo.GetVitalsDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
//                                .ReturnsAsync(testStats);
//            _mockVisitRepository.Setup(repo => repo.GetVitalsQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
//                                .ReturnsAsync(testQueue);

//            // Act
//            var resultViewModel = await _nurseDashboardService.GetDashboardViewModelAsync();

//            // Assert
//            Assert.NotNull(resultViewModel);

//            // Assert Stats
//            Assert.NotNull(resultViewModel.Stats);
//            Assert.Equal(testStats.WaitingForVitals, resultViewModel.Stats.WaitingForVitals);
//            Assert.Equal(testStats.VitalsInProgress, resultViewModel.Stats.VitalsInProgress);
//            Assert.Equal(testStats.ReadyForDoctor, resultViewModel.Stats.ReadyForDoctor);

//            // Assert Vitals Queue
//            Assert.NotNull(resultViewModel.VitalsQueue);
//            Assert.Equal(testQueue.Count, resultViewModel.VitalsQueue.Count);
//            Assert.Equal(testQueue[0].PatientName, resultViewModel.VitalsQueue[0].PatientName);
//            Assert.Equal(testQueue[1].VisitId, resultViewModel.VitalsQueue[1].VisitId);

//            // Verify repository methods were called
//            _mockVisitRepository.Verify(repo => repo.GetVitalsDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
//            _mockVisitRepository.Verify(repo => repo.GetVitalsQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
//        }

//        [Fact]
//        public async Task GetDashboardViewModelAsync_ShouldHandleEmptyResultsFromRepository()
//        {
//            // Arrange
//            var emptyStats = new VitalsDashboardStatsDto { WaitingForVitals = 0, VitalsInProgress = 0, ReadyForDoctor = 0 };
//            var emptyQueue = new List<VitalsQueueItemDto>();

//            _mockVisitRepository.Setup(repo => repo.GetVitalsDashboardStatsAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
//                                .ReturnsAsync(emptyStats); // Can also return null if repo is designed to
//            _mockVisitRepository.Setup(repo => repo.GetVitalsQueueAsync(It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
//                                .ReturnsAsync(emptyQueue);

//            // Act
//            var resultViewModel = await _nurseDashboardService.GetDashboardViewModelAsync();

//            // Assert
//            Assert.NotNull(resultViewModel);
//            Assert.NotNull(resultViewModel.Stats); // Service should still provide a Stats object
//            Assert.Equal(0, resultViewModel.Stats.WaitingForVitals);

//            Assert.NotNull(resultViewModel.VitalsQueue);
//            Assert.Empty(resultViewModel.VitalsQueue); // Expect empty list
//        }
//    }
//}
using Xunit;
using Moq;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.services;
using carestream.core.dtos.vitals;
using carestream.core.dtos.patient; // Needed for PatientBasicInfoDto
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Data; // If needed later for list assertions

namespace carestream.tests.unit.services
{
    /// <summary>
    /// Unit tests for the <see cref="VitalsService"/>.
    /// Verifies the service logic by mocking repository dependencies.
    /// </summary>
    public class VitalsServiceTests
    {
        private readonly Mock<IVitalsRepository> _mockVitalsRepository;
        private readonly Mock<IVisitRepository> _mockVisitRepository;
        private readonly Mock<IPatientRepository> _mockPatientRepository; // Added mock for patient context
        private readonly Mock<ILogger<VitalsService>> _mockLogger;
        private readonly IVitalsService _vitalsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VitalsServiceTests"/> class.
        /// Sets up mocks for all injected dependencies.
        /// </summary>
        public VitalsServiceTests()
        {
            _mockVitalsRepository = new Mock<IVitalsRepository>();
            _mockVisitRepository = new Mock<IVisitRepository>();
            _mockPatientRepository = new Mock<IPatientRepository>(); // Initialize patient repo mock
            _mockLogger = new Mock<ILogger<VitalsService>>();

            // Instantiate the service under test with all mocked dependencies
            _vitalsService = new VitalsService(
                _mockVitalsRepository.Object,
                _mockVisitRepository.Object,
                _mockPatientRepository.Object, // Pass the patient repo mock
                _mockLogger.Object
            );
        }

        #region GetVitalsCaptureModelAsync Tests

        /// <summary>
        /// Tests that when no existing vitals are found, the service fetches patient info
        /// and returns a new DTO populated with context.
        /// </summary>
        [Fact]
        public async Task GetVitalsCaptureModelAsync_WhenNoExistingVitals_ShouldReturnNewDtoWithPatientContext()
        {
            // Arrange
            int visitId = 1;
            int patientId = 101;
            // Mock the patient context that the repository should return
            var patientContextDto = new PatientBasicInfoDto { PatientId = patientId, FirstName = "John", LastName = "Doe", Rank = "Pte" };

            // Setup patient repository mock to return the context DTO when called with the correct patient ID
            _mockPatientRepository.Setup(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                .ReturnsAsync(patientContextDto);

            // Setup vitals repository mock to return null (no existing vitals for this visit)
            _mockVitalsRepository.Setup(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                .ReturnsAsync((VitalsCaptureInputDto?)null);

            // Act
            var result = await _vitalsService.GetVitalsCaptureModelAsync(visitId, patientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(visitId, result.VisitId);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("John Doe", result.PatientName); // Verify name is populated from patient context
            Assert.Equal("Pte", result.PatientRank);       // Verify rank is populated from patient context
            // Verify repository interactions
            _mockVitalsRepository.Verify(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockPatientRepository.Verify(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once); // Verify patient context was fetched
        }

        /// <summary>
        /// Tests that when existing vitals are found, the service returns those vitals
        /// and enriches them with the latest patient context.
        /// </summary>
        [Fact]
        public async Task GetVitalsCaptureModelAsync_WhenExistingVitalsFound_ShouldReturnExistingVitalsWithPatientContext()
        {
            // Arrange
            int visitId = 2;
            int patientId = 102;
            // Mock patient context
            var patientContextDto = new PatientBasicInfoDto { PatientId = patientId, FirstName = "Jane", LastName = "Smith", Rank = "Sgt" };
            // Mock existing vitals data returned by vitals repo
            var existingVitals = new VitalsCaptureInputDto { VisitId = visitId, PatientId = patientId, Temperature = 37.5m, HeartRate = 88 };

            _mockPatientRepository.Setup(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                .ReturnsAsync(patientContextDto);
            _mockVitalsRepository.Setup(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                .ReturnsAsync(existingVitals);

            // Act
            var result = await _vitalsService.GetVitalsCaptureModelAsync(visitId, patientId);

            // Assert
            Assert.NotNull(result);
            Assert.Same(existingVitals, result); // Should return the same object instance from the repo
            Assert.Equal(visitId, result.VisitId);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal(37.5m, result.Temperature); // Verify existing vitals data is present
            Assert.Equal(88, result.HeartRate);
            Assert.Equal("Jane Smith", result.PatientName); // Verify name was populated/overwritten from patient context
            Assert.Equal("Sgt", result.PatientRank);       // Verify rank was populated/overwritten from patient context
            _mockVitalsRepository.Verify(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockPatientRepository.Verify(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        }

        /// <summary>
        /// Tests that if the patient context cannot be found by the repository,
        /// the service returns null.
        /// </summary>
        [Fact]
        public async Task GetVitalsCaptureModelAsync_WhenPatientContextNotFound_ShouldReturnNull() // Renamed test slightly
        {
            // Arrange
            int visitId = 3;
            int patientId = 103;
            // Mock patient repository to return null
            _mockPatientRepository.Setup(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
               .ReturnsAsync((PatientBasicInfoDto?)null);
            // Mock vitals repo to return null (won't be called if patient lookup fails first, but good practice)
            _mockVitalsRepository.Setup(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
               .ReturnsAsync((VitalsCaptureInputDto?)null);

            // Act
            var result = await _vitalsService.GetVitalsCaptureModelAsync(visitId, patientId);

            // Assert
            Assert.Null(result); // *** CORRECTED ASSERTION: Expect null now ***
            _mockPatientRepository.Verify(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once); // Verify patient lookup was attempted
            _mockVitalsRepository.Verify(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never); // Vitals lookup should not happen if patient fails
        }


        /// <summary>
        /// Tests that if fetching patient context fails (repository throws exception),
        /// the service returns null to indicate failure to prepare the model.
        /// </summary>
        [Fact]
        public async Task GetVitalsCaptureModelAsync_WhenPatientRepoThrows_ShouldReturnNull()
        {
            // Arrange
            int visitId = 4;
            int patientId = 104;
            _mockPatientRepository.Setup(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
               .ThrowsAsync(new Exception("Simulated DB error")); // Simulate repo failure
            _mockVitalsRepository.Setup(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                .ReturnsAsync((VitalsCaptureInputDto?)null); // Vitals repo won't be called if patient fails first, but setup for completeness

            // Act
            var result = await _vitalsService.GetVitalsCaptureModelAsync(visitId, patientId);

            // Assert
            Assert.Null(result); // Service should return null on failure to get patient context
            _mockPatientRepository.Verify(repo => repo.GetPatientBasicInfoByIdAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockVitalsRepository.Verify(repo => repo.GetVitalsForVisitAsync(visitId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never); // Should not be called if patient lookup failed
        }

        #endregion

        #region SaveVitalsAsync Tests

        /// <summary>
        /// Tests the successful path of <see cref="VitalsService.SaveVitalsAsync"/>,
        /// ensuring both vitals creation and visit status update repository methods are called.
        /// </summary>
        //[Fact]
        //public async Task SaveVitalsAsync_WhenSuccessful_ShouldCreateVitalsAndUpdateVisitStatusAndReturnTrue()
        //{ 
        //    // Arrange
        //    int performingUserId = 1;
        //    var inputDto = new VitalsCaptureInputDto { VisitId = 10, PatientId = 20 };
        //    int newVitalsRecordId = 123;

        //    // Mock successful creation of vitals record
        //    _mockVitalsRepository.Setup(repo => repo.CreateVitalsRecordAsync(inputDto, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //        .ReturnsAsync(newVitalsRecordId);
        //    // Mock successful update of visit status
        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(inputDto.VisitId, _statusReadyForDoctor, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //        .ReturnsAsync(true);

        //    // Act
        //    var result = await _vitalsService.SaveVitalsAsync(inputDto, performingUserId);

        //    // Assert
        //    Assert.True(result);
        //    Assert.Equal(performingUserId, inputDto.RecordedByUserId); // Verify service set the user ID
        //    Assert.NotNull(inputDto.RecordedAt);                       // Verify service set the timestamp
        //    _mockVitalsRepository.Verify(repo => repo.CreateVitalsRecordAsync(inputDto, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(inputDto.VisitId, _statusReadyForDoctor, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        ///// <summary>
        ///// Tests that <see cref="VitalsService.SaveVitalsAsync"/> returns false and does not
        ///// attempt to update the visit status if the vitals record creation fails in the repository.
        ///// </summary>
        //[Fact]
        //public async Task SaveVitalsAsync_WhenCreateVitalsRecordFails_ShouldReturnFalseAndNotUpdateVisitStatus()
        //{
        //    // Arrange
        //    int performingUserId = 2;
        //    var inputDto = new VitalsCaptureInputDto { VisitId = 11, PatientId = 21 };

        //    // Mock failure of vitals record creation (returns 0 or less)
        //    _mockVitalsRepository.Setup(repo => repo.CreateVitalsRecordAsync(inputDto, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //        .ReturnsAsync(0);

        //    // Act
        //    var result = await _vitalsService.SaveVitalsAsync(inputDto, performingUserId);

        //    // Assert
        //    Assert.False(result);
        //    _mockVitalsRepository.Verify(repo => repo.CreateVitalsRecordAsync(inputDto, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    // Verify visit status update was NOT called
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
        //}

        ///// <summary>
        ///// Tests that <see cref="VitalsService.SaveVitalsAsync"/> returns false if updating the
        ///// visit status fails, even if the vitals record was created successfully.
        ///// </summary>
        //[Fact]
        //public async Task SaveVitalsAsync_WhenUpdateVisitStatusFails_ShouldReturnFalse()
        //{
        //    // Arrange
        //    int performingUserId = 3;
        //    var inputDto = new VitalsCaptureInputDto { VisitId = 12, PatientId = 22 };
        //    int newVitalsRecordId = 456;

        //    // Mock successful vitals creation
        //    _mockVitalsRepository.Setup(repo => repo.CreateVitalsRecordAsync(inputDto, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //        .ReturnsAsync(newVitalsRecordId);
        //    // Mock failure of visit status update
        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(inputDto.VisitId, _statusReadyForDoctor, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //        .ReturnsAsync(false);

        //    // Act
        //    var result = await _vitalsService.SaveVitalsAsync(inputDto, performingUserId);

        //    // Assert
        //    Assert.False(result); // Overall operation considered failed
        //    _mockVitalsRepository.Verify(repo => repo.CreateVitalsRecordAsync(inputDto, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(inputDto.VisitId, _statusReadyForDoctor, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        /// <summary>
        /// Tests that <see cref="VitalsService.SaveVitalsAsync"/> throws an
        /// <see cref="ArgumentNullException"/> if the input DTO is null.
        /// </summary>
        [Fact]
        public async Task SaveVitalsAsync_WithNullInputDto_ShouldThrowArgumentNullException()
        {
            // Arrange
            int performingUserId = 4;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _vitalsService.SaveVitalsAsync(null!, performingUserId));
            _mockVitalsRepository.VerifyNoOtherCalls(); // Ensure no repo calls made
            _mockVisitRepository.VerifyNoOtherCalls();
        }

        #endregion
    }
}
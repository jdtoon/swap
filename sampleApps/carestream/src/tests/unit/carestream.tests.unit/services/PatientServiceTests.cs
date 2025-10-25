using Xunit;
using Moq;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.services;
using carestream.core.dtos.patient;
using carestream.core.dtos.checkin;
using carestream.core.dtos.visit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Data; // Not strictly needed but good to have for list ops if tests evolve

namespace carestream.tests.unit.services
{
    /// <summary>
    /// Unit tests for the <see cref="PatientService"/>.
    /// These tests mock repository dependencies to isolate and verify service layer logic,
    /// including interactions with logging.
    /// </summary>
    public class PatientServiceTests
    {
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly Mock<IVisitRepository> _mockVisitRepository;
        private readonly Mock<ILogger<PatientService>> _mockLogger;
        private readonly IPatientService _patientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientServiceTests"/> class.
        /// Sets up mocks for repositories and logger, and instantiates the service under test.
        /// </summary>
        public PatientServiceTests()
        {
            _mockPatientRepository = new Mock<IPatientRepository>();
            _mockVisitRepository = new Mock<IVisitRepository>();
            _mockLogger = new Mock<ILogger<PatientService>>();

            _patientService = new PatientService(
                _mockPatientRepository.Object,
                _mockVisitRepository.Object,
                _mockLogger.Object
            );
        }

        #region GetPatientByForceNumberAsync Tests

        /// <summary>
        /// Tests that <see cref="PatientService.GetPatientByForceNumberAsync"/>
        /// calls the repository and returns the patient when a valid force number is provided
        /// and the patient exists.
        /// </summary>
        [Fact]
        public async Task GetPatientByForceNumberAsync_WithValidForceNumber_ShouldCallRepositoryAndReturnPatient()
        {
            // Arrange
            string validForceNumber = "P123";
            var expectedPatient = new PatientDetailDto { PatientId = 101, ForceNumber = validForceNumber, FirstName = "James", LastName = "Wilson" };

            _mockPatientRepository.Setup(repo => repo.FindByForceNumberAsync(validForceNumber, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(expectedPatient);

            // Act
            var actualPatient = await _patientService.GetPatientByForceNumberAsync(validForceNumber);

            // Assert
            Assert.NotNull(actualPatient);
            Assert.Equal(expectedPatient.PatientId, actualPatient.PatientId);
            Assert.Equal(expectedPatient.ForceNumber, actualPatient.ForceNumber);
            _mockPatientRepository.Verify(repo => repo.FindByForceNumberAsync(validForceNumber, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockVisitRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Tests that <see cref="PatientService.GetPatientByForceNumberAsync"/>
        /// returns null if the repository returns null (patient not found).
        /// </summary>
        [Fact]
        public async Task GetPatientByForceNumberAsync_WithValidForceNumber_ShouldReturnNullIfRepositoryReturnsNull()
        {
            // Arrange
            string nonExistentForceNumber = "X999";
            _mockPatientRepository.Setup(repo => repo.FindByForceNumberAsync(nonExistentForceNumber, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync((PatientDetailDto?)null);

            // Act
            var actualPatient = await _patientService.GetPatientByForceNumberAsync(nonExistentForceNumber);

            // Assert
            Assert.Null(actualPatient);
            _mockPatientRepository.Verify(repo => repo.FindByForceNumberAsync(nonExistentForceNumber, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockVisitRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Tests that <see cref="PatientService.GetPatientByForceNumberAsync"/>
        /// returns null and does not call the repository if an invalid force number (null, empty, whitespace) is provided.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")] // Whitespace
        public async Task GetPatientByForceNumberAsync_WithInvalidForceNumber_ShouldReturnNullAndNotCallRepository(string? invalidForceNumber)
        {
            // Act
            var actualPatient = await _patientService.GetPatientByForceNumberAsync(invalidForceNumber!);

            // Assert
            Assert.Null(actualPatient);
            _mockPatientRepository.Verify(repo => repo.FindByForceNumberAsync(It.IsAny<string>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
            _mockVisitRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Tests that <see cref="PatientService.GetPatientByForceNumberAsync"/>
        /// trims whitespace from the force number before calling the repository.
        /// </summary>
        [Fact]
        public async Task GetPatientByForceNumberAsync_ShouldTrimForceNumberBeforeCallingRepository()
        {
            // Arrange
            string forceNumberWithWhitespace = " P123 ";
            string expectedTrimmedForceNumber = "P123";
            var expectedPatient = new PatientDetailDto { PatientId = 101, ForceNumber = expectedTrimmedForceNumber };

            _mockPatientRepository.Setup(repo => repo.FindByForceNumberAsync(expectedTrimmedForceNumber, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(expectedPatient);

            // Act
            await _patientService.GetPatientByForceNumberAsync(forceNumberWithWhitespace);

            // Assert
            _mockPatientRepository.Verify(repo => repo.FindByForceNumberAsync(expectedTrimmedForceNumber, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockPatientRepository.Verify(repo => repo.FindByForceNumberAsync(forceNumberWithWhitespace, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
            _mockVisitRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region GetActiveVisitForPatientAsync Tests

        /// <summary>
        /// Tests that <see cref="PatientService.GetActiveVisitForPatientAsync"/>
        /// returns active visit data when the repository finds one.
        /// </summary>
        [Fact]
        public async Task GetActiveVisitForPatientAsync_WhenActiveVisitExists_ShouldReturnActiveVisitDto()
        {
            // Arrange
            int patientId = 1;
            var expectedVisit = new ActiveVisitDto { VisitId = 123, Status = "Waiting for Doctor", VisitTimestamp = DateTime.UtcNow };
            _mockVisitRepository.Setup(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync(expectedVisit);

            // Act
            var actualVisit = await _patientService.GetActiveVisitForPatientAsync(patientId);

            // Assert
            Assert.NotNull(actualVisit);
            Assert.Equal(expectedVisit.VisitId, actualVisit.VisitId);
            Assert.Equal(expectedVisit.Status, actualVisit.Status);
            _mockVisitRepository.Verify(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockPatientRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Tests that <see cref="PatientService.GetActiveVisitForPatientAsync"/>
        /// returns null when the repository finds no active visit.
        /// </summary>
        [Fact]
        public async Task GetActiveVisitForPatientAsync_WhenNoActiveVisitExists_ShouldReturnNull()
        {
            // Arrange
            int patientId = 2;
            _mockVisitRepository.Setup(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
                                .ReturnsAsync((ActiveVisitDto?)null);

            // Act
            var actualVisit = await _patientService.GetActiveVisitForPatientAsync(patientId);

            // Assert
            Assert.Null(actualVisit);
            _mockVisitRepository.Verify(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
            _mockPatientRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region CreateNewVisitAndCheckinAsync Tests

        /// <summary>
        /// Tests that <see cref="PatientService.CreateNewVisitAndCheckinAsync"/>
        /// successfully creates a new visit when no active visit exists.
        /// </summary>
        //[Fact]
        //public async Task CreateNewVisitAndCheckinAsync_WhenNoActiveVisit_ShouldCreateNewVisitAndSucceed()
        //{
        //    // Arrange
        //    int patientId = 101;
        //    int performingUserId = 1;
        //    string expectedInitialStatus = "Waiting for Vitals";
        //    int newVisitId = 55;

        //    _mockVisitRepository.Setup(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync((ActiveVisitDto?)null);
        //    _mockVisitRepository.Setup(repo => repo.CreateVisitAsync(patientId, expectedInitialStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(newVisitId);

        //    // Act
        //    var result = await _patientService.CreateNewVisitAndCheckinAsync(patientId, performingUserId, "", "");

        //    // Assert
        //    Assert.True(result.Success);
        //    Assert.Null(result.ErrorMessage);
        //    _mockVisitRepository.Verify(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.CreateVisitAsync(patientId, expectedInitialStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        ///// <summary>
        ///// Tests that <see cref="PatientService.CreateNewVisitAndCheckinAsync"/>
        ///// fails and does not create a new visit if an active visit already exists for the patient.
        ///// </summary>
        //[Fact]
        //public async Task CreateNewVisitAndCheckinAsync_WhenActiveVisitExists_ShouldFailAndNotCreate()
        //{
        //    // Arrange
        //    int patientId = 102;
        //    int performingUserId = 2;
        //    var activeVisit = new ActiveVisitDto { VisitId = 77, Status = "In Treatment" };
        //    _mockVisitRepository.Setup(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(activeVisit);

        //    // Act
        //    var result = await _patientService.CreateNewVisitAndCheckinAsync(patientId, performingUserId, "", "");

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.False(result.Success);
        //    Assert.NotNull(result.ErrorMessage);
        //    Assert.Contains("active visit", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        //    Assert.Contains("already exists", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        //    _mockVisitRepository.Verify(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.CreateVisitAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
        //    _mockPatientRepository.VerifyNoOtherCalls();
        //}

        ///// <summary>
        ///// Tests that <see cref="PatientService.CreateNewVisitAndCheckinAsync"/>
        ///// returns a failure if the repository fails to create a new visit.
        ///// </summary>
        //[Fact]
        //public async Task CreateNewVisitAndCheckinAsync_WhenCreateVisitFailsInRepo_ShouldFail()
        //{
        //    // Arrange
        //    int patientId = 103;
        //    int performingUserId = 3;
        //    string expectedInitialStatus = "Waiting for Vitals";
        //    _mockVisitRepository.Setup(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync((ActiveVisitDto?)null);
        //    _mockVisitRepository.Setup(repo => repo.CreateVisitAsync(patientId, expectedInitialStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //                        .ThrowsAsync(new System.Data.DataException("DB Create Error"));

        //    // Act
        //    var result = await _patientService.CreateNewVisitAndCheckinAsync(patientId, performingUserId, "", "");

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.NotNull(result.ErrorMessage);
        //    Assert.Contains("Failed to create a new visit record", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        //    _mockVisitRepository.Verify(repo => repo.FindLatestActiveVisitAsync(patientId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.CreateVisitAsync(patientId, expectedInitialStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        #endregion

        #region ResumeActiveVisitAsync Tests

        /// <summary>
        /// Tests that <see cref="PatientService.ResumeActiveVisitAsync"/>
        /// successfully updates an existing visit's status when the repository reports success.
        /// </summary>
        //[Fact]
        //public async Task ResumeActiveVisitAsync_WhenUpdateSucceeds_ShouldReturnSuccess()
        //{
        //    // Arrange
        //    int visitId = 201;
        //    int patientId = 301;
        //    int performingUserId = 4;
        //    string expectedTargetStatus = "Waiting for Vitals";
        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(visitId, expectedTargetStatus, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(true);

        //    // Act
        //    var result = await _patientService.ResumeActiveVisitAsync(visitId, patientId, performingUserId);

        //    // Assert
        //    Assert.True(result.Success);
        //    Assert.Null(result.ErrorMessage);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(visitId, expectedTargetStatus, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        ///// <summary>
        ///// Tests that <see cref="PatientService.ResumeActiveVisitAsync"/>
        ///// returns a failure when the repository fails to update the visit status.
        ///// </summary>
        //[Fact]
        //public async Task ResumeActiveVisitAsync_WhenUpdateFailsInRepo_ShouldReturnFailure()
        //{
        //    // Arrange
        //    int visitId = 202;
        //    int patientId = 302;
        //    int performingUserId = 5;
        //    string expectedTargetStatus = "Waiting for Vitals";
        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(visitId, expectedTargetStatus, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(false);

        //    // Act
        //    var result = await _patientService.ResumeActiveVisitAsync(visitId, patientId, performingUserId);

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.NotNull(result.ErrorMessage);
        //    Assert.Contains("Failed to update status", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(visitId, expectedTargetStatus, null, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        #endregion

        #region CloseAndStartNewVisitAsync Tests

        /// <summary>
        /// Tests that <see cref="PatientService.CloseAndStartNewVisitAsync"/>
        /// successfully closes an old visit and creates a new one when all repository operations succeed.
        /// </summary>
        //[Fact]
        //public async Task CloseAndStartNewVisitAsync_WhenAllSucceeds_ShouldReturnSuccess()
        //{
        //    // Arrange
        //    int oldVisitId = 301;
        //    int patientId = 401;
        //    int performingUserId = 6;
        //    string adminClosedStatus = "Administratively Closed";
        //    string newVisitTargetStatus = "Waiting for Vitals";
        //    int newVisitId = 88;

        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(oldVisitId, adminClosedStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(true);
        //    _mockVisitRepository.Setup(repo => repo.CreateVisitAsync(patientId, newVisitTargetStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(newVisitId);

        //    // Act
        //    var result = await _patientService.CloseAndStartNewVisitAsync(oldVisitId, patientId, performingUserId);

        //    // Assert
        //    Assert.True(result.Success);
        //    Assert.Null(result.ErrorMessage);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(oldVisitId, adminClosedStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.CreateVisitAsync(patientId, newVisitTargetStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        /// <summary>
        /// Tests that <see cref="PatientService.CloseAndStartNewVisitAsync"/>
        /// returns a failure and does not create a new visit if closing the old visit fails.
        /// </summary>
        //[Fact]
        //public async Task CloseAndStartNewVisitAsync_WhenClosingOldVisitFails_ShouldReturnFailureAndNotCreateNew()
        //{
        //    // Arrange
        //    int oldVisitId = 302;
        //    int patientId = 402;
        //    int performingUserId = 7;
        //    string adminClosedStatus = "Administratively Closed";

        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(oldVisitId, adminClosedStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(false); // Simulate failure

        //    // Act
        //    var result = await _patientService.CloseAndStartNewVisitAsync(oldVisitId, patientId, performingUserId);

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.NotNull(result.ErrorMessage);
        //    Assert.Contains("Failed to close the existing active visit", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(oldVisitId, adminClosedStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.CreateVisitAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Never);
        //}

        ///// <summary>
        ///// Tests that <see cref="PatientService.CloseAndStartNewVisitAsync"/>
        ///// returns a failure if creating the new visit fails, even if closing the old visit succeeded.
        ///// </summary>
        //[Fact]
        //public async Task CloseAndStartNewVisitAsync_WhenCreatingNewVisitFails_ShouldReturnFailure()
        //{
        //    // Arrange
        //    int oldVisitId = 303;
        //    int patientId = 403;
        //    int performingUserId = 8;
        //    string adminClosedStatus = "Administratively Closed";
        //    string newVisitTargetStatus = "Waiting for Vitals";

        //    _mockVisitRepository.Setup(repo => repo.UpdateVisitStatusAsync(oldVisitId, adminClosedStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>())).ReturnsAsync(true); // Close succeeds
        //    _mockVisitRepository.Setup(repo => repo.CreateVisitAsync(patientId, newVisitTargetStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()))
        //                        .ThrowsAsync(new System.Data.DataException("DB Create Error")); // New visit create fails

        //    // Act
        //    var result = await _patientService.CloseAndStartNewVisitAsync(oldVisitId, patientId, performingUserId);

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.NotNull(result.ErrorMessage);
        //    // Corrected assertion to match the actual error message from PatientService.cs
        //    Assert.Contains("Successfully closed the old visit, but failed to create a new one", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        //    _mockVisitRepository.Verify(repo => repo.UpdateVisitStatusAsync(oldVisitId, adminClosedStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //    _mockVisitRepository.Verify(repo => repo.CreateVisitAsync(patientId, newVisitTargetStatus, performingUserId, It.IsAny<IDbConnection?>(), It.IsAny<IDbTransaction?>()), Times.Once);
        //}

        #endregion
    }
}
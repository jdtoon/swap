using Xunit.Abstractions;
using Dapper;
using carestream.core.interfaces.repositories;
using carestream.persistence.repositories;
using carestream.core.dtos.vitals;

namespace carestream.tests.integration.repositories
{
    /// <summary>
    /// Integration tests for the <see cref="VitalsRepository"/> using transactional rollback.
    /// </summary>
    public class VitalsRepositoryIntegrationTests : BaseIntegrationTest, IDisposable
    {
        private readonly IVitalsRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="VitalsRepositoryIntegrationTests"/> class.
        /// </summary>
        public VitalsRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new VitalsRepository(Configuration, GetMockLogger<VitalsRepository>(), GetCurrentFacilityContext());
            // Transaction is started by the base class constructor
        }

        /// <summary>
        /// Cleans up by rolling back the transaction after each test.
        /// </summary>
        //public void Dispose()
        //{
        //    // Transaction is rolled back by the base class Dispose method
        //    // which is called because this class implements IDisposable and xUnit calls it.
        //    // If BaseIntegrationTest itself handles IDisposable for transaction, this class might not need to implement it.
        //    // However, for clarity that each test runs in a transaction that is then disposed,
        //    // this explicit Dispose calling a protected RollbackTransaction (if it existed) or relying on base Dispose is fine.
        //    // Current BaseIntegrationTest handles transaction rollback in its Dispose.
        //    GC.SuppressFinalize(this);
        //}

        // --- Helper Methods to Seed Data within the Current Transaction ---
        private async Task<int> SeedUserAsync(int userSuffix)
        {
            string userForceNumber = $"U_VITALTEST_{userSuffix:D3}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            Fixture.Output?.WriteLine($"Seeding User for Vitals tests: {userForceNumber}");
            const string sql = @"
                INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active)
                VALUES (@ForceNumber, 'VitalsTestUserFN', 'UserLN', 'TestRankU', 'TestDeptU', TRUE)
                RETURNING user_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new { ForceNumber = userForceNumber }, transaction: Transaction);
        }

        private async Task<int> SeedPatientAsync(int patientSuffix, int forUserId)
        {
            string patientForceNumber = $"P_VITALTEST_{patientSuffix:D3}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            Fixture.Output?.WriteLine($"Seeding Patient for Vitals tests: {patientForceNumber}");
            const string sql = @"
                INSERT INTO app.patients (force_number, first_name, last_name, rank, date_of_birth, gender, user_id)
                VALUES (@ForceNumber, 'VitalsTestPatientFN', 'PatientLN', 'TestRankP', '1995-01-01', 'Female', @UserId)
                RETURNING patient_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new { ForceNumber = patientForceNumber, UserId = forUserId }, transaction: Transaction);
        }

        private async Task<int> SeedVisitForVitalsAsync(int patientId, int checkedInByUserId, string status = "Waiting for Vitals")
        {
            Fixture.Output?.WriteLine($"Seeding Visit for Vitals tests: PatientId={patientId}, Status={status}");
            const string sql = @"
                INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, visit_timestamp)
                VALUES (@PatientId, @Status, @CheckedInByUserId, NOW())
                RETURNING visit_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new
            {
                PatientId = patientId,
                Status = status,
                CheckedInByUserId = checkedInByUserId
            }, transaction: Transaction);
        }

        // --- Actual Test Methods ---
        [Fact]
        public async Task CreateVitalsRecordAsync_ShouldInsertRecordAndReturnNewId()
        {
            // Arrange
            int testUserId = await SeedUserAsync(1);
            int testPatientId = await SeedPatientAsync(1, testUserId);
            int testVisitId = await SeedVisitForVitalsAsync(testPatientId, testUserId);

            var vitalsData = new VitalsCaptureInputDto
            {
                VisitId = testVisitId,
                PatientId = testPatientId,
                BloodPressureSystolic = 120,
                BloodPressureDiastolic = 80,
                HeartRate = 72,
                Temperature = 36.6m,
                RespiratoryRate = 16,
                OxygenSaturation = 98,
                PainLevel = 2,
                UrinalysisColor = "Yellow",
                UrinalysisClarity = "Clear",
                UrinalysisSpecificGravity = 1.020m,
                UrinalysisPh = 6.0m,
                UrinalysisProtein = "Trace",
                UrinalysisGlucose = "Negative",
                ClinicalNotes = "Patient seems stable for vitals test.",
                RequiresFollowUp = false,
                MarkAsUrgent = false,
                RecordedAt = DateTimeOffset.UtcNow,
                RecordedByUserId = testUserId
            };

            // Act
            int newVitalsId = await _repository.CreateVitalsRecordAsync(vitalsData, connection: Connection, transaction: Transaction);

            // Assert
            Assert.True(newVitalsId > 0, "CreateVitalsRecordAsync should return a positive new vital_signs_id.");

            const string verificationSql = @"SELECT visit_id AS VisitId, patient_id AS PatientId, blood_pressure_systolic AS BloodPressureSystolic, clinical_notes AS ClinicalNotes, recorded_by_user_id AS RecordedByUserId, urinalysis_specific_gravity AS UrinalysisSpecificGravity FROM app.vital_signs WHERE vital_signs_id = @NewVitalsId";
            var createdVitals = await Connection.QuerySingleOrDefaultAsync<VitalsCaptureInputDto>(verificationSql, new { NewVitalsId = newVitalsId }, transaction: Transaction);

            Assert.NotNull(createdVitals);
            Assert.Equal(vitalsData.VisitId, createdVitals.VisitId);
            Assert.Equal(vitalsData.BloodPressureSystolic, createdVitals.BloodPressureSystolic);
            // Add more assertions as needed
        }

        [Fact]
        public async Task GetVitalsForVisitAsync_ShouldReturnVitals_WhenRecordExists()
        {
            // Arrange
            int testUserId = await SeedUserAsync(2);
            int testPatientId = await SeedPatientAsync(2, testUserId);
            int testVisitId = await SeedVisitForVitalsAsync(testPatientId, testUserId);

            var initialVitals = new VitalsCaptureInputDto
            {
                VisitId = testVisitId,
                PatientId = testPatientId,
                HeartRate = 80,
                Temperature = 37.0m,
                RecordedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                RecordedByUserId = testUserId,
                BloodPressureSystolic = 130,
                BloodPressureDiastolic = 75,
                RespiratoryRate = 18,
                OxygenSaturation = 97,
                PainLevel = 1
            };
            // Use repository method to create, which uses its own connection but within the test's overall transaction context
            // if the repository was passed the transaction.
            // For full control in test, better to seed directly if testing GetVitalsForVisitAsync
            const string insertSql = @"INSERT INTO app.vital_signs (visit_id, patient_id, heart_rate, temperature, recorded_at, recorded_by_user_id, blood_pressure_systolic, blood_pressure_diastolic, respiratory_rate, oxygen_saturation, pain_level) VALUES (@VisitId, @PatientId, @HeartRate, @Temperature, @RecordedAt, @RecordedByUserId, @BloodPressureSystolic, @BloodPressureDiastolic, @RespiratoryRate, @OxygenSaturation, @PainLevel)";
            await Connection.ExecuteAsync(insertSql, initialVitals, transaction: Transaction);


            // Act
            var retrievedVitals = await _repository.GetVitalsForVisitAsync(testVisitId, connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(retrievedVitals);
            Assert.Equal(testVisitId, retrievedVitals.VisitId);
            Assert.Equal(80, retrievedVitals.HeartRate);
            Assert.Equal(130, retrievedVitals.BloodPressureSystolic);
        }

        [Fact]
        public async Task GetVitalsForVisitAsync_ShouldReturnNull_WhenNoRecordExistsForVisit()
        {
            // Arrange
            int testUserId = await SeedUserAsync(3);
            int testPatientId = await SeedPatientAsync(3, testUserId);
            int visitIdWithNoVitals = await SeedVisitForVitalsAsync(testPatientId, testUserId);

            // Act
            var retrievedVitals = await _repository.GetVitalsForVisitAsync(visitIdWithNoVitals, connection: Connection, transaction: Transaction);

            // Assert
            Assert.Null(retrievedVitals);
        }

        [Fact]
        public async Task CreateVitalsRecordAsync_ShouldHandleNullOptionalFields()
        {
            // Arrange
            int testUserId = await SeedUserAsync(4);
            int testPatientId = await SeedPatientAsync(4, testUserId);
            int testVisitId = await SeedVisitForVitalsAsync(testPatientId, testUserId);

            var vitalsData = new VitalsCaptureInputDto
            {
                VisitId = testVisitId,
                PatientId = testPatientId,
                BloodPressureSystolic = 110,
                BloodPressureDiastolic = 70,
                HeartRate = 60,
                Temperature = 36.5m,
                RespiratoryRate = 14,
                OxygenSaturation = 99,
                RecordedAt = DateTimeOffset.UtcNow,
                RecordedByUserId = testUserId
            };

            // Act
            int newVitalsId = await _repository.CreateVitalsRecordAsync(vitalsData, connection: Connection, transaction: Transaction);

            // Assert
            Assert.True(newVitalsId > 0);
            const string verificationSql = @"SELECT pain_level AS PainLevel, urinalysis_color AS UrinalysisColor, clinical_notes AS ClinicalNotes FROM app.vital_signs WHERE vital_signs_id = @NewVitalsId";
            var createdVitals = await Connection.QuerySingleOrDefaultAsync<VitalsCaptureInputDto>(verificationSql, new { NewVitalsId = newVitalsId }, transaction: Transaction);
            Assert.NotNull(createdVitals);
            Assert.Null(createdVitals.PainLevel);
            Assert.Null(createdVitals.UrinalysisColor);
            Assert.Null(createdVitals.ClinicalNotes);
        }
    }
}
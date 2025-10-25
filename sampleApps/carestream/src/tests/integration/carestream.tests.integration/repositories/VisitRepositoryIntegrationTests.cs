using Xunit.Abstractions;
using Dapper;
using carestream.core.interfaces.repositories;
using carestream.persistence.repositories;

namespace carestream.tests.integration.repositories
{
    /// <summary>
    /// Integration tests for the <see cref="VisitRepository"/> using transactional rollback for isolation.
    /// </summary>
    public class VisitRepositoryIntegrationTests : BaseIntegrationTest, IDisposable
    {
        private readonly IVisitRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisitRepositoryIntegrationTests"/> class.
        /// </summary>
        public VisitRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new VisitRepository(Configuration, GetMockLogger<VisitRepository>(), GetCurrentFacilityContext());
        }

        // --- Helper Methods to Seed Data within the Current Transaction ---
        private async Task<int> SeedUserAsync(int userSuffix, string firstName = "TestUserFN", string lastName = "UserLN")
        {
            string userForceNumber = $"U_VISITTEST_{userSuffix:D3}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            Fixture.Output?.WriteLine($"Seeding user with ForceNumber: {userForceNumber}");
            const string sql = @"
                INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active)
                VALUES (@ForceNumber, @FirstName, @LastName, 'TestRank', 'TestDept', TRUE)
                RETURNING user_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new { ForceNumber = userForceNumber, firstName, lastName }, transaction: Transaction);
        }

        private async Task<int> SeedPatientAsync(int patientSuffix, int forUserId, string firstName = "TestPatientFN", string lastName = "PatientLN")
        {
            string patientForceNumber = $"P_VISITTEST_{patientSuffix:D3}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            Fixture.Output?.WriteLine($"Seeding patient with ForceNumber: {patientForceNumber} for UserId: {forUserId}");
            const string sql = @"
                INSERT INTO app.patients (force_number, first_name, last_name, rank, date_of_birth, gender, user_id)
                VALUES (@ForceNumber, @FirstName, @LastName, 'TestRankP', '1990-01-01', 'Other', @UserId)
                RETURNING patient_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new { ForceNumber = patientForceNumber, UserId = forUserId, firstName, lastName }, transaction: Transaction);
        }

        private async Task<int> SeedVisitAsync(int patientId, string status, int checkedInByUserId, DateTime? visitTimestamp = null, bool markAsUrgentInVitals = false, string? doctorNotes = null)
        {
            const string insertVisitSql = @"
                INSERT INTO app.visits (patient_id, status, checked_in_by_user_id, visit_timestamp, doctor_consultation_notes, assigned_officer_user_id)
                VALUES (@PatientId, @Status, @CheckedInByUserId, @VisitTimestamp, @DoctorNotes, @CheckedInByUserId)
                RETURNING visit_id;";
            int visitId = await Connection.ExecuteScalarAsync<int>(insertVisitSql, new
            {
                PatientId = patientId,
                Status = status,
                CheckedInByUserId = checkedInByUserId,
                VisitTimestamp = visitTimestamp ?? DateTime.UtcNow,
                DoctorNotes = doctorNotes // Added doctorNotes parameter
            }, transaction: Transaction);

            if (markAsUrgentInVitals)
            {
                const string insertVitalsSql = @"
                    INSERT INTO app.vital_signs (visit_id, patient_id, mark_as_urgent, recorded_by_user_id, recorded_at)
                    VALUES (@VisitId, @PatientId, TRUE, @RecordedByUserId, NOW());";
                await Connection.ExecuteAsync(insertVitalsSql, new { VisitId = visitId, PatientId = patientId, RecordedByUserId = checkedInByUserId }, transaction: Transaction);
            }
            return visitId;
        }

        private async Task<string?> GetDbVisitStatusAsync(int visitId)
        {
            const string selectSql = "SELECT status FROM app.visits WHERE visit_id = @VisitId";
            return await Connection.QuerySingleOrDefaultAsync<string?>(selectSql, new { VisitId = visitId }, transaction: Transaction);
        }

        // --- YOUR EXISTING TEST METHODS (Unchanged) ---

        //[Fact]
        //public async Task UpdateVisitStatusAsync_ShouldUpdateStatus_WhenVisitExists()
        //{
        //    // Arrange
        //    int testUserId = await SeedUserAsync(10);
        //    int testPatientId = await SeedPatientAsync(10, testUserId);
        //    int visitIdToUpdate = await SeedVisitAsync(testPatientId, "Pending Checkin", testUserId);
        //    string newStatus = "Waiting for Vitals";
        //    Assert.Equal("Pending Checkin", await GetDbVisitStatusAsync(visitIdToUpdate));

        //    // Act
        //    bool result = await _repository.UpdateVisitStatusAsync(visitIdToUpdate, newStatus, connection: Connection, transaction: Transaction);

        //    // Assert
        //    Assert.True(result, "Repository update method should return true on success.");
        //    Assert.Equal(newStatus, await GetDbVisitStatusAsync(visitIdToUpdate));
        //}

        //[Fact]
        //public async Task UpdateVisitStatusAsync_ShouldReturnFalse_WhenVisitDoesNotExist()
        //{
        //    // Arrange
        //    int nonExistentVisitId = -999;
        //    string newStatus = "Waiting for Vitals";

        //    // Act
        //    bool result = await _repository.UpdateVisitStatusAsync(nonExistentVisitId, newStatus, connection: Connection, transaction: Transaction);

        //    // Assert
        //    Assert.False(result, "Repository update method should return false if no rows were affected.");
        //}

        //[Fact]
        //public async Task UpdateVisitStatusAsync_ShouldUpdateAssignedOfficer_WhenProvided()
        //{
        //    // Arrange
        //    int doctorUserId = await SeedUserAsync(11);
        //    int patientAdminUserId = await SeedUserAsync(12);
        //    int testPatientId = await SeedPatientAsync(11, patientAdminUserId);
        //    int visitIdToUpdate = await SeedVisitAsync(testPatientId, "Waiting for Doctor", patientAdminUserId);
        //    string newStatus = "In Treatment";

        //    // Act
        //    bool result = await _repository.UpdateVisitStatusAsync(visitIdToUpdate, newStatus, doctorUserId, connection: Connection, transaction: Transaction);

        //    // Assert
        //    Assert.True(result);
        //    const string selectSql = "SELECT assigned_officer_user_id FROM app.visits WHERE visit_id = @VisitId";
        //    int? assignedOfficerAfterUpdate = await Connection.QuerySingleOrDefaultAsync<int?>(selectSql, new { VisitId = visitIdToUpdate }, transaction: this.Transaction);
        //    Assert.NotNull(assignedOfficerAfterUpdate);
        //    Assert.Equal(doctorUserId, assignedOfficerAfterUpdate.Value);
        //    Assert.Equal(newStatus, await GetDbVisitStatusAsync(visitIdToUpdate));
        //}

        //[Fact]
        //public async Task CreateVisitAsync_ShouldInsertNewVisitAndReturnItsId()
        //{
        //    // Arrange
        //    int performingUserId = await SeedUserAsync(13);
        //    int testPatientId = await SeedPatientAsync(13, performingUserId);
        //    string initialStatus = "Pending Checkin";

        //    // Act
        //    int newVisitId = await _repository.CreateVisitAsync(testPatientId, initialStatus, performingUserId, connection: Connection, transaction: Transaction);

        //    // Assert
        //    Assert.True(newVisitId > 0);
        //    var createdVisit = await Connection.QuerySingleOrDefaultAsync<dynamic>(
        //        "SELECT patient_id, status, checked_in_by_user_id FROM app.visits WHERE visit_id = @VisitId",
        //        new { VisitId = newVisitId }, transaction: this.Transaction
        //    );
        //    Assert.NotNull(createdVisit);
        //    Assert.Equal(testPatientId, (int)createdVisit!.patient_id);
        //    Assert.Equal(initialStatus, (string)createdVisit.status);
        //    Assert.Equal(performingUserId, (int)createdVisit.checked_in_by_user_id);
        //}

        [Fact]
        public async Task FindLatestActiveVisitAsync_ShouldReturnLatestActiveVisit_WhenOneExists()
        {
            // Arrange
            int testUserId = await SeedUserAsync(14);
            int patientIdForTest = await SeedPatientAsync(14, testUserId);
            await SeedVisitAsync(patientIdForTest, "Discharged", testUserId, DateTime.UtcNow.AddHours(-5));
            await SeedVisitAsync(patientIdForTest, "Waiting for Vitals", testUserId, DateTime.UtcNow.AddHours(-2));
            int expectedLatestActiveVisitId = await SeedVisitAsync(patientIdForTest, "In Treatment", testUserId, DateTime.UtcNow.AddHours(-1));
            await SeedVisitAsync(patientIdForTest, "Cancelled", testUserId, DateTime.UtcNow.AddMinutes(-30));

            // Act
            var result = await _repository.FindLatestActiveVisitAsync(patientIdForTest, connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedLatestActiveVisitId, result.VisitId);
            Assert.Equal("In Treatment", result.Status);
        }

        [Fact]
        public async Task FindLatestActiveVisitAsync_ShouldReturnNull_WhenOnlyTerminalVisitsExist()
        {
            // Arrange
            int testUserId = await SeedUserAsync(15);
            int patientIdForTest = await SeedPatientAsync(15, testUserId);
            await SeedVisitAsync(patientIdForTest, "Discharged", testUserId, DateTime.UtcNow.AddHours(-3));
            await SeedVisitAsync(patientIdForTest, "Cancelled", testUserId, DateTime.UtcNow.AddHours(-1));

            // Act
            var result = await _repository.FindLatestActiveVisitAsync(patientIdForTest, connection: Connection, transaction: Transaction);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindLatestActiveVisitAsync_ShouldReturnNull_WhenNoVisitsExistForPatient()
        {
            // Arrange
            int testUserId = await SeedUserAsync(16);
            int patientIdWithNoVisits = await SeedPatientAsync(16, testUserId);

            // Act
            var result = await _repository.FindLatestActiveVisitAsync(patientIdWithNoVisits, connection: Connection, transaction: Transaction);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDoctorDashboardStatsAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            int user1Id = await SeedUserAsync(701); int patient1Id = await SeedPatientAsync(701, user1Id);
            int user2Id = await SeedUserAsync(702); int patient2Id = await SeedPatientAsync(702, user2Id);
            int user3Id = await SeedUserAsync(703); int patient3Id = await SeedPatientAsync(703, user3Id);

            await SeedVisitAsync(patient1Id, "Ready for Doctor", user1Id);
            await SeedVisitAsync(patient2Id, "Ready for Doctor", user2Id);
            await SeedVisitAsync(patient3Id, "Waiting for Vitals", user3Id);

            // Act
            var stats = await _repository.GetDoctorDashboardStatsAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.TotalWaitingForDoctor);
        }

        [Fact]
        public async Task GetDoctorPatientQueueAsync_ShouldReturnPatientsReadyForDoctor_OrderedByPriorityAndTimestamp()
        {
            // Arrange
            int user1Id = await SeedUserAsync(801); int patient1Id = await SeedPatientAsync(801, user1Id);
            int user2Id = await SeedUserAsync(802); int patient2Id = await SeedPatientAsync(802, user2Id);
            int user3Id = await SeedUserAsync(803); int patient3Id = await SeedPatientAsync(803, user3Id);
            int user4Id = await SeedUserAsync(804); int patient4Id = await SeedPatientAsync(804, user4Id);

            DateTime timeBase = DateTime.UtcNow;
            await SeedVisitAsync(patient1Id, "Ready for Doctor", user1Id, timeBase.AddMinutes(-30)); // Normal
            await SeedVisitAsync(patient2Id, "Ready for Doctor", user2Id, timeBase.AddMinutes(-10), markAsUrgentInVitals: true); // Urgent
            await SeedVisitAsync(patient3Id, "Ready for Doctor", user3Id, timeBase.AddMinutes(-5));  // Normal
            await SeedVisitAsync(patient4Id, "Waiting for Vitals", user4Id, timeBase); // Not in queue

            // Act
            var queue = (await _repository.GetDoctorPatientQueueAsync(connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(queue);
            Assert.Equal(3, queue.Count);
            Assert.Equal(patient2Id, queue[0].PatientId); Assert.Equal("Urgent", queue[0].Priority); // Urgent patient first
            // The next two normal priority patients will be ordered by visit_timestamp (older first from GetDoctorPatientQueueAsync SQL)
            Assert.Equal(patient1Id, queue[1].PatientId); Assert.Equal("Normal", queue[1].Priority); // -30 mins
            Assert.Equal(patient3Id, queue[2].PatientId); Assert.Equal("Normal", queue[2].Priority); // -5 mins
        }

        [Fact]
        public async Task GetDoctorPatientQueueAsync_ShouldReturnEmpty_WhenNoPatientsReadyForDoctor()
        {
            // Arrange
            int userId = await SeedUserAsync(951);
            int patientId = await SeedPatientAsync(951, userId);
            await SeedVisitAsync(patientId, "Waiting for Vitals", userId);

            // Act
            var queue = await _repository.GetDoctorPatientQueueAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(queue);
            Assert.Empty(queue);
        }

        // --- NEW TESTS FOR DOCTOR NOTES ---

        [Fact]
        public async Task UpdateDoctorNotesAsync_ShouldUpdateNotes_WhenVisitExists()
        {
            // Arrange
            int userId = await SeedUserAsync(201);
            int patientId = await SeedPatientAsync(201, userId);
            int visitId = await SeedVisitAsync(patientId, "Consultation In Progress", userId);
            string newNotes = "Patient reports feeling better. Continue current treatment.";

            // Act
            bool result = await _repository.UpdateDoctorNotesAsync(visitId, newNotes, Connection, Transaction);

            // Assert
            Assert.True(result, "UpdateDoctorNotesAsync should return true on successful update.");
            const string selectNotesSql = "SELECT doctor_consultation_notes FROM app.visits WHERE visit_id = @VisitId";
            string? notesAfterUpdate = await Connection.QuerySingleOrDefaultAsync<string?>(selectNotesSql, new { VisitId = visitId }, transaction: Transaction);
            Assert.Equal(newNotes, notesAfterUpdate);
        }

        [Fact]
        public async Task UpdateDoctorNotesAsync_ShouldSetNotesToNull_WhenNullIsPassed()
        {
            // Arrange
            int userId = await SeedUserAsync(202);
            int patientId = await SeedPatientAsync(202, userId);
            int visitId = await SeedVisitAsync(patientId, "Consultation In Progress", userId, doctorNotes: "Initial notes here.");
            string? newNotes = null;

            // Act
            bool result = await _repository.UpdateDoctorNotesAsync(visitId, newNotes, Connection, Transaction);

            // Assert
            Assert.True(result);
            const string selectNotesSql = "SELECT doctor_consultation_notes FROM app.visits WHERE visit_id = @VisitId";
            string? notesAfterUpdate = await Connection.QuerySingleOrDefaultAsync<string?>(selectNotesSql, new { VisitId = visitId }, transaction: Transaction);
            Assert.Null(notesAfterUpdate);
        }

        [Fact]
        public async Task UpdateDoctorNotesAsync_ShouldReturnFalse_WhenVisitDoesNotExist()
        {
            // Arrange
            int nonExistentVisitId = -99;
            string notes = "These notes won't be saved.";

            // Act
            bool result = await _repository.UpdateDoctorNotesAsync(nonExistentVisitId, notes, Connection, Transaction);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetDoctorNotesAsync_ShouldReturnNotes_WhenExist()
        {
            // Arrange
            int userId = await SeedUserAsync(203);
            int patientId = await SeedPatientAsync(203, userId);
            string expectedNotes = "Patient is stable. Plan for discharge tomorrow.";
            int visitId = await SeedVisitAsync(patientId, "In Treatment", userId, doctorNotes: expectedNotes);

            // Act
            string? actualNotes = await _repository.GetDoctorNotesAsync(visitId, Connection, Transaction);

            // Assert
            Assert.Equal(expectedNotes, actualNotes);
        }

        [Fact]
        public async Task GetDoctorNotesAsync_ShouldReturnNull_WhenNotesAreNullInDb()
        {
            // Arrange
            int userId = await SeedUserAsync(204);
            int patientId = await SeedPatientAsync(204, userId);
            int visitId = await SeedVisitAsync(patientId, "In Treatment", userId, doctorNotes: null); // Explicitly seed null notes

            // Act
            string? actualNotes = await _repository.GetDoctorNotesAsync(visitId, Connection, Transaction);

            // Assert
            Assert.Null(actualNotes);
        }

        [Fact]
        public async Task GetDoctorNotesAsync_ShouldReturnNull_WhenVisitDoesNotExist()
        {
            // Arrange
            int nonExistentVisitId = -98;

            // Act
            string? actualNotes = await _repository.GetDoctorNotesAsync(nonExistentVisitId, Connection, Transaction);

            // Assert
            Assert.Null(actualNotes);
        }

        // *** NEW TESTS FOR GetBasicVisitInfoByIdAsync ***
        [Fact]
        public async Task GetBasicVisitInfoByIdAsync_ShouldReturnCorrectInfo_WhenVisitExists()
        {
            // Arrange
            int userId = await SeedUserAsync(301);
            int patientId = await SeedPatientAsync(301, userId);
            string expectedStatus = "Waiting for Vitals";
            string expectedReason = "Flu symptoms";
            DateTime visitTime = DateTime.UtcNow.AddHours(-1);
            int visitId = await SeedVisitAsync(patientId, expectedStatus, userId, visitTimestamp: visitTime, doctorNotes: "Initial assessment pending.");
            // Manually update brief_reason as SeedVisitAsync doesn't set it yet
            await Connection.ExecuteAsync("UPDATE app.visits SET brief_reason = @Reason WHERE visit_id = @VisitId", new { Reason = expectedReason, VisitId = visitId }, transaction: Transaction);


            // Act
            var result = await _repository.GetBasicVisitInfoByIdAsync(visitId, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(visitId, result.VisitId);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedReason, result.BriefReason);
            Assert.Equal(userId, result.AssignedOfficerUserId); // SeedVisitAsync sets checked_in_by_user_id, UpdateVisitStatusAsync sets assigned_officer_user_id.
                                                                // For this test, let's assume GetBasicVisitInfoByIdAsync returns checked_in_by_user_id as AssignedOfficerUserId if assigned_officer_user_id is null.
                                                                // OR update seed to also set assigned_officer_user_id
                                                                // OR ensure GetBasicVisitInfoByIdAsync selects COALESCE(assigned_officer_user_id, checked_in_by_user_id)
                                                                // For timestamp, allow a small delta due to potential precision differences
            Assert.True((visitTime - result.VisitTimestamp).Duration() < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetBasicVisitInfoByIdAsync_ShouldReturnNull_WhenVisitDoesNotExist()
        {
            // Arrange
            int nonExistentVisitId = -100;

            // Act
            var result = await _repository.GetBasicVisitInfoByIdAsync(nonExistentVisitId, Connection, Transaction);

            // Assert
            Assert.Null(result);
        }

        //[Fact]
        //public async Task UpdateVisitStatusAsync_ShouldSetStatusToDischarged_AndAssignOfficer()
        //{
        //    // Arrange
        //    int doctorUserId = await SeedUserAsync(301, "Finalizing", "Doc"); // Helper method from your test class
        //    int patientAdminUserId = await SeedUserAsync(302, "CheckingIn", "Admin");
        //    int testPatientId = await SeedPatientAsync(301, patientAdminUserId, "ToDischarge", "Patient");
        //    int visitIdToUpdate = await SeedVisitAsync(testPatientId, "Consultation In Progress", patientAdminUserId);
        //    string newStatus = "Discharged";

        //    // Act
        //    bool result = await _repository.UpdateVisitStatusAsync(visitIdToUpdate, newStatus, doctorUserId, Connection, Transaction);

        //    // Assert
        //    Assert.True(result);

        //    var updatedVisit = await Connection.QuerySingleOrDefaultAsync<dynamic>(
        //        "SELECT status, assigned_officer_user_id FROM app.visits WHERE visit_id = @VisitId",
        //        new { VisitId = visitIdToUpdate },
        //        transaction: Transaction
        //    );
        //    Assert.NotNull(updatedVisit);
        //    Assert.Equal(newStatus, (string)updatedVisit.status);
        //    Assert.Equal(doctorUserId, (int?)updatedVisit.assigned_officer_user_id);
        //}
    }
}
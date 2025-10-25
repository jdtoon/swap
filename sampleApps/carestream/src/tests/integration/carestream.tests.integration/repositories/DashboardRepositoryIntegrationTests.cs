using Xunit.Abstractions;
using Dapper;
using carestream.core.interfaces.repositories;
using carestream.persistence.repositories;

namespace carestream.tests.integration.repositories
{
    /// <summary>
    /// Integration tests for the <see cref="DashboardRepository"/> using transactional rollback.
    /// </summary>
    public class DashboardRepositoryIntegrationTests : BaseIntegrationTest // Inherits from BaseIntegrationTest
    {
        private readonly IDashboardRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardRepositoryIntegrationTests"/> class.
        /// </summary>
        public DashboardRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output) // Pass fixture and output to base constructor
        {
            // Repository is instantiated using Configuration and GetMockLogger from BaseIntegrationTest
            _repository = new DashboardRepository(Configuration, GetMockLogger<DashboardRepository>(), GetCurrentFacilityContext());
            // Transaction is begun by the base class's constructor or test setup logic now.
            // If using BaseIntegrationTest that handles transactions in its constructor/Dispose:
            // No BeginTransaction() call needed here if base handles it.
            // If base only provides Connection, and derived implements IDisposable:
            // BeginTransaction(); // Call here
        }

        // If derived class implements IDisposable for per-test transaction:
        // public void Dispose()
        // {
        //     RollbackTransaction(); // Call base method to roll back
        //     GC.SuppressFinalize(this);
        // }


        // --- Helper Methods to Seed Data for Tests (using Connection and Transaction from BaseIntegrationTest) ---
        private async Task<int> SeedUserAsync(string forceNumberSuffix, string firstName, string lastName, string rank = "Pte", string department = "General")
        {
            string forceNumber = $"U_DASH_{forceNumberSuffix}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            Fixture.Output?.WriteLine($"Seeding User for Dashboard tests: {forceNumber}");
            const string sql = @"
                INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active)
                VALUES (@ForceNumber, @FirstName, @LastName, @Rank, @Department, TRUE)
                RETURNING user_id;";
            // Use Connection and Transaction from the base class
            return await Connection.ExecuteScalarAsync<int>(sql,
                new { ForceNumber = forceNumber, FirstName = firstName, LastName = lastName, Rank = rank, Department = department },
                transaction: Transaction); // Use 'Transaction' from base
        }

        private async Task<int> SeedPatientAsync(string forceNumberSuffix, string firstName, string lastName, string rank = "Pte", DateTime? dob = null)
        {
            string forceNumber = $"P_DASH_{forceNumberSuffix}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            Fixture.Output?.WriteLine($"Seeding Patient for Dashboard tests: {forceNumber}");
            const string sql = @"
                INSERT INTO app.patients (force_number, first_name, last_name, rank, date_of_birth, gender)
                VALUES (@ForceNumber, @FirstName, @LastName, @Rank, @DateOfBirth, 'Other')
                RETURNING patient_id;";
            return await Connection.ExecuteScalarAsync<int>(sql,
                new { ForceNumber = forceNumber, FirstName = firstName, LastName = lastName, Rank = rank, DateOfBirth = dob ?? new DateTime(1990, 1, 1) },
                transaction: Transaction);
        }

        private async Task<int> SeedVisitAsync(int patientId, int checkedInByUserId, string status, string briefReason, DateTime visitTimestamp)
        {
            const string sql = @"
                INSERT INTO app.visits (patient_id, checked_in_by_user_id, status, brief_reason, visit_timestamp)
                VALUES (@PatientId, @CheckedInByUserId, @Status, @BriefReason, @VisitTimestamp)
                RETURNING visit_id;";
            return await Connection.ExecuteScalarAsync<int>(sql,
                new { PatientId = patientId, CheckedInByUserId = checkedInByUserId, Status = status, BriefReason = briefReason, VisitTimestamp = visitTimestamp },
                transaction: Transaction);
        }

        private async Task<int> SeedStaffReportAsync(int authorUserId, string title, string department, string priority, DateTime timestamp)
        {
            const string sql = @"
                INSERT INTO app.staff_reports (author_user_id, title, department, priority, content, created_at)
                VALUES (@AuthorUserId, @Title, @Department, @Priority, 'Test Content for Dashboard', @Timestamp)
                RETURNING report_id;";
            return await Connection.ExecuteScalarAsync<int>(sql,
                new { AuthorUserId = authorUserId, Title = title, Department = department, Priority = priority, Timestamp = timestamp },
                transaction: Transaction);
        }


        [Fact]
        public async Task GetRecentPatientsAsync_ShouldReturnInsertedPatients_OrderedByTimestamp()
        {
            // Arrange
            int userId1 = await SeedUserAsync("DRP01", "TestDash", "User1");
            int patientId1 = await SeedPatientAsync("DRP01", "SarahDash", "Connor");
            int patientId2 = await SeedPatientAsync("DRP02", "ThomasDash", "Anderson");
            int patientId3 = await SeedPatientAsync("DRP03", "JohnDash", "Wick");

            await SeedVisitAsync(patientId1, userId1, "In Treatment", "Headache", DateTime.UtcNow.AddHours(-1));
            await SeedVisitAsync(patientId2, userId1, "Discharged", "Checkup", DateTime.UtcNow.AddHours(-2));
            await SeedVisitAsync(patientId3, userId1, "Pending Checkin", "Fever", DateTime.UtcNow.AddMinutes(-30));

            // Act
            // Pass the connection and transaction from the base test class
            var result = (await _repository.GetRecentPatientsAsync(limit: 5, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(patientId3, result[0].PatientId);
            Assert.Equal(patientId1, result[1].PatientId);
            Assert.Equal(patientId2, result[2].PatientId);
        }

        [Fact]
        public async Task GetRecentStaffReportsAsync_ShouldReturnInsertedReports_OrderedByTimestamp()
        {
            // Arrange
            int userId1 = await SeedUserAsync("DRSR01", "JamesDash", "Wilson");
            int userId2 = await SeedUserAsync("DRSR02", "SarahDash", "Mitchell");
            await SeedStaffReportAsync(userId1, "Daily Summary Dash", "Medicine", "Medium", DateTime.UtcNow.AddHours(-1));
            await SeedStaffReportAsync(userId2, "Emergency Update Dash", "Emergency", "High", DateTime.UtcNow.AddHours(-2));
            await SeedStaffReportAsync(userId1, "Supply Request Dash", "Logistics", "Low", DateTime.UtcNow.AddMinutes(-30));

            // Act
            var result = (await _repository.GetRecentStaffReportsAsync(limit: 3, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Supply Request Dash", result[0].Title);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnCorrectCounts_FromInsertedData()
        {
            // Arrange
            int userId = await SeedUserAsync("DDS01", "TestDash", "User");
            int p1 = await SeedPatientAsync("DDS01", "P", "1"); int p2 = await SeedPatientAsync("DDS02", "P", "2");
            int p3 = await SeedPatientAsync("DDS03", "P", "3"); int p4 = await SeedPatientAsync("DDS04", "P", "4");
            int p5 = await SeedPatientAsync("DDS05", "P", "5");

            await SeedVisitAsync(p1, userId, "In Treatment", "R A", DateTime.UtcNow.AddHours(-1));
            await SeedVisitAsync(p2, userId, "In Treatment", "R B", DateTime.UtcNow.AddHours(-2));
            await SeedVisitAsync(p3, userId, "Pending Checkin", "R C", DateTime.UtcNow.AddHours(-3));
            await SeedVisitAsync(p4, userId, "Discharged", "R D", DateTime.UtcNow.AddHours(-4));
            await SeedVisitAsync(p5, userId, "Waiting for Vitals", "R E", DateTime.UtcNow.AddHours(-5));

            // Act
            var result = await _repository.GetDashboardStatsAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalSickBayVisits); // Query counts all visits
            Assert.Equal(2, result.CurrentlyInTreatment);
            Assert.Equal(1, result.PendingCheckin);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ShouldReturnZeros_WhenNoRelevantVisits()
        {
            // Arrange
            int userId = await SeedUserAsync("DDS02", "TestDash", "UserN");
            int p1 = await SeedPatientAsync("DDS06", "P", "Six");
            await SeedVisitAsync(p1, userId, "Discharged", "R X", DateTime.UtcNow.AddHours(-1));

            // Act
            var result = await _repository.GetDashboardStatsAsync(connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalSickBayVisits); // Still counts this one visit
            Assert.Equal(0, result.CurrentlyInTreatment);
            Assert.Equal(0, result.PendingCheckin);
        }
    }
}
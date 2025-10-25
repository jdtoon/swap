using Xunit.Abstractions;
using Dapper;
using carestream.core.interfaces.repositories;
using carestream.persistence.repositories;

namespace carestream.tests.integration.repositories
{
    /// <summary>
    /// Integration tests for the <see cref="PatientRepository"/> using transactional rollback.
    /// </summary>
    public class PatientRepositoryIntegrationTests : BaseIntegrationTest, IDisposable
    {
        private readonly IPatientRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientRepositoryIntegrationTests"/> class.
        /// </summary>
        public PatientRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new PatientRepository(Configuration, GetMockLogger<PatientRepository>(), GetCurrentFacilityContext());
        }

        private async Task<(int PatientId, string ForceNumber)> SeedPatientAsync(string forceNumberSuffix, string firstName, string lastName, string rank = "Pte", DateTime? dob = null, int? associatedUserId = null)
        {
            string forceNumber = $"P_PATTEST_{forceNumberSuffix}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            Fixture.Output?.WriteLine($"Seeding Patient for PatientRepo tests: {forceNumber}");
            const string sql = @"
                INSERT INTO app.patients (force_number, first_name, last_name, rank, date_of_birth, gender, user_id)
                VALUES (@ForceNumber, @FirstName, @LastName, @Rank, @DateOfBirth, 'Other', @AssociatedUserId)
                RETURNING patient_id;";
            int patientId = await Connection.ExecuteScalarAsync<int>(sql,
                new { ForceNumber = forceNumber, FirstName = firstName, LastName = lastName, Rank = rank, DateOfBirth = dob ?? new DateTime(1990, 1, 1), AssociatedUserId = associatedUserId },
                transaction: this.Transaction);
            return (patientId, forceNumber);
        }
        private async Task<int> SeedUserAsync(string forceNumberSuffix) // Simplified user seed for this context
        {
            string forceNumber = $"U_PATTEST_{forceNumberSuffix}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            const string sql = @"
                INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active)
                VALUES (@ForceNumber, 'UserFN', 'UserLN', 'RankU', 'DeptU', TRUE)
                RETURNING user_id;";
            return await Connection.ExecuteScalarAsync<int>(sql, new { ForceNumber = forceNumber }, transaction: this.Transaction);
        }


        // --- FindByForceNumberAsync Tests ---
        [Theory]
        [InlineData("James", "Wilson", "Pte")]
        [InlineData("Sarah", "Connor", "Sgt")]
        public async Task FindByForceNumberAsync_ShouldReturnCorrectPatient_WhenExists(string firstName, string lastName, string rank)
        {
            // Arrange
            // Seed a patient with a unique force number for this test
            var (_, forceNumber) = await SeedPatientAsync("FNTest", firstName, lastName, rank);

            // Act
            var result = await _repository.FindByForceNumberAsync(forceNumber, connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(forceNumber, result.ForceNumber);
            Assert.Equal(firstName, result.FirstName);
            Assert.Equal(lastName, result.LastName);
            Assert.Equal(rank, result.Rank);
        }

        [Fact]
        public async Task FindByForceNumberAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            string nonExistentForceNumber = $"X_PATTEST_{Guid.NewGuid():N}";

            // Act
            var result = await _repository.FindByForceNumberAsync(nonExistentForceNumber, connection: Connection, transaction: Transaction);

            // Assert
            Assert.Null(result);
        }

        // --- GetPatientBasicInfoByIdAsync Tests ---
        [Fact]
        public async Task GetPatientBasicInfoByIdAsync_ShouldReturnCorrectInfo_WhenExists()
        {
            // Arrange
            int testUserId = await SeedUserAsync("UBI01");
            var (patientId, _) = await SeedPatientAsync("PBI01", "BasicInfo", "Patient", "Cpl", null, testUserId);

            // Act
            var result = await _repository.GetPatientBasicInfoByIdAsync(patientId, connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("BasicInfo", result.FirstName);
            Assert.Equal("Patient", result.LastName);
            Assert.Equal("Cpl", result.Rank);
        }

        [Fact]
        public async Task GetPatientBasicInfoByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            int nonExistentPatientId = -99999; // An ID that should not exist

            // Act
            var result = await _repository.GetPatientBasicInfoByIdAsync(nonExistentPatientId, connection: Connection, transaction: Transaction);

            // Assert
            Assert.Null(result);
        }
    }
}
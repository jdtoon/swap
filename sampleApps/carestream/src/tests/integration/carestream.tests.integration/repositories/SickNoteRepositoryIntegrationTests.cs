using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System;
using Dapper;
using carestream.persistence.repositories;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.consultation;

namespace carestream.tests.integration.repositories
{
    public class SickNoteRepositoryIntegrationTests : BaseIntegrationTest
    {
        private readonly ISickNoteRepository _repository;
        private int _testUserId;
        private int _testPatientId;
        private int _testVisitId;

        public SickNoteRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new SickNoteRepository(Configuration, GetMockLogger<SickNoteRepository>(), GetCurrentFacilityContext());
            SetupTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestDataAsync()
        {
            _testUserId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.users (force_number, first_name, last_name) VALUES (@FN, 'SickNote', 'User') RETURNING user_id;",
                new { FN = $"U_SNTEST_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testPatientId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.patients (force_number, first_name, last_name) VALUES (@FN, 'SickNote', 'Patient') RETURNING patient_id;",
                 new { FN = $"P_SNTEST_{Guid.NewGuid().ToString("N").Substring(0, 4)}" }, transaction: Transaction);

            _testVisitId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id) VALUES (@PatientId, 'Consultation In Progress', @UserId) RETURNING visit_id;",
                new { PatientId = _testPatientId, UserId = _testUserId }, transaction: Transaction);
            Fixture.Output?.WriteLine($"Setup SickNote Tests: VisitId={_testVisitId}, UserId={_testUserId}");
        }

        private SickNoteInputDto CreateSampleSickNoteDto(int visitId, int? sickNoteId = null)
        {
            return new SickNoteInputDto
            {
                SickNoteId = sickNoteId,
                VisitId = visitId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(2),
                Diagnosis = "Viral Infection",
                Recommendations = "Rest and fluids."
            };
        }

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

        [Fact]
        public async Task CreateSickNoteAsync_ShouldInsertAndReturnData()
        {
            // Arrange
            var inputDto = CreateSampleSickNoteDto(_testVisitId);

            // Act
            var result = await _repository.CreateSickNoteAsync(inputDto, _testUserId, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.SickNoteId.HasValue && result.SickNoteId > 0);
            Assert.Equal(inputDto.VisitId, result.VisitId);
            Assert.Equal(inputDto.Diagnosis, result.Diagnosis);
            Assert.NotNull(result.IssuedAt);
            Assert.Contains("SickNote", result.IssuedByUserName); // Check if user name was populated

            var dbNote = await Connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT * FROM app.sick_notes WHERE sick_note_id = @Id", new { Id = result.SickNoteId }, transaction: Transaction);
            Assert.NotNull(dbNote);
            Assert.Equal(inputDto.Recommendations, dbNote.recommendations);
        }

        [Fact]
        public async Task GetSickNoteByVisitIdAsync_ShouldReturnExistingNote()
        {
            // Arrange
            var inputDto = CreateSampleSickNoteDto(_testVisitId);
            var createdNote = await _repository.CreateSickNoteAsync(inputDto, _testUserId, Connection, Transaction);
            Assert.NotNull(createdNote);

            // Act
            var result = await _repository.GetSickNoteByVisitIdAsync(_testVisitId, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdNote.SickNoteId, result.SickNoteId);
            Assert.Equal(inputDto.Diagnosis, result.Diagnosis);
            Assert.Contains("SickNote User", result.IssuedByUserName);
        }

        [Fact]
        public async Task GetSickNoteByVisitIdAsync_ShouldReturnNull_WhenNoNoteExists()
        {
            // Arrange
            int visitWithNoNoteId = await Connection.ExecuteScalarAsync<int>(
                "INSERT INTO app.visits (patient_id, status, checked_in_by_user_id) VALUES (@PatientId, 'New', @UserId) RETURNING visit_id;",
                new { PatientId = _testPatientId, UserId = _testUserId }, transaction: Transaction);

            // Act
            var result = await _repository.GetSickNoteByVisitIdAsync(visitWithNoNoteId, Connection, Transaction);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSickNoteAsync_ShouldUpdateExistingNote()
        {
            // Arrange
            var initialDto = CreateSampleSickNoteDto(_testVisitId);
            var createdNote = await _repository.CreateSickNoteAsync(initialDto, _testUserId, Connection, Transaction);
            Assert.NotNull(createdNote);
            Assert.NotNull(createdNote.SickNoteId);

            var updateDto = new SickNoteInputDto
            {
                SickNoteId = createdNote.SickNoteId,
                VisitId = _testVisitId,
                StartDate = createdNote.StartDate?.AddDays(1),
                EndDate = createdNote.EndDate?.AddDays(1),
                Diagnosis = "Updated Diagnosis",
                Recommendations = "Updated Recommendations"
            };
            int updatingUserId = await SeedUserAsync(999, "Update", "User");


            // Act
            var result = await _repository.UpdateSickNoteAsync(updateDto, updatingUserId, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.SickNoteId, result.SickNoteId);
            Assert.Equal(updateDto.Diagnosis, result.Diagnosis);
            Assert.Equal(updateDto.Recommendations, result.Recommendations);
            Assert.Contains("Update User", result.IssuedByUserName); // Check updated user

            var dbNote = await Connection.QuerySingleAsync<dynamic>(
                "SELECT diagnosis, recommendations, issued_by_user_id FROM app.sick_notes WHERE sick_note_id = @Id",
                new { Id = result.SickNoteId }, transaction: Transaction);
            Assert.Equal("Updated Diagnosis", dbNote.diagnosis);
            Assert.Equal(updatingUserId, dbNote.issued_by_user_id);
        }

        [Fact]
        public async Task UpdateSickNoteAsync_ShouldReturnNull_IfNoteIdInvalid()
        {
            // Arrange
            var updateDto = CreateSampleSickNoteDto(_testVisitId, sickNoteId: -1); // Invalid SickNoteId

            // Act
            var result = await _repository.UpdateSickNoteAsync(updateDto, _testUserId, Connection, Transaction);

            // Assert
            Assert.Null(result); // Repository currently returns null if update fails (0 rows affected or bad ID)
        }
    }
}
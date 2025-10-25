using Xunit.Abstractions;
using Dapper;
using carestream.core.interfaces.repositories;
using carestream.persistence.repositories;
using carestream.core.dtos.admin;

namespace carestream.tests.integration.repositories
{
    /// <summary>
    /// Integration tests for the <see cref="UserRepository"/> using transactional rollback.
    /// </summary>
    public class UserRepositoryIntegrationTests : BaseIntegrationTest, IDisposable
    {
        private readonly IUserRepository _repository;
        private int _seededUserId1;
        private int _seededUserId2;
        private int _seededUserId3;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepositoryIntegrationTests"/> class.
        /// </summary>
        public UserRepositoryIntegrationTests(DatabaseTestFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _repository = new UserRepository(Configuration, GetMockLogger<UserRepository>(), GetCurrentFacilityContext());
            SeedInitialUsersAsync().GetAwaiter().GetResult();
        }

        private async Task SeedInitialUsersAsync()
        {
            // Clear any existing users from app.users to ensure clean state for these tests,
            // as other test classes might also seed users.
            // Alternatively, ensure unique force_numbers for each test class/method.
            // For simplicity now, we'll assume this test class wants a specific set.
            // await Connection.ExecuteAsync("DELETE FROM app.staff_reports; DELETE FROM app.visits; DELETE FROM app.patients; DELETE FROM app.users;", transaction: Transaction);


            _seededUserId1 = await Connection.ExecuteScalarAsync<int>(
                @"INSERT INTO app.users (force_number, first_name, last_name, rank, department, logto_sub, is_active)
                  VALUES ('ADM001', 'Alice', 'Admin', 'SysAdmin', 'IT', 'logto|admin1', TRUE) RETURNING user_id;",
                transaction: Transaction);
            _seededUserId2 = await Connection.ExecuteScalarAsync<int>(
                @"INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active)
                  VALUES ('USR002', 'Bob', 'User', 'Staff', 'Ops', FALSE) RETURNING user_id;", // Inactive user
                transaction: Transaction);
            _seededUserId3 = await Connection.ExecuteScalarAsync<int>(
                @"INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active)
                  VALUES ('LNK003', 'Carol', 'Linkable', 'Clerk', 'Records', TRUE) RETURNING user_id;", // User to be linked
                transaction: Transaction);
            Fixture.Output?.WriteLine($"Seeded Users for UserRepositoryTests: User1={_seededUserId1}, User2={_seededUserId2}, User3={_seededUserId3}");
        }

        [Fact]
        public async Task GetAllUsersForAdminAsync_ShouldReturnAllSeededUsers_WhenNoSearchTerm()
        {
            // Act
            var results = (await _repository.GetAllUsersForAdminAsync(connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.True(results.Count >= 3); // At least the 3 seeded users
            Assert.Contains(results, u => u.UserId == _seededUserId1 && u.FullName == "Alice Admin");
            Assert.Contains(results, u => u.UserId == _seededUserId2 && u.FullName == "Bob User" && !u.IsActive);
            Assert.Contains(results, u => u.UserId == _seededUserId3 && u.FullName == "Carol Linkable" && u.LogtoSub == null);
        }

        [Theory]
        [InlineData("Alice", 1, "Alice Admin")] // SearchTerm, ExpectedCount, ExpectedPartOfFullNameIfFound
        [InlineData("adm001", 1, "Alice Admin")]
        [InlineData("User", 1, "Bob User")]    // Matches Bob User by last name
        [InlineData("Ops", 1, "Bob User")]     // Matches Bob User by department
        [InlineData("NonExistent", 0, null)]
        public async Task GetAllUsersForAdminAsync_WithSearchTerm_ShouldReturnMatchingUsers(string searchTerm, int expectedCount, string? expectedPartOfFullName)
        {
            // Arrange: Data is seeded in SeedInitialUsersAsync

            // Act
            var results = (await _repository.GetAllUsersForAdminAsync(searchTerm: searchTerm, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(expectedCount, results.Count);

            if (expectedCount > 0 && results.Any())
            {
                // Check that each returned user matches the search term in at least one relevant field
                foreach (var user in results)
                {
                    bool matchesSearchTerm =
                        (user.FullName?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) == true) ||
                        (user.ForceNumber?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) == true) ||
                        (user.Department?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) == true) ||
                        // If your SQL also searches parts of the name individually:
                        (user.FullName?.Split(' ')[0].Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) == true) || // First name
                        (user.FullName?.Split(' ').Length > 1 && user.FullName?.Split(' ')[1].Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) == true); // Last name

                    Assert.True(matchesSearchTerm, $"User '{user.FullName}' (ForceNo: {user.ForceNumber}, Dept: {user.Department}) was returned but does not appear to match search term '{searchTerm}'.");

                    // If a specific name is expected for a given search term (like in your inline data),
                    // you can add a more targeted check here if only one result is expected.
                    if (expectedCount == 1 && !string.IsNullOrEmpty(expectedPartOfFullName))
                    {
                        Assert.Contains(expectedPartOfFullName, user.FullName, StringComparison.InvariantCultureIgnoreCase);
                    }
                }
            }
        }

        [Fact]
        public async Task GetAllUsersForAdminAsync_ShouldRespectPagination()
        {
            // Arrange: Ensure more users than pageSize exist for a broader search
            await Connection.ExecuteScalarAsync<int>(@"INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active) VALUES ('PG004', 'David', 'PageTest', 'Mgr', 'IT', TRUE) RETURNING user_id;", transaction: Transaction);
            await Connection.ExecuteScalarAsync<int>(@"INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active) VALUES ('PG005', 'Eve', 'PageTest', 'Intern', 'IT', TRUE) RETURNING user_id;", transaction: Transaction);

            // Act
            var page1 = (await _repository.GetAllUsersForAdminAsync(searchTerm: "IT", pageSize: 1, pageNumber: 1, connection: Connection, transaction: Transaction)).ToList();
            var page2 = (await _repository.GetAllUsersForAdminAsync(searchTerm: "IT", pageSize: 1, pageNumber: 2, connection: Connection, transaction: Transaction)).ToList();

            // Assert
            Assert.Single(page1);
            Assert.Single(page2);
            Assert.NotEqual(page1[0].UserId, page2[0].UserId); // Ensure different users on different pages
            Assert.Contains("IT", page1[0].Department!);
            Assert.Contains("IT", page2[0].Department!);
        }

        [Fact]
        public async Task LinkLogtoSubAsync_ShouldUpdateLogtoSubForUser()
        {
            // Arrange
            string newLogtoSub = $"logto|{Guid.NewGuid()}";

            // Act
            bool result = await _repository.LinkLogtoSubAsync(_seededUserId3, newLogtoSub, Connection, Transaction);

            // Assert
            Assert.True(result);
            var updatedUser = await Connection.QuerySingleOrDefaultAsync<AdminUserListItemDto>(
                "SELECT logto_sub AS LogtoSub FROM app.users WHERE user_id = @UserId",
                new { UserId = _seededUserId3 }, transaction: Transaction);
            Assert.NotNull(updatedUser);
            Assert.Equal(newLogtoSub, updatedUser.LogtoSub);
        }

        [Fact]
        public async Task LinkLogtoSubAsync_ShouldReturnFalse_ForNonExistentUser()
        {
            // Act
            bool result = await _repository.LinkLogtoSubAsync(-1, "logto|fake", Connection, Transaction);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserForAdminByIdAsync_UserExists_ShouldReturnUser()
        {
            // Act
            var result = await _repository.GetUserForAdminByIdAsync(_seededUserId1, Connection, Transaction);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_seededUserId1, result.UserId);
            Assert.Equal("Alice Admin", result.FullName);
            Assert.Equal("logto|admin1", result.LogtoSub);
        }

        [Fact]
        public async Task GetUserForAdminByIdAsync_UserNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetUserForAdminByIdAsync(-1, Connection, Transaction);

            // Assert
            Assert.Null(result);
        }

        private async Task<(int UserId, string LogtoSub, string ForceNumber)> SeedUserForLinkingTestAsync(string forceNumberSuffix, string logtoSubValue)
        {
            string forceNumber = $"U_LINKTEST_{forceNumberSuffix}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            Fixture.Output?.WriteLine($"Seeding User for Link Test: {forceNumber}, LogtoSub: {logtoSubValue}");
            const string sql = @"
                INSERT INTO app.users (force_number, first_name, last_name, rank, department, is_active, logto_sub)
                VALUES (@ForceNumber, 'LinkTestFN', 'UserLN', 'TestRankL', 'TestDeptL', TRUE, @LogtoSub)
                RETURNING user_id;";
            int userId = await Connection.ExecuteScalarAsync<int>(sql,
                new { ForceNumber = forceNumber, LogtoSub = logtoSubValue },
                transaction: this.Transaction);
            return (userId, logtoSubValue, forceNumber);
        }

        [Fact]
        public async Task GetUserIdByLogtoSubAsync_ShouldReturnUserId_WhenLogtoSubExists()
        {
            // Arrange
            string targetLogtoSub = $"test_sub_{Guid.NewGuid()}";
            var (expectedUserId, _, _) = await SeedUserForLinkingTestAsync("SUB01", targetLogtoSub);

            // Act
            var actualUserId = await _repository.GetUserIdByLogtoSubAsync(targetLogtoSub, connection: Connection, transaction: Transaction);

            // Assert
            Assert.NotNull(actualUserId);
            Assert.Equal(expectedUserId, actualUserId.Value);
        }

        [Fact]
        public async Task GetUserIdByLogtoSubAsync_ShouldReturnNull_WhenLogtoSubDoesNotExist()
        {
            // Arrange
            string nonExistentLogtoSub = $"non_existent_sub_{Guid.NewGuid()}";

            // Act
            var actualUserId = await _repository.GetUserIdByLogtoSubAsync(nonExistentLogtoSub, connection: Connection, transaction: Transaction);

            // Assert
            Assert.Null(actualUserId);
        }
    }
}
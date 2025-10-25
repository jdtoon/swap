using carestream.core.dtos.admin;
using carestream.core.dtos.user;
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using carestream.core.infrastructure;

namespace carestream.persistence.repositories
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(IConfiguration configuration, ILogger<UserRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext) // Pass facilityContext to BaseRepository
        {
        }

        public async Task<int?> GetUserIdByLogtoSubAsync(string logtoSub, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // This query is intentionally *not* filtered by facility_id, as logto_sub is a global identifier for a user
            // before their specific facility context is established.
            const string sql = @"
                SELECT user_id
                FROM app.users
                WHERE logto_sub = @LogtoSubParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var userId = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { LogtoSubParam = logtoSub }, transaction: trans);
                return userId;
            }, connection, transaction);
        }

        public async Task<UserVerificationCodeInfo?> GetUserVerificationCodeInfoAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id filter as user verification info is global to the user.
            const string sql = @"
                SELECT
                    hashed_verification_code AS HashedVerificationCode,
                    verification_code_salt AS VerificationCodeSalt
                FROM app.users
                WHERE user_id = @UserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<UserVerificationCodeInfo?>(sql, new { UserIdParam = userId }, transaction: trans),
                connection, transaction);
        }

        public async Task<bool> SetUserVerificationCodeAsync(int userId, string hashedCode, string salt, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id filter as updating user verification info is global to the user.
            // Added updated_at column update.
            const string sql = @"
                UPDATE app.users
                SET
                    hashed_verification_code = @HashedCode,
                    verification_code_salt = @Salt,
                    updated_at = NOW()
                WHERE user_id = @UserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { UserIdParam = userId, HashedCode = hashedCode, Salt = salt }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<IEnumerable<AdminUserListItemDto>> GetAllUsersForAdminAsync(
            string? searchTerm = null, int pageSize = 25, int pageNumber = 1,
            IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var sqlBuilder = new StringBuilder(@"
                SELECT
                    u.user_id AS UserId,
                    u.force_number AS ForceNumber,
                    u.first_name || ' ' || u.last_name AS FullName,
                    u.rank AS Rank,
                    u.department AS Department,
                    u.logto_sub AS LogtoSub,
                    u.is_active AS IsActive,
                    f.name AS FacilityName,
                    '' AS Roles
                FROM app.users u
                LEFT JOIN app.user_facilities uf ON u.user_id = uf.user_id AND uf.is_default = TRUE
                LEFT JOIN app.facilities f ON uf.facility_id = f.facility_id
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("(u.first_name ILIKE @SearchPattern OR u.last_name ILIKE @SearchPattern OR u.force_number ILIKE @SearchPattern OR u.department ILIKE @SearchPattern OR f.name ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{searchTerm}%");
            }

            if (whereClauses.Any())
            {
                sqlBuilder.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            sqlBuilder.Append(" ORDER BY u.last_name, u.first_name");
            sqlBuilder.Append(" LIMIT @PageSize OFFSET @Offset");
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (pageNumber - 1) * pageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<AdminUserListItemDto>(sqlBuilder.ToString(), parameters, transaction: trans),
                connection, transaction);
        }

        public async Task<bool> LinkLogtoSubAsync(int userId, string logtoSub, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.users
                SET logto_sub = @LogtoSub,
                    updated_at = NOW() -- Added updated_at as per schema
                WHERE user_id = @UserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { UserIdParam = userId, LogtoSub = logtoSub }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<AdminUserListItemDto?> GetUserForAdminByIdAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    u.user_id AS UserId,
                    u.force_number AS ForceNumber,
                    u.first_name || ' ' || u.last_name AS FullName,
                    u.rank AS Rank,
                    u.department AS Department,
                    u.logto_sub AS LogtoSub,
                    u.is_active AS IsActive,
                    f.name AS FacilityName,
                    '' AS Roles
                FROM app.users u
                LEFT JOIN app.user_facilities uf ON u.user_id = uf.user_id AND uf.is_default = TRUE
                LEFT JOIN app.facilities f ON uf.facility_id = f.facility_id
                WHERE u.user_id = @UserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<AdminUserListItemDto>(sql, new { UserIdParam = userId }, transaction: trans),
                connection, transaction);
        }

        public async Task<IEnumerable<UserFacilityLinkDto>> GetUserFacilityLinksAsync(int internalUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    user_id AS UserId,
                    facility_id AS FacilityId,
                    is_default AS IsDefault
                FROM app.user_facilities
                WHERE user_id = @InternalUserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<UserFacilityLinkDto>(sql, new { InternalUserIdParam = internalUserId }, transaction: trans),
                connection, transaction);
        }

        public async Task<AdminUserDetailDto?> GetUserDetailForAdminAsync(int userId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    u.user_id AS UserId,
                    u.force_number AS ForceNumber,
                    u.first_name || ' ' || u.last_name AS FullName,
                    u.rank AS Rank,
                    u.department AS Department,
                    u.logto_sub AS LogtoSub,
                    u.is_active AS IsActive
                FROM app.users u
                WHERE u.user_id = @UserIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QuerySingleOrDefaultAsync<AdminUserDetailDto>(sql, new { UserIdParam = userId }, transaction: trans),
                connection, transaction);
        }

        public async Task<bool> UpdateUserPersonalInfoForAdminAsync(AdminUserEditInputDto userEditInput, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Added updated_at and updated_by_user_id to the SET clause.
            const string sql = @"
                UPDATE app.users
                SET
                    rank = @Rank,
                    department = @Department,
                    is_active = @IsActive,
                    updated_at = NOW(),
                    updated_by_user_id = @UpdatedByUserId
                WHERE user_id = @UserId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, userEditInput, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<bool> LinkUserToFacilityAsync(int userId, int facilityId, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.user_facilities (user_id, facility_id, created_at, created_by_user_id)
                VALUES (@UserId, @FacilityId, NOW(), @CreatedByUserId)
                ON CONFLICT (user_id, facility_id) DO NOTHING;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { UserId = userId, FacilityId = facilityId, CreatedByUserId = createdByUserId }, transaction: trans);
                return affectedRows == 1; // ON CONFLICT DO NOTHING returns 0 affected rows if a conflict occurred
            }, connection, transaction);
        }

        public async Task<bool> UnlinkUserFromFacilityAsync(int userId, int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                DELETE FROM app.user_facilities
                WHERE user_id = @UserId AND facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { UserId = userId, FacilityId = facilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<bool> SetUserDefaultFacilityLinkAsync(int userId, int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string unsetDefaultSql = @"
                UPDATE app.user_facilities
                SET is_default = FALSE
                WHERE user_id = @UserId;";

            const string setDefaultSql = @"
                UPDATE app.user_facilities
                SET is_default = TRUE
                WHERE user_id = @UserId AND facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                // Unset any existing default for the user
                await conn.ExecuteAsync(unsetDefaultSql, new { UserId = userId }, transaction: trans);

                // Set the specified facility as default
                var affectedRows = await conn.ExecuteAsync(setDefaultSql, new { UserId = userId, FacilityId = facilityId }, transaction: trans);

                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateUserAsync(CreateUserInputDto userDto, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Use transaction for consistency across user and user_facilities inserts
            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                // Ensure a transaction is active for this compound operation
                bool ownsTransaction = trans == null;
                if (ownsTransaction)
                {
                    trans = conn.BeginTransaction();
                }

                try
                {
                    const string insertUserSql = @"
                        INSERT INTO app.users (
                            force_number, first_name, last_name, rank, department, is_active,
                            created_at, updated_at -- Corrected: removed created_by_user_id, added updated_at
                        ) VALUES (
                            @ForceNumber, @FirstName, @LastName, @Rank, @Department, @IsActive,
                            NOW(), NOW() -- Both created_at and updated_at set to NOW()
                        )
                        RETURNING user_id;";

                    var userId = await conn.ExecuteScalarAsync<int>(insertUserSql, new
                    {
                        userDto.ForceNumber,
                        userDto.FirstName,
                        userDto.LastName,
                        userDto.Rank,
                        userDto.Department,
                        userDto.IsActive
                        // Removed CreatedByUserId from here as it's not a column in app.users
                    }, transaction: trans);

                    // Link to initial facility and set as default
                    const string linkFacilitySql = @"
                        INSERT INTO app.user_facilities (user_id, facility_id, is_default, created_at, created_by_user_id)
                        VALUES (@UserId, @InitialFacilityId, TRUE, NOW(), @CreatedByUserId);";

                    await conn.ExecuteAsync(linkFacilitySql, new
                    {
                        UserId = userId,
                        InitialFacilityId = userDto.InitialFacilityId,
                        CreatedByUserId = createdByUserId // This is correct for user_facilities
                    }, transaction: trans);

                    if (ownsTransaction)
                    {
                        trans.Commit();
                    }
                    return userId;
                }
                catch (Npgsql.PostgresException pgEx)
                {
                    if (ownsTransaction)
                    {
                        trans.Rollback();
                    }
                    // Check for specific error codes related to unique constraints (e.g., 23505 for unique_violation)
                    if (pgEx.SqlState == "23505") // Unique Violation
                    {
                        // You can log this specifically if you want, e.g., for force_number uniqueness
                        _logger.LogError(pgEx, "Unique constraint violation during user creation for ForceNumber: {ForceNumber}", userDto.ForceNumber);
                        throw new InvalidOperationException("Force Number already exists.", pgEx); // Or a custom exception
                    }
                    throw; // Re-throw other exceptions
                }
                catch (Exception)
                {
                    if (ownsTransaction)
                    {
                        trans.Rollback();
                    }
                    throw; // Re-throw to propagate the exception
                }
            }, connection, transaction);
        }
    }
}
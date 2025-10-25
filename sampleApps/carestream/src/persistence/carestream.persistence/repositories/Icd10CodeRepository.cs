using carestream.core.dtos.consultation; // For Icd10CodeDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing ICD-10 code data persistence.
    /// </summary>
    public class Icd10CodeRepository : BaseRepository, IIcd10CodeRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Icd10CodeRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public Icd10CodeRepository(IConfiguration configuration, ILogger<Icd10CodeRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Icd10CodeDto>> SearchIcd10CodesAsync(string searchTerm, int limit = 10, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    icd10_code_id AS Icd10CodeId,
                    code AS Code,
                    description AS Description,
                    category AS Category,
                    is_active AS IsActive
                FROM app.icd10_codes
                WHERE (code ILIKE @SearchPattern OR description ILIKE @SearchPattern)
                   AND is_active = TRUE
                ORDER BY code
                LIMIT @Limit;";

            var searchPattern = $"%{searchTerm}%";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<Icd10CodeDto>(sql, new { SearchPattern = searchPattern, Limit = limit }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<Icd10CodeDto>();
        }

        /// <inheritdoc/>
        public async Task<Icd10CodeDto?> GetIcd10CodeByIdAsync(int icd10CodeId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    icd10_code_id AS Icd10CodeId,
                    code AS Code,
                    description AS Description,
                    category AS Category,
                    is_active AS IsActive
                FROM app.icd10_codes
                WHERE icd10_code_id = @Icd10CodeIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<Icd10CodeDto>(sql, new { Icd10CodeIdParam = icd10CodeId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<Icd10CodeDto?> GetIcd10CodeByCodeAsync(string code, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    icd10_code_id AS Icd10CodeId,
                    code AS Code,
                    description AS Description,
                    category AS Category,
                    is_active AS IsActive
                FROM app.icd10_codes
                WHERE code ILIKE @CodeParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<Icd10CodeDto>(sql, new { CodeParam = code }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<Icd10CodeDto> Items, int TotalCount)> GetAllIcd10CodesAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.icd10_codes ic
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(ic.code ILIKE @SearchPattern OR ic.description ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }
            if (options.IsActiveFilter.HasValue)
            {
                whereClauses.Add("ic.is_active = @IsActiveFilter");
                parameters.Add("IsActiveFilter", options.IsActiveFilter.Value);
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(ic.icd10_code_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    ic.icd10_code_id AS Icd10CodeId,
                    ic.code AS Code,
                    ic.description AS Description,
                    ic.category AS Category,
                    ic.is_active AS IsActive
                {baseSql.ToString()}
                ORDER BY ic.code
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<Icd10CodeDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<Icd10CodeDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateIcd10CodeAsync(Icd10CodeDto icd10CodeDto, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.icd10_codes (
                    code, description, category, is_active, created_at, created_by_user_id
                ) VALUES (
                    @Code, @Description, @Category, @IsActive, NOW(), @CreatedByUserId
                )
                RETURNING icd10_code_id;";

            var parameters = new DynamicParameters(icd10CodeDto);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateIcd10CodeAsync(Icd10CodeDto icd10CodeDto, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (icd10CodeDto.Icd10CodeId <= 0) return false;

            const string sql = @"
                UPDATE app.icd10_codes
                SET
                    code = @Code,
                    description = @Description,
                    category = @Category,
                    is_active = @IsActive,
                    updated_at = NOW(),
                    updated_by_user_id = @UpdatedByUserId
                WHERE icd10_code_id = @Icd10CodeId;";

            var parameters = new DynamicParameters(icd10CodeDto);
            parameters.Add("UpdatedByUserId", updatedByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateIcd10CodeAsync(int icd10CodeId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (icd10CodeId <= 0) return false;

            const string sql = @"
                UPDATE app.icd10_codes
                SET
                    is_active = FALSE,
                    updated_at = NOW(),
                    updated_by_user_id = @DeactivatedByUserId
                WHERE icd10_code_id = @Icd10CodeId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { Icd10CodeId = icd10CodeId, DeactivatedByUserId = deactivatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> ActivateIcd10CodeAsync(int icd10CodeId, int activatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (icd10CodeId <= 0) return false;

            const string sql = @"
                UPDATE app.icd10_codes
                SET
                    is_active = TRUE,
                    updated_at = NOW(),
                    updated_by_user_id = @ActivatedByUserId
                WHERE icd10_code_id = @Icd10CodeId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { Icd10CodeId = icd10CodeId, ActivatedByUserId = activatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}
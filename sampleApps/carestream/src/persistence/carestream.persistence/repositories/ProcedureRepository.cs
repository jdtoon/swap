using carestream.core.dtos.consultation; // For ProcedureDto
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
    /// Repository for managing medical procedure data persistence.
    /// </summary>
    public class ProcedureRepository : BaseRepository, IProcedureRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcedureRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public ProcedureRepository(IConfiguration configuration, ILogger<ProcedureRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProcedureDto>> SearchProceduresAsync(string searchTerm, int limit = 10, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    procedure_id AS ProcedureId,
                    code AS Code,
                    name AS Name,
                    description AS Description,
                    category AS Category,
                    is_active AS IsActive
                FROM app.procedures
                WHERE (code ILIKE @SearchPattern OR name ILIKE @SearchPattern OR description ILIKE @SearchPattern)
                   AND is_active = TRUE
                ORDER BY name, code
                LIMIT @Limit;";

            var searchPattern = $"%{searchTerm}%";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<ProcedureDto>(sql, new { SearchPattern = searchPattern, Limit = limit }, transaction: trans),
                connection, transaction) ?? Enumerable.Empty<ProcedureDto>();
        }

        /// <inheritdoc/>
        public async Task<ProcedureDto?> GetProcedureByIdAsync(int procedureId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    procedure_id AS ProcedureId,
                    code AS Code,
                    name AS Name,
                    description AS Description,
                    category AS Category,
                    is_active AS IsActive
                FROM app.procedures
                WHERE procedure_id = @ProcedureIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<ProcedureDto>(sql, new { ProcedureIdParam = procedureId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<ProcedureDto?> GetProcedureByCodeAsync(string code, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    procedure_id AS ProcedureId,
                    code AS Code,
                    name AS Name,
                    description AS Description,
                    category AS Category,
                    is_active AS IsActive
                FROM app.procedures
                WHERE code ILIKE @CodeParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<ProcedureDto>(sql, new { CodeParam = code }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<ProcedureDto> Items, int TotalCount)> GetAllProceduresAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.procedures p
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1)) // Search by code or name or description
            {
                whereClauses.Add("(p.code ILIKE @SearchPattern OR p.name ILIKE @SearchPattern OR p.description ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }
            if (options.IsActiveFilter.HasValue) // Filter by active status
            {
                whereClauses.Add("p.is_active = @IsActiveFilter");
                parameters.Add("IsActiveFilter", options.IsActiveFilter.Value);
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(p.procedure_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    p.procedure_id AS ProcedureId,
                    p.code AS Code,
                    p.name AS Name,
                    p.description AS Description,
                    p.category AS Category,
                    p.is_active AS IsActive
                {baseSql.ToString()}
                ORDER BY p.name
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<ProcedureDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<ProcedureDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateProcedureAsync(ProcedureDto procedureDto, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.procedures (
                    code, name, description, category, is_active, created_at, created_by_user_id
                ) VALUES (
                    @Code, @Name, @Description, @Category, @IsActive, NOW(), @CreatedByUserId
                )
                RETURNING procedure_id;";

            var parameters = new DynamicParameters(procedureDto);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateProcedureAsync(ProcedureDto procedureDto, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (procedureDto.ProcedureId <= 0) return false;

            const string sql = @"
                UPDATE app.procedures
                SET
                    code = @Code,
                    name = @Name,
                    description = @Description,
                    category = @Category,
                    is_active = @IsActive,
                    updated_at = NOW(),
                    updated_by_user_id = @UpdatedByUserId
                WHERE procedure_id = @ProcedureId;";

            var parameters = new DynamicParameters(procedureDto);
            parameters.Add("UpdatedByUserId", updatedByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateProcedureAsync(int procedureId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (procedureId <= 0) return false;

            const string sql = @"
                UPDATE app.procedures
                SET
                    is_active = FALSE,
                    updated_at = NOW(),
                    updated_by_user_id = @DeactivatedByUserId
                WHERE procedure_id = @ProcedureId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ProcedureId = procedureId, DeactivatedByUserId = deactivatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> ActivateProcedureAsync(int procedureId, int activatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (procedureId <= 0) return false;

            const string sql = @"
                UPDATE app.procedures
                SET
                    is_active = TRUE,
                    updated_at = NOW(),
                    updated_by_user_id = @ActivatedByUserId
                WHERE procedure_id = @ProcedureId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ProcedureId = procedureId, ActivatedByUserId = activatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}
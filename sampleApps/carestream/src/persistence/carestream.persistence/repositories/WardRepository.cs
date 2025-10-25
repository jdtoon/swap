using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.admin.facility;
using carestream.core.interfaces.repositories;
using carestream.core.infrastructure;
using carestream.core.dtos.shared;
using System.Text;
using System.Linq; // For .Any()
using System; // For NOW()

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing Ward data persistence using Dapper.
    /// </summary>
    public class WardRepository : BaseRepository, IWardRepository
    {
        public WardRepository(IConfiguration configuration, ILogger<WardRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<WardDto?> GetWardByIdAsync(int wardId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    w.ward_id AS WardId,
                    w.facility_id AS FacilityId,
                    f.name AS FacilityName,
                    w.department_id AS DepartmentId,
                    d.name AS DepartmentName,
                    w.name AS Name,
                    w.description AS Description,
                    w.is_active AS IsActive
                FROM app.wards w
                JOIN app.facilities f ON w.facility_id = f.facility_id
                LEFT JOIN app.departments d ON w.department_id = d.department_id
                WHERE w.ward_id = @WardIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<WardDto>(sql, new { WardIdParam = wardId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<WardDto?> GetWardByNameAndFacilityAsync(int facilityId, string wardName, int? departmentId = null, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var sqlBuilder = new StringBuilder(@"
                SELECT
                    w.ward_id AS WardId,
                    w.facility_id AS FacilityId,
                    f.name AS FacilityName,
                    w.department_id AS DepartmentId,
                    d.name AS DepartmentName,
                    w.name AS Name,
                    w.description AS Description,
                    w.is_active AS IsActive
                FROM app.wards w
                JOIN app.facilities f ON w.facility_id = f.facility_id
                LEFT JOIN app.departments d ON w.department_id = d.department_id
                WHERE w.facility_id = @FacilityIdParam AND w.name ILIKE @WardNameParam
            ");

            var parameters = new DynamicParameters();
            parameters.Add("FacilityIdParam", facilityId);
            parameters.Add("WardNameParam", wardName);

            if (departmentId.HasValue)
            {
                sqlBuilder.Append(" AND w.department_id = @DepartmentIdParam");
                parameters.Add("DepartmentIdParam", departmentId.Value);
            }
            else
            {
                sqlBuilder.Append(" AND w.department_id IS NULL");
            }

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<WardDto>(sqlBuilder.ToString(), parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<WardDto> Items, int TotalCount)> GetWardsByFacilityAsync(int facilityId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.wards w
                JOIN app.facilities f ON w.facility_id = f.facility_id
                LEFT JOIN app.departments d ON w.department_id = d.department_id
                WHERE w.facility_id = @FacilityIdParam
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("FacilityIdParam", facilityId);

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(w.name ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }

            if (options.SearchTerm2 != null && int.TryParse(options.SearchTerm2, out int departmentIdFilter))
            {
                if (departmentIdFilter == 0)
                {
                    whereClauses.Add("w.department_id IS NULL");
                }
                else
                {
                    whereClauses.Add("w.department_id = @DepartmentIdFilter");
                }

                parameters.Add("DepartmentIdFilter", departmentIdFilter);
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" AND ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(w.ward_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    w.ward_id AS WardId,
                    w.facility_id AS FacilityId,
                    f.name AS FacilityName,
                    w.department_id AS DepartmentId,
                    d.name AS DepartmentName,
                    w.name AS Name,
                    w.description AS Description,
                    w.is_active AS IsActive
                {baseSql.ToString()}
                ORDER BY w.name
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<WardDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<WardDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<WardDto>> GetAllActiveWardsByFacilityAsync(int facilityId, int? departmentId = null, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var sqlBuilder = new StringBuilder(@"
                SELECT
                    w.ward_id AS WardId,
                    w.facility_id AS FacilityId,
                    f.name AS FacilityName,
                    w.department_id AS DepartmentId,
                    d.name AS DepartmentName,
                    w.name AS Name,
                    w.description AS Description,
                    w.is_active AS IsActive
                FROM app.wards w
                JOIN app.facilities f ON w.facility_id = f.facility_id
                LEFT JOIN app.departments d ON w.department_id = d.department_id
                WHERE w.facility_id = @FacilityIdParam AND w.is_active = TRUE
            ");
            var parameters = new DynamicParameters();
            parameters.Add("FacilityIdParam", facilityId);

            if (departmentId.HasValue)
            {
                sqlBuilder.Append(" AND w.department_id = @DepartmentIdParam");
                parameters.Add("DepartmentIdParam", departmentId.Value);
            }
            sqlBuilder.Append(" ORDER BY w.name;");

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<WardDto>(sqlBuilder.ToString(), parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateWardAsync(CreateUpdateWardDto ward, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.wards (facility_id, department_id, name, description, is_active, created_at, created_by_user_id)
                VALUES (@FacilityId, @DepartmentId, @Name, @Description, @IsActive, NOW(), @CreatedByUserId)
                RETURNING ward_id;";

            var parameters = new DynamicParameters(ward);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateWardAsync(CreateUpdateWardDto ward, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Removed facility_id from SET clause as it's typically immutable for wards.
            const string sql = @"
                UPDATE app.wards
                SET
                    department_id = @DepartmentId,
                    name = @Name,
                    description = @Description,
                    is_active = @IsActive,
                    updated_at = NOW(),
                    updated_by_user_id = @UpdatedByUserId
                WHERE ward_id = @WardId AND facility_id = @FacilityId;"; // Ensure facility_id is in WHERE for update

            var parameters = new DynamicParameters(ward);
            parameters.Add("UpdatedByUserId", updatedByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> DeactivateWardAsync(int wardId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.wards
                SET
                    is_active = FALSE,
                    updated_at = NOW(),
                    updated_by_user_id = @DeactivatedByUserId
                WHERE ward_id = @WardId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { WardId = wardId, DeactivatedByUserId = deactivatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}
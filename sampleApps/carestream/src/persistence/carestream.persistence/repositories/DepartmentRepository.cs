using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.admin.facility;
using carestream.core.interfaces.repositories;
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Text; // For StringBuilder
using System.Linq; // For .Any()
using System; // For NOW()

namespace carestream.persistence.repositories
{
    public class DepartmentRepository : BaseRepository, IDepartmentRepository
    {
        public DepartmentRepository(IConfiguration configuration, ILogger<DepartmentRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    d.department_id AS DepartmentId,
                    d.facility_id AS FacilityId,
                    d.name AS Name,
                    d.description AS Description,
                    d.is_active AS IsActive,
                    f.name AS FacilityName
                FROM app.departments d
                JOIN app.facilities f ON d.facility_id = f.facility_id
                WHERE d.department_id = @DepartmentIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { DepartmentIdParam = departmentId }, transaction: trans),
                connection, transaction);
        }

        public async Task<DepartmentDto?> GetDepartmentByNameAndFacilityAsync(int facilityId, string departmentName, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    d.department_id AS DepartmentId,
                    d.facility_id AS FacilityId,
                    d.name AS Name,
                    d.description AS Description,
                    d.is_active AS IsActive,
                    f.name AS FacilityName
                FROM app.departments d
                JOIN app.facilities f ON d.facility_id = f.facility_id
                WHERE d.facility_id = @FacilityIdParam AND d.name ILIKE @DepartmentNameParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { FacilityIdParam = facilityId, DepartmentNameParam = departmentName }, transaction: trans),
                connection, transaction);
        }

        public async Task<(IEnumerable<DepartmentDto> Items, int TotalCount)> GetDepartmentsByFacilityAsync(int facilityId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.departments d
                JOIN app.facilities f ON d.facility_id = f.facility_id
                WHERE d.facility_id = @FacilityIdParam
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("FacilityIdParam", facilityId);

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(d.name ILIKE @SearchPattern OR d.description ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" AND ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(d.department_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    d.department_id AS DepartmentId,
                    d.facility_id AS FacilityId,
                    d.name AS Name,
                    d.description AS Description,
                    d.is_active AS IsActive,
                    f.name AS FacilityName
                {baseSql.ToString()}
                ORDER BY d.name
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<DepartmentDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<DepartmentDto>(), totalCount);
            }, connection, transaction);
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllActiveDepartmentsByFacilityAsync(int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    d.department_id AS DepartmentId,
                    d.facility_id AS FacilityId,
                    d.name AS Name,
                    d.description AS Description,
                    d.is_active AS IsActive,
                    f.name AS FacilityName
                FROM app.departments d
                JOIN app.facilities f ON d.facility_id = f.facility_id
                WHERE d.facility_id = @FacilityIdParam AND d.is_active = TRUE
                ORDER BY d.name;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<DepartmentDto>(sql, new { FacilityIdParam = facilityId }, transaction: trans),
                connection, transaction);
        }

        public async Task<int> CreateDepartmentAsync(CreateUpdateDepartmentDto department, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.departments (facility_id, name, description, is_active, created_at, created_by_user_id)
                VALUES (@FacilityId, @Name, @Description, @IsActive, NOW(), @CreatedByUserId)
                RETURNING department_id;";

            var parameters = new DynamicParameters(department);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        public async Task<bool> UpdateDepartmentAsync(CreateUpdateDepartmentDto department, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.departments
                SET
                    name = @Name,
                    description = @Description,
                    is_active = @IsActive,
                    updated_at = NOW(),
                    updated_by_user_id = @UpdatedByUserId
                WHERE department_id = @DepartmentId AND facility_id = @FacilityId;";

            var parameters = new DynamicParameters(department);
            parameters.Add("UpdatedByUserId", updatedByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<bool> DeactivateDepartmentAsync(int departmentId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.departments
                SET
                    is_active = FALSE,
                    updated_at = NOW(),
                    updated_by_user_id = @DeactivatedByUserId
                WHERE department_id = @DepartmentId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { DepartmentId = departmentId, DeactivatedByUserId = deactivatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<DepartmentDetailWithChildrenDto?> GetDepartmentDetailsWithChildrenAsync(int departmentId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // SQL for multi-mapping: fetches department and its associated wards.
            const string sql = @"
                SELECT
                    d.department_id AS DepartmentId,
                    d.facility_id AS FacilityId,
                    d.name AS Name,
                    d.description AS Description,
                    d.is_active AS IsActive,
                    f.name AS FacilityName, -- From Facility join
                    d.created_at AS CreatedAt,
                    d.created_by_user_id AS CreatedByUserId,
                    d.updated_at AS UpdatedAt,
                    d.updated_by_user_id AS UpdatedByUserId,
                    -- Ward details (prefixed for Dapper mapping)
                    w.ward_id AS WardId,
                    w.name AS WardName,
                    w.description AS WardDescription,
                    w.is_active AS WardIsActive
                FROM app.departments d
                JOIN app.facilities f ON d.facility_id = f.facility_id
                LEFT JOIN app.wards w ON d.department_id = w.department_id AND w.is_active = TRUE
                WHERE d.department_id = @DepartmentIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var departmentDictionary = new Dictionary<int, DepartmentDetailWithChildrenDto>();

                await conn.QueryAsync<DepartmentDetailWithChildrenDto, WardDto, DepartmentDetailWithChildrenDto>(
                    sql,
                    (department, ward) =>
                    {
                        if (!departmentDictionary.TryGetValue(department.DepartmentId, out var currentDepartment))
                        {
                            currentDepartment = department;
                            departmentDictionary.Add(department.DepartmentId, currentDepartment);
                        }

                        if (ward != null && !currentDepartment.Wards.Any(w => w.WardId == ward.WardId))
                        {
                            currentDepartment.Wards.Add(ward);
                        }

                        return currentDepartment;
                    },
                    new { DepartmentIdParam = departmentId },
                    splitOn: "WardId", // Dapper splits on this column
                    transaction: trans
                );

                return departmentDictionary.Values.FirstOrDefault();
            }, connection, transaction);
        }
    }
}
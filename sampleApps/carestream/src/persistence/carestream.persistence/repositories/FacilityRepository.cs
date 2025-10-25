using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using carestream.core.dtos.facility;
using carestream.core.interfaces.repositories;
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.dtos.admin.facility; // For CreateUpdateFacilityDto, FacilityDetailWithChildrenDto, DepartmentDto, WardDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Text; // For StringBuilder

namespace carestream.persistence.repositories
{
    public class FacilityRepository : BaseRepository, IFacilityRepository
    {
        public FacilityRepository(IConfiguration configuration, ILogger<FacilityRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public async Task<FacilityDto?> GetFacilityByIdAsync(int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    facility_id AS FacilityId,
                    name AS Name,
                    short_code AS ShortCode,
                    is_active AS IsActive,
                    address_line1 AS AddressLine1,
                    address_line2 AS AddressLine2,
                    city AS City,
                    province AS Province,
                    country AS Country,
                    phone_number AS PhoneNumber,
                    email_address AS EmailAddress
                FROM app.facilities
                WHERE facility_id = @FacilityIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<FacilityDto>(sql, new { FacilityIdParam = facilityId }, transaction: trans),
                connection, transaction);
        }

        public async Task<FacilityDto?> GetFacilityByNameAsync(string name, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    facility_id AS FacilityId,
                    name AS Name,
                    short_code AS ShortCode,
                    is_active AS IsActive,
                    city AS City
                FROM app.facilities
                WHERE name ILIKE @NameParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<FacilityDto>(sql, new { NameParam = name }, transaction: trans),
                connection, transaction);
        }

        public async Task<FacilityDto?> GetFacilityByShortCodeAsync(string shortCode, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    facility_id AS FacilityId,
                    name AS Name,
                    short_code AS ShortCode,
                    is_active AS IsActive,
                    city AS City
                FROM app.facilities
                WHERE short_code ILIKE @ShortCodeParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<FacilityDto>(sql, new { ShortCodeParam = shortCode }, transaction: trans),
                connection, transaction);
        }

        public async Task<IEnumerable<FacilityDto>> GetAllActiveFacilitiesAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    facility_id AS FacilityId,
                    name AS Name,
                    short_code AS ShortCode,
                    is_active AS IsActive,
                    city AS City
                FROM app.facilities
                WHERE is_active = TRUE
                ORDER BY name;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<FacilityDto>(sql, transaction: trans),
                connection, transaction);
        }

        public async Task<(IEnumerable<FacilityDto> Items, int TotalCount)> GetFacilitiesForAdminAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.facilities f
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1))
            {
                whereClauses.Add("(f.name ILIKE @SearchPattern OR f.short_code ILIKE @SearchPattern OR f.city ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }

            if (whereClauses.Any())
            {
                baseSql.Append(" WHERE ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(f.facility_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    f.facility_id AS FacilityId,
                    f.name AS Name,
                    f.short_code AS ShortCode,
                    f.is_active AS IsActive,
                    f.city AS City
                {baseSql.ToString()}
                ORDER BY f.name
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<FacilityDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<FacilityDto>(), totalCount);
            }, connection, transaction);
        }

        public async Task<int> CreateFacilityAsync(CreateUpdateFacilityDto facility, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.facilities (
                    name, short_code, address_line1, address_line2, city,
                    province, country, phone_number, email_address, is_active,
                    created_at, created_by_user_id
                ) VALUES (
                    @Name, @ShortCode, @AddressLine1, @AddressLine2, @City,
                    @Province, @Country, @PhoneNumber, @EmailAddress, @IsActive,
                    NOW(), @CreatedByUserId
                )
                RETURNING facility_id;";

            var parameters = new DynamicParameters(facility);
            parameters.Add("CreatedByUserId", createdByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        public async Task<bool> UpdateFacilityAsync(CreateUpdateFacilityDto facility, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.facilities
                SET
                    name = @Name,
                    short_code = @ShortCode,
                    address_line1 = @AddressLine1,
                    address_line2 = @AddressLine2,
                    city = @City,
                    province = @Province,
                    country = @Country,
                    phone_number = @PhoneNumber,
                    email_address = @EmailAddress,
                    is_active = @IsActive,
                    updated_at = NOW(),
                    updated_by_user_id = @UpdatedByUserId
                WHERE facility_id = @FacilityId;";

            var parameters = new DynamicParameters(facility);
            parameters.Add("UpdatedByUserId", updatedByUserId);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        public async Task<bool> DeactivateFacilityAsync(int facilityId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.facilities
                SET
                    is_active = FALSE,
                    updated_at = NOW(),
                    updated_by_user_id = @DeactivatedByUserId
                WHERE facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { FacilityId = facilityId, DeactivatedByUserId = deactivatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> ActivateFacilityAsync(int facilityId, int activatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                UPDATE app.facilities
                SET
                    is_active = TRUE,
                    updated_at = NOW(),
                    updated_by_user_id = @ActivatedByUserId
                WHERE facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { FacilityId = facilityId, ActivatedByUserId = activatedByUserId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<FacilityDetailWithChildrenDto?> GetFacilityDetailsWithChildrenAsync(int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // SQL for multi-mapping: fetches facility, its departments, and its wards.
            // Ordering is crucial for Dapper's multi-mapping to correctly group.
            const string sql = @"
                SELECT
                    f.facility_id AS FacilityId,
                    f.name AS Name,
                    f.short_code AS ShortCode,
                    f.address_line1 AS AddressLine1,
                    f.address_line2 AS AddressLine2,
                    f.city AS City,
                    f.province AS Province,
                    f.country AS Country,
                    f.phone_number AS PhoneNumber,
                    f.email_address AS EmailAddress,
                    f.is_active AS IsActive,
                    f.created_at AS CreatedAt,
                    f.created_by_user_id AS CreatedByUserId,
                    f.updated_at AS UpdatedAt,
                    f.updated_by_user_id AS UpdatedByUserId,
                    -- Department details (prefixed for Dapper mapping)
                    d.department_id AS DepartmentId,
                    d.name AS DepartmentName,
                    d.description AS DepartmentDescription,
                    d.is_active AS DepartmentIsActive,
                    -- Ward details (prefixed for Dapper mapping)
                    w.ward_id AS WardId,
                    w.name AS WardName,
                    w.description AS WardDescription,
                    w.is_active AS WardIsActive
                FROM app.facilities f
                LEFT JOIN app.departments d ON f.facility_id = d.facility_id AND d.is_active = TRUE
                LEFT JOIN app.wards w ON f.facility_id = w.facility_id AND w.is_active = TRUE
                WHERE f.facility_id = @FacilityIdParam
                ORDER BY d.department_id, w.ward_id;"; // Order ensures correct grouping for Dapper

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var facilityDictionary = new Dictionary<int, FacilityDetailWithChildrenDto>();

                await conn.QueryAsync<FacilityDetailWithChildrenDto, DepartmentDto, WardDto, FacilityDetailWithChildrenDto>(
                    sql,
                    (facility, department, ward) =>
                    {
                        if (!facilityDictionary.TryGetValue(facility.FacilityId, out var currentFacility))
                        {
                            currentFacility = facility;
                            facilityDictionary.Add(facility.FacilityId, currentFacility);
                        }

                        if (department != null && !currentFacility.Departments.Any(d => d.DepartmentId == department.DepartmentId))
                        {
                            currentFacility.Departments.Add(department);
                        }

                        if (ward != null)
                        {
                            // Wards might be linked to departments, so add to department's ward list if applicable
                            // For simplicity, adding to the facility's overall ward list here, or could refine to department's list
                            // Based on FacilityDetailWithChildrenDto DTO, wards are direct children of facility (List<WardDto> Wards)
                            if (!currentFacility.Wards.Any(w => w.WardId == ward.WardId))
                            {
                                currentFacility.Wards.Add(ward);
                            }
                            // If you need wards under their respective departments in the DTO, the DTO structure needs nesting (DepartmentDto contains List<WardDto>)
                            // and the Dapper mapping logic would become more complex, involving an additional mapping level.
                            // For now, based on provided DTO (FacilityDetailWithChildrenDto), wards are directly under facility.
                        }

                        return currentFacility;
                    },
                    new { FacilityIdParam = facilityId },
                    splitOn: "DepartmentId,WardId", // Dapper splits on these columns
                    transaction: trans
                );

                return facilityDictionary.Values.FirstOrDefault();
            }, connection, transaction);
        }
    }
}
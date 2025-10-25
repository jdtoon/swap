using carestream.core.dtos.admin.staffreport; // For StaffReportDto, CreateUpdateStaffReportDto
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
    /// Repository for managing staff report data persistence.
    /// </summary>
    public class StaffReportRepository : BaseRepository, IStaffReportRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StaffReportRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public StaffReportRepository(IConfiguration configuration, ILogger<StaffReportRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<StaffReportDto?> GetStaffReportByIdAsync(int reportId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    sr.report_id AS ReportId,
                    sr.author_user_id AS AuthorUserId,
                    u.first_name || ' ' || u.last_name AS AuthorUserName,
                    u.rank AS AuthorUserRank,
                    sr.title AS Title,
                    sr.priority AS Priority,
                    sr.content AS Content,
                    sr.created_at AS CreatedAt,
                    sr.facility_id AS FacilityId,
                    f.name AS FacilityName,
                    sr.department_id AS DepartmentId,
                    d.name AS DepartmentName
                FROM app.staff_reports sr
                JOIN app.users u ON sr.author_user_id = u.user_id
                LEFT JOIN app.facilities f ON sr.facility_id = f.facility_id
                LEFT JOIN app.departments d ON sr.department_id = d.department_id
                WHERE sr.report_id = @ReportIdParam;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryFirstOrDefaultAsync<StaffReportDto>(sql, new { ReportIdParam = reportId }, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<StaffReportDto> Items, int TotalCount)> GetAllStaffReportsAsync(int facilityId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            var baseSql = new StringBuilder(@"
                FROM app.staff_reports sr
                JOIN app.users u ON sr.author_user_id = u.user_id
                LEFT JOIN app.facilities f ON sr.facility_id = f.facility_id
                LEFT JOIN app.departments d ON sr.department_id = d.department_id
                WHERE sr.facility_id = @FacilityIdParam
            ");

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("FacilityIdParam", facilityId);

            if (!string.IsNullOrWhiteSpace(options.SearchTerm1)) // Search by title, content, or author name
            {
                whereClauses.Add("(sr.title ILIKE @SearchPattern OR sr.content ILIKE @SearchPattern OR u.first_name ILIKE @SearchPattern OR u.last_name ILIKE @SearchPattern)");
                parameters.Add("SearchPattern", $"%{options.SearchTerm1}%");
            }
            if (!string.IsNullOrWhiteSpace(options.SearchTerm2)) // Search by department name or priority
            {
                whereClauses.Add("(d.name ILIKE @SearchPattern2 OR sr.priority ILIKE @SearchPattern2)");
                parameters.Add("SearchPattern2", $"%{options.SearchTerm2}%");
            }
            // Add date range filtering if needed for StartDate/EndDate options

            if (whereClauses.Any())
            {
                baseSql.Append(" AND ").Append(string.Join(" AND ", whereClauses));
            }

            string countSql = $"SELECT COUNT(sr.report_id) {baseSql.ToString()}";

            var itemsSql = new StringBuilder($@"
                SELECT
                    sr.report_id AS ReportId,
                    sr.author_user_id AS AuthorUserId,
                    u.first_name || ' ' || u.last_name AS AuthorUserName,
                    u.rank AS AuthorUserRank,
                    sr.title AS Title,
                    sr.priority AS Priority,
                    sr.content AS Content,
                    sr.created_at AS CreatedAt,
                    sr.facility_id AS FacilityId,
                    f.name AS FacilityName,
                    sr.department_id AS DepartmentId,
                    d.name AS DepartmentName
                {baseSql.ToString()}
                ORDER BY sr.created_at DESC
                LIMIT @PageSize OFFSET @Offset;
            ");
            parameters.Add("PageSize", options.PageSize);
            parameters.Add("Offset", (options.PageNumber - 1) * options.PageSize);

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var totalCount = await conn.QuerySingleAsync<int>(countSql, parameters, transaction: trans);
                var items = await conn.QueryAsync<StaffReportDto>(itemsSql.ToString(), parameters, transaction: trans);
                return (items ?? Enumerable.Empty<StaffReportDto>(), totalCount);
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> CreateStaffReportAsync(CreateUpdateStaffReportDto reportData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                INSERT INTO app.staff_reports (
                    author_user_id, title, priority, content, created_at, facility_id, department_id
                ) VALUES (
                    @AuthorUserId, @Title, @Priority, @Content, NOW(), @FacilityId, @DepartmentId
                )
                RETURNING report_id;";

            var parameters = new DynamicParameters(reportData);
            // AuthorUserId and FacilityId are expected to be set in the DTO or passed from service.

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.ExecuteScalarAsync<int>(sql, parameters, transaction: trans),
                connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateStaffReportAsync(CreateUpdateStaffReportDto reportData, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (reportData.ReportId <= 0) return false;

            const string sql = @"
                UPDATE app.staff_reports
                SET
                    title = @Title,
                    priority = @Priority,
                    content = @Content,
                    department_id = @DepartmentId
                WHERE report_id = @ReportId AND facility_id = @FacilityId;";

            var parameters = new DynamicParameters(reportData);
            parameters.Add("FacilityId", _facilityContext.CurrentFacilityId); // Ensure facility_id is in WHERE for update.

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, parameters, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteStaffReportAsync(int reportId, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            if (reportId <= 0) return false;

            const string sql = @"
                DELETE FROM app.staff_reports
                WHERE report_id = @ReportId AND facility_id = @FacilityId;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var affectedRows = await conn.ExecuteAsync(sql, new { ReportId = reportId, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans);
                return affectedRows == 1;
            }, connection, transaction);
        }
    }
}
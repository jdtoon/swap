using carestream.core.dtos.dashboard;
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using carestream.core.infrastructure; // Added for ICurrentFacilityContext
using carestream.core.enums; // Added for VisitStatus enum
using System.Collections.Generic; // For List<string>
using System.Linq; // For Enumerable.Empty()

namespace carestream.persistence.repositories
{
    public class DashboardRepository : BaseRepository, IDashboardRepository
    {
        public DashboardRepository(IConfiguration configuration, ILogger<DashboardRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // Updated SQL: 'Pending Checkin' changed to 'WaitingForVitals'
            // and removed the outer WHERE clause to sum counts across all relevant statuses for the facility.
            const string sql = @"
                SELECT
                    (SELECT COUNT(*) FROM app.visits WHERE facility_id = @FacilityId) AS TotalSickBayVisits,
                    SUM(CASE WHEN v.status = @InTreatmentStatus THEN 1 ELSE 0 END)::int AS CurrentlyInTreatment,
                    SUM(CASE WHEN v.status = @WaitingForVitalsStatus THEN 1 ELSE 0 END)::int AS PendingCheckin
                FROM app.visits v
                WHERE v.facility_id = @FacilityId;"; // Filter by facility for all counts

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                var stats = await conn.QueryFirstOrDefaultAsync<DashboardStatsDto>(sql, new
                {
                    FacilityId = _facilityContext.CurrentFacilityId,
                    InTreatmentStatus = VisitStatus.InTreatment.ToString(),
                    WaitingForVitalsStatus = VisitStatus.WaitingForVitals.ToString()
                }, transaction: trans);
                return stats ?? new DashboardStatsDto { TotalSickBayVisits = 0, CurrentlyInTreatment = 0, PendingCheckin = 0 };
            }, connection, transaction);
        }

        public async Task<IEnumerable<RecentPatientDto>> GetRecentPatientsAsync(int limit = 5, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            const string sql = @"
                SELECT
                    v.visit_id AS VisitId,
                    p.patient_id AS PatientId,
                    p.rank || '. ' || p.first_name || ' ' || p.last_name AS Name,
                    v.visit_timestamp AS VisitTimestamp,
                    v.brief_reason AS BriefReason,
                    v.status AS Status
                FROM app.visits v
                JOIN app.patients p ON v.patient_id = p.patient_id
                WHERE v.facility_id = @FacilityId
                ORDER BY v.visit_timestamp DESC
                LIMIT @LimitValue;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<RecentPatientDto>(sql, new { LimitValue = limit, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }

        public async Task<IEnumerable<RecentStaffReportDto>> GetRecentStaffReportsAsync(int limit = 5, IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // CORRECTED SQL: Joins app.departments to get the department name based on department_id.
            const string sql = @"
                SELECT
                    sr.report_id AS ReportId,
                    sr.title AS Title,
                    u.rank || '. ' || u.first_name || ' ' || u.last_name AS Author,
                    COALESCE(d.name, 'N/A') AS Department, -- Use d.name and COALESCE for null department_id
                    sr.priority AS Priority,
                    sr.created_at AS Timestamp
                FROM app.staff_reports sr
                JOIN app.users u ON sr.author_user_id = u.user_id
                LEFT JOIN app.departments d ON sr.department_id = d.department_id -- LEFT JOIN to get department name
                WHERE sr.facility_id = @FacilityId
                ORDER BY sr.created_at DESC
                LIMIT @LimitValue;";

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
                await conn.QueryAsync<RecentStaffReportDto>(sql, new { LimitValue = limit, FacilityId = _facilityContext.CurrentFacilityId }, transaction: trans),
                connection, transaction);
        }
    }
}
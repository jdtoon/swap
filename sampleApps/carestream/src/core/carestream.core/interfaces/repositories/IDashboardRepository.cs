using carestream.core.dtos.dashboard;
using System.Data;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for retrieving dashboard-specific data.
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Gets the key statistics for the Patient Administration dashboard.
        /// </summary>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A DTO containing dashboard statistics.</returns>
        Task<DashboardStatsDto> GetDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Gets a list of the most recent patient visits for the dashboard.
        /// </summary>
        /// <param name="limit">The maximum number of recent patients to retrieve.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="RecentPatientDto"/>.</returns>
        Task<IEnumerable<RecentPatientDto>> GetRecentPatientsAsync(int limit = 5, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Gets a list of the most recent staff reports for the dashboard.
        /// </summary>
        /// <param name="limit">The maximum number of recent reports to retrieve.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="RecentStaffReportDto"/>.</returns>
        Task<IEnumerable<RecentStaffReportDto>> GetRecentStaffReportsAsync(int limit = 5, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
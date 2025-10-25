using carestream.core.dtos.admin.staffreport; // For StaffReportDto, CreateUpdateStaffReportDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for staff reports.
    /// </summary>
    public interface IStaffReportRepository
    {
        /// <summary>
        /// Retrieves a specific staff report by its ID.
        /// </summary>
        /// <param name="reportId">The ID of the report.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="StaffReportDto"/> if found; otherwise, null.</returns>
        Task<StaffReportDto?> GetStaffReportByIdAsync(int reportId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of staff reports for a specific facility, with optional filters.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to filter reports by.</param>
        /// <param name="options">Options for filtering, searching, and pagination (SearchTerm1 for title/content/author, SearchTerm2 for department/priority).</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="StaffReportDto"/> and the total count.</returns>
        Task<(IEnumerable<StaffReportDto> Items, int TotalCount)> GetAllStaffReportsAsync(int facilityId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new staff report.
        /// </summary>
        /// <param name="reportData">The DTO containing the report data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created report, or 0 if creation failed.</returns>
        Task<int> CreateStaffReportAsync(CreateUpdateStaffReportDto reportData, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing staff report.
        /// </summary>
        /// <param name="reportData">The DTO containing the updated report data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateStaffReportAsync(CreateUpdateStaffReportDto reportData, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deletes a staff report. (Consider deactivation instead for auditability).
        /// </summary>
        /// <param name="reportId">The ID of the report to delete.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the report was deleted, false otherwise.</returns>
        Task<bool> DeleteStaffReportAsync(int reportId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
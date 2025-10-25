using carestream.core.dtos.admin.staffreport;
using carestream.core.dtos.shared;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for managing staff reports.
    /// </summary>
    public interface IStaffReportService
    {
        /// <summary>
        /// Retrieves a view model containing a single staff report by its ID.
        /// </summary>
        /// <param name="reportId">The ID of the facility to retrieve reports for.</param>
        /// <returns>A <see cref="StaffReportDto"/> contains a single staff report dto.</returns>
        Task<StaffReportDto> GetStaffReportByReportId(int reportId);

        /// <summary>
        /// Retrieves a view model containing a paginated list of staff reports for a specific facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to retrieve reports for.</param>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="StaffReportListViewModel"/> containing the paginated list of reports.</returns>
        Task<StaffReportListViewModel> GetStaffReportsViewModelAsync(int facilityId, FilterAndPaginationOptions options);

        /// <summary>
        /// Creates a new staff report in the system.
        /// </summary>
        /// <param name="reportData">The DTO containing the details of the new staff report.</param>
        /// <returns>The ID of the newly created report, or 0 if creation failed.</returns>
        Task<int> CreateStaffReportAsync(CreateUpdateStaffReportDto reportData);

        /// <summary>
        /// Updates an existing staff report.
        /// </summary>
        /// <param name="reportData">The DTO containing the updated details of the staff report.</param>
        /// <returns>True if the report was successfully updated, false otherwise.</returns>
        Task<bool> UpdateStaffReportAsync(CreateUpdateStaffReportDto reportData);

        /// <summary>
        /// Deletes a staff report from the system.
        /// </summary>
        /// <param name="reportId">The ID of the report to delete.</param>
        /// <returns>True if the report was successfully deleted, false otherwise.</returns>
        Task<bool> DeleteStaffReportAsync(int reportId);
    }
}
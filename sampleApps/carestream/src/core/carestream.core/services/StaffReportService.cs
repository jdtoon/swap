using carestream.core.dtos.admin.staffreport;
using carestream.core.dtos.shared;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for managing staff reports.
    /// </summary>
    public class StaffReportService : IStaffReportService
    {
        private readonly IStaffReportRepository _staffReportRepository;
        private readonly ILogger<StaffReportService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaffReportService"/> class.
        /// </summary>
        /// <param name="staffReportRepository">The staff report data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public StaffReportService(IStaffReportRepository staffReportRepository, ILogger<StaffReportService> logger)
        {
            _staffReportRepository = staffReportRepository ?? throw new ArgumentNullException(nameof(staffReportRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a view model containing a paginated list of staff reports for a specific facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to retrieve reports for.</param>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="StaffReportListViewModel"/> containing the paginated list of reports.</returns>
        public async Task<StaffReportListViewModel> GetStaffReportsViewModelAsync(int facilityId, FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Service: Getting Staff Reports ViewModel for FacilityId: {FacilityId} with options: {@Options}", facilityId, options);

            if (facilityId <= 0)
            {
                _logger.LogWarning("Service: GetStaffReportsViewModelAsync called with invalid FacilityId: {FacilityId}", facilityId);
                return new StaffReportListViewModel();
            }

            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25;

            var (items, totalCount) = await _staffReportRepository.GetAllStaffReportsAsync(facilityId, options);

            var viewModel = new StaffReportListViewModel
            {
                Reports = items?.ToList() ?? new List<StaffReportDto>(),
                Pagination = new PaginationDto
                {
                    CurrentPage = options.PageNumber,
                    PageSize = options.PageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize)
                },
                Filters = options,
                FacilityId = facilityId
            };

            return viewModel;
        }

        /// <summary>
        /// Creates a new staff report in the system.
        /// </summary>
        /// <param name="reportData">The DTO containing the details of the new staff report.</param>
        /// <returns>The ID of the newly created report, or 0 if creation failed.</returns>
        public async Task<int> CreateStaffReportAsync(CreateUpdateStaffReportDto reportData)
        {
            _logger.LogInformation("Service: Creating new staff report with Title: '{Title}' for FacilityId: {FacilityId}", reportData.Title, reportData.FacilityId);

            if (reportData == null || string.IsNullOrWhiteSpace(reportData.Title) || string.IsNullOrWhiteSpace(reportData.Content) || reportData.AuthorUserId <= 0 || reportData.FacilityId <= 0)
            {
                _logger.LogWarning("Service: CreateStaffReportAsync called with invalid input: Missing required fields or invalid IDs.");
                return 0;
            }

            try
            {
                int newReportId = await _staffReportRepository.CreateStaffReportAsync(reportData);
                if (newReportId > 0)
                {
                    _logger.LogInformation("Service: Successfully created new staff report with ID: {ReportId}", newReportId);
                    reportData.ReportId = newReportId; // Update DTO with new ID if needed by caller
                }
                else
                {
                    _logger.LogError("Service: Failed to create new staff report with title '{Title}'. Repository returned no ID.", reportData.Title);
                }
                return newReportId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while creating staff report with title '{Title}'.", reportData.Title);
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing staff report.
        /// </summary>
        /// <param name="reportData">The DTO containing the updated details of the staff report.</param>
        /// <returns>True if the report was successfully updated, false otherwise.</returns>
        public async Task<bool> UpdateStaffReportAsync(CreateUpdateStaffReportDto reportData)
        {
            _logger.LogInformation("Service: Updating staff report ID: {ReportId} with Title: '{Title}'", reportData.ReportId, reportData.Title);

            if (reportData == null || reportData.ReportId <= 0 || string.IsNullOrWhiteSpace(reportData.Title) || string.IsNullOrWhiteSpace(reportData.Content) || reportData.AuthorUserId <= 0 || reportData.FacilityId <= 0)
            {
                _logger.LogWarning("Service: UpdateStaffReportAsync called with invalid input: Missing required fields or invalid IDs.");
                return false;
            }

            try
            {
                bool success = await _staffReportRepository.UpdateStaffReportAsync(reportData);
                if (success)
                {
                    _logger.LogInformation("Service: Successfully updated staff report ID: {ReportId}", reportData.ReportId);
                }
                else
                {
                    _logger.LogError("Service: Failed to update staff report ID: {ReportId}. Repository operation failed.", reportData.ReportId);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while updating staff report ID: {ReportId}.", reportData.ReportId);
                return false;
            }
        }

        /// <summary>
        /// Deletes a staff report from the system.
        /// </summary>
        /// <param name="reportId">The ID of the report to delete.</param>
        /// <returns>True if the report was successfully deleted, false otherwise.</returns>
        public async Task<bool> DeleteStaffReportAsync(int reportId)
        {
            _logger.LogInformation("Service: Deleting staff report ID: {ReportId}", reportId);

            if (reportId <= 0)
            {
                _logger.LogWarning("Service: DeleteStaffReportAsync called with invalid ReportId: {ReportId}", reportId);
                return false;
            }

            try
            {
                bool success = await _staffReportRepository.DeleteStaffReportAsync(reportId);
                if (success)
                {
                    _logger.LogInformation("Service: Successfully deleted staff report ID: {ReportId}", reportId);
                }
                else
                {
                    _logger.LogError("Service: Failed to delete staff report ID: {ReportId}. Repository operation failed.", reportId);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while deleting staff report ID: {ReportId}.", reportId);
                return false;
            }
        }

        /// <summary>
        /// Retrieves a view model containing a single staff report by its ID.
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<StaffReportDto> GetStaffReportByReportId(int reportId)
        {
            _logger.LogInformation("Service: Getting Staff Report by ReportId: {ReportId}", reportId);
            if (reportId <= 0)
            {
                _logger.LogWarning("Service: GetStaffReportByReportId called with invalid ReportId: {ReportId}", reportId);
                throw new ArgumentException("Invalid ReportId", nameof(reportId));
            }
            try
            {
                var report = _staffReportRepository.GetStaffReportByIdAsync(reportId);
                if (report == null)
                {
                    _logger.LogWarning("Service: No staff report found for ReportId: {ReportId}", reportId);
                    return Task.FromResult<StaffReportDto>(null!);
                }
                return report!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while retrieving staff report by ReportId: {ReportId}.", reportId);
                throw;
            }
        }
    }
}
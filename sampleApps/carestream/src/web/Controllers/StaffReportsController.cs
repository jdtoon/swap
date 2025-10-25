using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories; // Needed for IUserRepository
using carestream.core.dtos.admin.staffreport; // For StaffReportDto, CreateUpdateStaffReportDto, StaffReportListViewModel
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using carestream.core.infrastructure; // For ICurrentFacilityContext
using System.Security.Claims;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing staff reports.
    /// Accessible primarily by PatientAdmin.
    /// </summary>
    [Authorize(Roles = "PatientAdmin,SystemAdmin,Doctor,Nurse")] // As per FRS
    public class StaffReportsController : Controller
    {
        private readonly IStaffReportService _staffReportService;
        private readonly IUserRepository _userRepository; // To get internal user ID for recording actions
        private readonly ICurrentFacilityContext _facilityContext; // To get current facility ID
        private readonly ILogger<StaffReportsController> _logger;
        private readonly IDepartmentAdminService _departmentAdminService;

        public StaffReportsController(
            IStaffReportService staffReportService,
            IUserRepository userRepository,
            ICurrentFacilityContext facilityContext,
            ILogger<StaffReportsController> logger,
            IDepartmentAdminService departmentAdminService)
        {
            _staffReportService = staffReportService ?? throw new ArgumentNullException(nameof(staffReportService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _facilityContext = facilityContext ?? throw new ArgumentNullException(nameof(facilityContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _departmentAdminService = departmentAdminService ?? throw new ArgumentNullException(nameof(departmentAdminService));
        }

        /// <summary>
        /// GET: /StaffReports/Index
        /// Displays the main container for the staff reports list (FR-REP-001).
        /// </summary>
        /// <returns>A partial view for the staff reports page.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Controller: StaffReports/Index requested.");
            // The list will be loaded by HTMX into #report-list-container
            return PartialView(); // Renders Views/StaffReports/Index.cshtml
        }

        /// <summary>
        /// GET: /StaffReports/ReportListPartial
        /// Fetches and returns the partial view containing the paginated list of staff reports (FR-REP-002).
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A partial view displaying the staff reports list.</returns>
        [HttpGet]
        public async Task<IActionResult> ReportListPartial([FromQuery] FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Controller: Fetching ReportListPartial with options: {@Options}", options);

            if (!_facilityContext.IsFacilityContextSet || _facilityContext.CurrentFacilityId <= 0)
            {
                _logger.LogWarning("Controller: ReportListPartial called without valid facility context.");
                return PartialView("~/Views/Shared/_ErrorPartial", "No active facility context. Please select a facility.");
            }

            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25; // Default page size for reports

            var viewModel = await _staffReportService.GetStaffReportsViewModelAsync(_facilityContext.CurrentFacilityId, options);
            viewModel.Pagination.HxGetUrl = Url.Action("ReportListPartial", "StaffReports") ?? "";
            viewModel.Pagination.HxTarget = "#report-list-container";
            viewModel.Pagination.HxSwap = "innerHTML";

            return PartialView("_ReportListPartial", viewModel);
        }

        /// <summary>
        /// GET: /StaffReports/CreateEditModal/{reportId?}
        /// Returns a partial view for a modal to create a new staff report or edit an existing one (FR-REP-003).
        /// </summary>
        /// <param name="reportId">Optional ID of the report to edit. If null, a new report is created.</param>
        /// <returns>A partial view for the create/edit modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateEditModal(int? reportId)
        {
            _logger.LogInformation("Controller: Fetching CreateEditModal for ReportId: {ReportId}", reportId);
            CreateUpdateStaffReportDto model;

            if (!_facilityContext.IsFacilityContextSet || _facilityContext.CurrentFacilityId <= 0)
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"No active facility. Cannot create/edit report.\"}");
                return Content("", "text/html");
            }

            if (reportId.HasValue && reportId.Value > 0)
            {
                var existingReport = await _staffReportService.GetStaffReportByReportId(reportId.Value);

                if (existingReport == null)
                {
                    _logger.LogWarning("Controller: Report {ReportId} not found or not in current facility {FacilityId} for edit modal.", reportId, _facilityContext.CurrentFacilityId);
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Staff report not found or not in current facility.\"}");
                    return Content("", "text/html");
                }
                model = new CreateUpdateStaffReportDto
                {
                    ReportId = existingReport.ReportId,
                    FacilityId = existingReport.FacilityId,
                    DepartmentId = existingReport.DepartmentId,
                    Title = existingReport.Title,
                    Priority = existingReport.Priority,
                    Content = existingReport.Content,
                    AuthorUserId = existingReport.AuthorUserId
                };
            }
            else
            {
                model = new CreateUpdateStaffReportDto { FacilityId = _facilityContext.CurrentFacilityId };
            }

            ViewData["CurrentFacilityName"] = _facilityContext.CurrentFacilityName;
            var allDepartments = (await _departmentAdminService.GetAllDepartmentsForFacilityAsync(_facilityContext.CurrentFacilityId)).ToList();
            ViewData["Departments"] = allDepartments;
            return PartialView("_CreateEditReportModalPartial", model);
        }

        /// <summary>
        /// POST: /StaffReports/SaveReport
        /// Handles the creation or update of a staff report (FR-REP-005).
        /// </summary>
        /// <param name="dto">The DTO containing report data.</param>
        /// <returns>Refreshes the report list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReport([FromForm] CreateUpdateStaffReportDto dto)
        {
            _logger.LogInformation("Controller: Saving staff report ID: {ReportId} (Title: {Title}) for FacilityId: {FacilityId}", dto.ReportId, dto.Title, dto.FacilityId);

            if (!_facilityContext.IsFacilityContextSet || _facilityContext.CurrentFacilityId != dto.FacilityId)
            {
                _logger.LogWarning("Controller: SaveReport - Facility context mismatch or invalid. Current: {CurrentFacilityId}, DTO: {DtoFacilityId}", _facilityContext.CurrentFacilityId, dto.FacilityId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Facility context mismatch. Please re-load page.\"}");
                return Content("", "text/html");
            }
            
            ViewData["CurrentFacilityName"] = _facilityContext.CurrentFacilityName;
            var allDepartments = (await _departmentAdminService.GetAllDepartmentsForFacilityAsync(_facilityContext.CurrentFacilityId)).ToList();
            ViewData["Departments"] = allDepartments;

            var performingUserId = await GetInternalUserId();
            if (!performingUserId.HasValue)
            {
                _logger.LogError("Controller: SaveReport - Could not identify performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot save report.\"}");
                return Content("", "text/html");
            }
            dto.AuthorUserId = performingUserId.Value; // Ensure author is current user

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for Staff Report ID: {ReportId}.", dto.ReportId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content"); // Assuming generic modal
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_CreateEditReportModalPartial", dto);
            }

            bool success;
            string successMessage;
            if (dto.ReportId > 0)
            {
                success = await _staffReportService.UpdateStaffReportAsync(dto);
                successMessage = "Staff report updated successfully!";
            }
            else
            {
                success = await _staffReportService.CreateStaffReportAsync(dto) > 0;
                successMessage = "Staff report created successfully!";
            }

            if (success)
            {
                _logger.LogInformation("Controller: Staff report ID {ReportId} {Action} successfully.", dto.ReportId, dto.ReportId > 0 ? "updated" : "created");
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"{successMessage}\", \"closeAdminGenericModal\": true, \"refreshReportList\": true }}");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to {Action} staff report ID {ReportId}.", dto.ReportId > 0 ? "update" : "create", dto.ReportId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save report. Please try again.\"}");
                ModelState.AddModelError("", "Failed to save report. Check for duplicate title or system error.");
                return PartialView("_CreateEditReportModalPartial", dto);
            }
        }

        /// <summary>
        /// POST: /StaffReports/DeleteReport
        /// Deletes a staff report (FR-REP-005).
        /// </summary>
        /// <param name="reportId">The ID of the report to delete.</param>
        /// <returns>Triggers a refresh of the report list and a toast.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            _logger.LogInformation("Controller: Deleting staff report ID: {ReportId}", reportId);

            if (reportId <= 0)
            {
                _logger.LogWarning("Controller: DeleteReport called with invalid ReportId: {ReportId}", reportId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid Report ID.\"}");
                return Ok();
            }

            bool success = await _staffReportService.DeleteStaffReportAsync(reportId);

            if (success)
            {
                _logger.LogInformation("Controller: Staff report ID {ReportId} deleted successfully.", reportId);
                Response.Headers.Append("HX-Trigger-After-Swap", "{\"showToastSuccess\": \"Staff report deleted!\", \"refreshReportList\": true }");
            }
            else
            {
                _logger.LogError("Controller: Failed to delete staff report ID: {ReportId}.", reportId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to delete report. It might not exist or has dependencies.\"}");
            }
            return Ok();
        }

        // Helper to get the internal user ID from claims
        private async Task<int?> GetInternalUserId()
        {
            var userIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                var logtoSub = User.FindFirstValue("sub");
                if (!string.IsNullOrEmpty(logtoSub))
                {
                    return await _userRepository.GetUserIdByLogtoSubAsync(logtoSub);
                }
                return null;
            }
            return userId;
        }
    }
}
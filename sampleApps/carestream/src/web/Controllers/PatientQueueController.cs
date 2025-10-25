using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services; // For IPatientQueueService
using carestream.core.dtos.patientadmin; // For PatientQueueListViewModel, PatientQueueBoardViewModel
using carestream.core.dtos.shared;      // For FilterAndPaginationOptions
using System.Security.Claims;          // For User.FindFirstValue
using System.Threading.Tasks;          // For Task
using Microsoft.Extensions.Logging;    // For ILogger
using System;                          // For ArgumentNullException
using System.Web;
using carestream.core.interfaces.repositories;                      // For HttpUtility.JavaScriptStringEncode

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing the patient queue display (List and Board views).
    /// </summary>
    [Authorize(Roles = "PatientAdmin")] // Accessible by PatientAdmin role
    public class PatientQueueController : Controller
    {
        private readonly IPatientQueueService _patientQueueService;
        private readonly IUserRepository _userRepository; // Needed for internal user ID lookup
        private readonly ILogger<PatientQueueController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientQueueController"/> class.
        /// </summary>
        /// <param name="patientQueueService">The service for retrieving and managing patient queue data.</param>
        /// <param name="userRepository">The repository for user data, to get internal user ID.</param>
        /// <param name="logger">The logger for this controller.</param>
        public PatientQueueController(
            IPatientQueueService patientQueueService,
            IUserRepository userRepository,
            ILogger<PatientQueueController> logger)
        {
            _patientQueueService = patientQueueService ?? throw new ArgumentNullException(nameof(patientQueueService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /PatientQueue/Index
        /// Serves as the default entry point for the patient queue section,
        /// rendering either the list or board view based on parameters/defaults.
        /// This is the action that the main SickReport/Index.cshtml will call.
        /// </summary>
        /// <param name="options">Filtering and pagination options for the list view.</param>
        /// <param name="viewType">Specifies the type of view to render: "list" (default) or "board".</param>
        /// <returns>A partial view for either the list or board queue.</returns>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FilterAndPaginationOptions options, [FromQuery] string viewType = "list")
        {
            _logger.LogInformation("Controller: PatientQueue/Index called with ViewType: {ViewType} and Options: {@Options}", viewType, options);

            // Ensure options are valid if coming from a filter form
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 10; // Default page size for queue list

            if (viewType == "board")
            {
                var boardViewModel = await _patientQueueService.GetPatientQueueBoardViewModelAsync();
                return PartialView("_PatientQueueBoardPartial", boardViewModel);
            }
            else // Default to list view
            {
                var listViewModel = await _patientQueueService.GetPatientQueueListViewModelAsync(options);
                // Set HX attributes for pagination links within the list partial
                listViewModel.PaginationInfo.HxGetUrl = Url.Action("Index", "PatientQueue") ?? "";
                listViewModel.PaginationInfo.HxTarget = "#patient-queue-content-container"; // Target the main queue container
                listViewModel.PaginationInfo.HxSwap = "innerHTML";
                return PartialView("_PatientQueuePartial", listViewModel);
            }
        }

        /// <summary>
        /// GET: /PatientQueue/TogglePartial
        /// Returns the partial view containing the list/board view toggle buttons.
        /// </summary>
        /// <param name="currentViewType">The currently active view type ("list" or "board").</param>
        /// <returns>A partial view with the toggle buttons.</returns>
        [HttpGet]
        public IActionResult TogglePartial([FromQuery] string currentViewType = "list")
        {
            _logger.LogInformation("Controller: PatientQueue/TogglePartial called with currentViewType: {CurrentViewType}", currentViewType);
            ViewData["CurrentViewType"] = currentViewType;
            return PartialView("_PatientQueueTogglePartial");
        }

        /// <summary>
        /// POST: /PatientQueue/CallPatient
        /// Updates the status of a patient in the queue (e.g., marks them as 'called').
        /// This action is triggered from both list and board views.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <returns>An empty OK result with HTMX triggers for toast and queue refresh.</returns>
        [HttpPost]
        public async Task<IActionResult> CallPatient(int visitId)
        {
            _logger.LogInformation("Controller: CallPatient action called for VisitId: {VisitId}", visitId);

            if (visitId <= 0)
            {
                _logger.LogWarning("Controller: CallPatient called with invalid VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid visit identifier for call.\"}");
                return Ok();
            }

            // Retrieve internal user ID from claims (fast path)
            var userIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int internalUserId))
            {
                _logger.LogError("Controller: CallPatient - Could not identify or link current user (carestream_user_id claim missing/invalid).");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User identification error. Please re-login.\"}");
                return Ok();
            }

            bool success = await _patientQueueService.CallPatientAsync(visitId, internalUserId);

            if (success)
            {
                _logger.LogInformation("Controller: CallPatient successful for VisitId: {VisitId}. Triggering queue refresh.", visitId);
                Response.Headers.Append("HX-Trigger-After-Settle", "refreshPatientQueue"); // Custom event to refresh queue
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Patient status updated: Called.\"}");
            }
            else
            {
                _logger.LogWarning("Controller: CallPatient failed for VisitId: {VisitId} at service level.", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Could not update patient status. Patient may have been processed or an error occurred.\"}");
            }
            return Ok();
        }
    }
}
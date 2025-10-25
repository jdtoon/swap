using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories; // Needed for IUserRepository
using carestream.core.dtos.patient; // For PatientMedicalHistoryDto, CreateUpdatePatientMedicalHistoryDto
using System.Security.Claims;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing patient medical history records.
    /// Accessible by Doctor and Nurse roles.
    /// </summary>
    [Authorize(Roles = "Doctor,Nurse")]
    public class PatientHistoryController : Controller
    {
        private readonly IPatientHistoryService _patientHistoryService;
        private readonly IUserRepository _userRepository; // To get internal user ID for recording actions
        private readonly ILogger<PatientHistoryController> _logger;

        public PatientHistoryController(
            IPatientHistoryService patientHistoryService,
            IUserRepository userRepository,
            ILogger<PatientHistoryController> logger)
        {
            _patientHistoryService = patientHistoryService ?? throw new ArgumentNullException(nameof(patientHistoryService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /PatientHistory/Index/{patientId}
        /// Displays the main container for a patient's medical history list.
        /// This is designed to be loaded into a tab or a main content area within the consultation.
        /// </summary>
        /// <param name="patientId">The ID of the patient whose history to display.</param>
        /// <returns>A partial view for the medical history list.</returns>
        [HttpGet]
        public IActionResult Index(int patientId)
        {
            _logger.LogInformation("Controller: PatientHistory/Index requested for PatientId: {PatientId}", patientId);

            if (patientId <= 0)
            {
                _logger.LogWarning("Controller: PatientHistory/Index called with invalid PatientId: {PatientId}", patientId);
                return PartialView("~/Views/Shared/_ErrorPartial", "Invalid Patient ID for history.");
            }

            ViewData["PatientId"] = patientId;
            return PartialView(); // Renders Views/PatientHistory/Index.cshtml
        }

        /// <summary>
        /// GET: /PatientHistory/HistoryListPartial/{patientId}
        /// Fetches and returns the partial view containing the list of medical history entries for a patient.
        /// This is the primary target for HTMX calls within the Index view.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view displaying the list of medical history.</returns>
        [HttpGet]
        public async Task<IActionResult> HistoryListPartial(int patientId)
        {
            _logger.LogInformation("Controller: Fetching HistoryListPartial for PatientId: {PatientId}", patientId);
            if (patientId <= 0)
            {
                return PartialView("~/Views/Shared/_ErrorPartial", "Invalid Patient ID for history list.");
            }

            var historyItems = await _patientHistoryService.GetPatientMedicalHistoryAsync(patientId);
            ViewData["PatientId"] = patientId; // Pass PatientId back to the partial for form context
            return PartialView("_HistoryListPartial", historyItems);
        }

        /// <summary>
        /// GET: /PatientHistory/CreateEditModal/{patientId}/{historyId?}
        /// Returns a partial view for a modal to create a new medical history entry or edit an existing one.
        /// </summary>
        /// <param name="patientId">The ID of the patient the history belongs to.</param>
        /// <param name="historyId">Optional ID of the history entry to edit. If null, a new entry is created.</param>
        /// <returns>A partial view for the create/edit modal.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateEditModal(int patientId, int? historyId)
        {
            _logger.LogInformation("Controller: Fetching CreateEditModal for PatientId: {PatientId}, HistoryId: {HistoryId}", patientId, historyId);
            CreateUpdatePatientMedicalHistoryDto model;

            if (patientId <= 0)
            {
                return BadRequest("Invalid Patient ID.");
            }

            if (historyId.HasValue && historyId.Value > 0)
            {
                // Fetch existing history item for editing
                // Note: GetPatientMedicalHistoryAsync returns IEnumerable, need to filter client-side or add GetHistoryById to repo/service if frequently needed.
                var existingItem = (await _patientHistoryService.GetPatientMedicalHistoryAsync(patientId))
                                    .FirstOrDefault(h => h.HistoryId == historyId.Value);
                if (existingItem == null)
                {
                    _logger.LogWarning("Controller: History item {HistoryId} not found for PatientId {PatientId} for edit modal.", historyId, patientId);
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Medical history entry not found.\"}");
                    return Content("", "text/html");
                }
                model = new CreateUpdatePatientMedicalHistoryDto
                {
                    HistoryId = existingItem.HistoryId,
                    PatientId = patientId, // Ensure PatientId is set for the DTO
                    Type = existingItem.Type,
                    Description = existingItem.Description,
                    OnsetDate = existingItem.OnsetDate,
                    ResolutionDate = existingItem.ResolutionDate,
                    Severity = existingItem.Severity,
                    Notes = existingItem.Notes,
                    IsActive = existingItem.IsActive
                };
            }
            else
            {
                // New history item
                model = new CreateUpdatePatientMedicalHistoryDto { PatientId = patientId }; // Set PatientId for new DTO
            }

            ViewData["PatientId"] = patientId; // Pass PatientId to modal for form context
            return PartialView("_CreateEditHistoryModalPartial", model);
        }

        /// <summary>
        /// POST: /PatientHistory/SaveEntry
        /// Handles the creation or update of a medical history entry.
        /// </summary>
        /// <param name="dto">The DTO containing medical history data (including PatientId).</param>
        /// <returns>Refreshes the history list or re-renders modal with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEntry([FromForm] CreateUpdatePatientMedicalHistoryDto dto)
        {
            _logger.LogInformation("Controller: Saving medical history entry for PatientId: {PatientId}, HistoryId: {HistoryId}", dto.PatientId, dto.HistoryId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for medical history entry for PatientId: {PatientId}, HistoryId: {HistoryId}.", dto.PatientId, dto.HistoryId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content"); // Assuming a generic modal for this
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                ViewData["PatientId"] = dto.PatientId; // Re-pass patient ID for modal context
                return PartialView("_CreateEditHistoryModalPartial", dto);
            }

            var performingUserId = await GetInternalUserId();
            if (!performingUserId.HasValue)
            {
                _logger.LogError("Controller: SaveEntry - Could not identify performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot save history.\"}");
                return Content("", "text/html");
            }

            bool success = await _patientHistoryService.SavePatientMedicalHistoryEntryAsync(dto, performingUserId.Value); // MODIFIED: Call matches new service signature

            if (success)
            {
                _logger.LogInformation("Controller: Medical history entry {HistoryId} {Action} successfully for PatientId {PatientId}.", dto.HistoryId, dto.HistoryId > 0 ? "updated" : "created", dto.PatientId);
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"Medical history entry saved successfully!\", \"closeAdminGenericModal\": true, \"refreshPatientHistory\": {dto.PatientId} }}");
                return Content("", "text/html");
            }
            else
            {
                _logger.LogError("Controller: Failed to save medical history entry for PatientId: {PatientId}, HistoryId: {HistoryId}.", dto.PatientId, dto.HistoryId);
                Response.Headers.Append("HX-Retarget", "#admin_generic_modal_content");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save medical history entry. Please try again.\"}");
                ModelState.AddModelError("", "Failed to save entry. Check for duplicates or system error.");
                ViewData["PatientId"] = dto.PatientId; // Re-pass patient ID for modal context
                return PartialView("_CreateEditHistoryModalPartial", dto);
            }
        }

        /// <summary>
        /// POST: /PatientHistory/Deactivate
        /// Deactivates a medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to deactivate.</param>
        /// <param name="patientId">The ID of the patient (for refresh context).</param>
        /// <returns>Triggers a refresh of the history list and a toast.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int historyId, int patientId)
        {
            _logger.LogInformation("Controller: Deactivating medical history entry ID: {HistoryId} for PatientId: {PatientId}", historyId, patientId);

            if (historyId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: Deactivate called with invalid IDs. HistoryId: {HistoryId}, PatientId: {PatientId}", historyId, patientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid history or patient ID.\"}");
                return Ok();
            }

            var performingUserId = await GetInternalUserId();
            if (!performingUserId.HasValue)
            {
                _logger.LogError("Controller: Deactivate - Could not identify performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot deactivate history.\"}");
                return Ok();
            }

            bool success = await _patientHistoryService.DeactivatePatientMedicalHistoryEntryAsync(historyId, performingUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Medical history entry ID {HistoryId} deactivated successfully.", historyId);
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"History entry deactivated!\", \"refreshPatientHistory\": {patientId} }}");
            }
            else
            {
                _logger.LogError("Controller: Failed to deactivate medical history entry ID: {HistoryId}.", historyId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to deactivate history entry. It might be already inactive or has dependencies.\"}");
            }
            return Ok();
        }

        /// <summary>
        /// POST: /PatientHistory/Activate
        /// Activates a medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to activate.</param>
        /// <param name="patientId">The ID of the patient (for refresh context).</param>
        /// <returns>Triggers a refresh of the history list and a toast.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int historyId, int patientId)
        {
            _logger.LogInformation("Controller: Activating medical history entry ID: {HistoryId} for PatientId: {PatientId}", historyId, patientId);

            if (historyId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: Activate called with invalid IDs. HistoryId: {HistoryId}, PatientId: {PatientId}", historyId, patientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid history or patient ID.\"}");
                return Ok();
            }

            var performingUserId = await GetInternalUserId();
            if (!performingUserId.HasValue)
            {
                _logger.LogError("Controller: Activate - Could not identify performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot activate history.\"}");
                return Ok();
            }

            bool success = await _patientHistoryService.ActivatePatientMedicalHistoryEntryAsync(historyId, performingUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Medical history entry ID {HistoryId} activated successfully.", historyId);
                Response.Headers.Append("HX-Trigger-After-Swap", $"{{ \"showToastSuccess\": \"History entry activated!\", \"refreshPatientHistory\": {patientId} }}");
            }
            else
            {
                _logger.LogError("Controller: Failed to activate medical history entry ID: {HistoryId}.", historyId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to activate history entry. It might be already active or has dependencies.\"}");
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
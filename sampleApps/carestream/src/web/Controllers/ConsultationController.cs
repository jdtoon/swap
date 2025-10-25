using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories; // Still needed for IUserRepository
using carestream.core.dtos.consultation; // For ConsultationViewModel, Icd10CodeDto, ProcedureDto
using carestream.core.dtos.prescription; // For AddPrescriptionItemInputDto, MedicationSearchResultDto
using carestream.core.dtos.medication; // For SickNoteInputDto

namespace carestream.web.controllers
{
    [Authorize(Roles = "Doctor,Nurse")]
    public class ConsultationController : Controller
    {
        private readonly IConsultationService _consultationService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly ISickNoteService _sickNoteService;
        private readonly IUserRepository _userRepository; // Still needed for internal user ID lookup
        private readonly ILogger<ConsultationController> _logger;

        public ConsultationController(
            IConsultationService consultationService,
            IPrescriptionService prescriptionService,
            ISickNoteService sickNoteService,
            IUserRepository userRepository,
            ILogger<ConsultationController> logger)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _sickNoteService = sickNoteService ?? throw new ArgumentNullException(nameof(sickNoteService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initiates a consultation for a given visit and patient.
        /// Uses IConsultationService.StartConsultationSessionAsync to set status and assign doctor,
        /// then retrieves the view model.
        /// </summary>
        /// <param name="visitId">The ID of the visit to start consultation for.</param>
        /// <param name="patientId">The ID of the patient associated with the visit.</param>
        /// <returns>A partial view for the consultation layout.</returns>
        [HttpGet]
        public async Task<IActionResult> StartConsultation(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: StartConsultation requested for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: StartConsultation called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                TempData["ErrorMessage"] = "Invalid patient or visit identifier.";
                return PartialView("~/Views/Shared/_ErrorPartial");
            }

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Controller: StartConsultation failed: Could not determine current user ID (doctor/nurse). Cannot initiate consultation for VisitId: {VisitId}.", visitId);
                TempData["ErrorMessage"] = "User session error. Cannot initiate consultation.";
                return PartialView("~/Views/Shared/_ErrorPartial");
            }

            // NEW: Call the service method to update status and assign doctor/nurse
            bool sessionStarted = await _consultationService.StartConsultationSessionAsync(visitId, patientId, currentUserId.Value);
            if (!sessionStarted)
            {
                _logger.LogError("Controller: Failed to start/resume consultation session for VisitId: {VisitId}. Status update or assignment failed.", visitId);
                TempData["ErrorMessage"] = "Failed to start consultation session. Please try again.";
                return PartialView("~/Views/Shared/_ErrorPartial");
            }

            var viewModel = await _consultationService.GetConsultationViewModelAsync(visitId, patientId);

            if (viewModel.PatientBanner == null || viewModel.PatientBanner.VisitId == 0) // Check if valid data was returned
            {
                _logger.LogWarning("Controller: Failed to retrieve consultation view model after session started for VisitId: {VisitId}, PatientId: {PatientId}.", visitId, patientId);
                TempData["ErrorMessage"] = "Consultation data not found after session started. Please report this issue.";
                return PartialView("~/Views/Shared/_ErrorPartial");
            }

            return PartialView("_ConsultationLayout", viewModel);
        }

        /// <summary>
        /// Gets the content for the Vital Signs tab within a consultation.
        /// Refactored to use IConsultationService.GetConsultationViewModelAsync.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VitalsTab(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Fetching VitalsTab for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            var viewModel = await _consultationService.GetConsultationViewModelAsync(visitId, patientId);

            if (viewModel.PatientBanner == null || viewModel.PatientBanner.VisitId == 0)
            {
                _logger.LogWarning("Controller: Could not retrieve consultation view model for VitalsTab. VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);
                return PartialView("~/Views/Shared/_ErrorPartial", "Could not load vitals information.");
            }

            viewModel.ActiveTab = "VitalSigns"; // Ensure the correct tab is marked active
            return PartialView("_ConsultationVitalsTab", viewModel);
        }

        /// <summary>
        /// Loads the content for the Medications tab.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MedicationsTab(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Fetching MedicationsTab for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("MedicationsTab called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                return PartialView("~/Views/Shared/_ErrorPartial", "Invalid visit or patient identifier for medications tab.");
            }

            var viewModel = await _prescriptionService.GetMedicationsViewModelAsync(visitId, patientId);

            if (viewModel == null)
            {
                _logger.LogError("Controller: GetMedicationsViewModelAsync returned null for VisitId: {VisitId}, PatientId: {PatientId}.", visitId, patientId);
                return PartialView("~/Views/Shared/_ErrorPartial", "Could not load medication information for this consultation.");
            }

            ViewData["VisitIdForRemove"] = visitId;
            return PartialView("_ConsultationMedicationsTab", viewModel);
        }

        /// <summary>
        /// Loads the content for the Sick Note tab within a consultation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SickNoteTab(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Fetching SickNoteTab for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);
            if (visitId <= 0)
            {
                _logger.LogWarning("SickNoteTab called with invalid VisitId: {VisitId}", visitId);
                return PartialView("~/Views/Shared/_ErrorPartial", "Invalid visit identifier for sick note.");
            }

            var sickNoteData = await _sickNoteService.GetSickNoteForVisitAsync(visitId);

            if (sickNoteData == null)
            {
                _logger.LogInformation("No existing sick note found for VisitId: {VisitId}. Initializing new DTO.", visitId);
                sickNoteData = new SickNoteInputDto { VisitId = visitId };
            }
            else
            {
                _logger.LogInformation("Existing sick note found for VisitId: {VisitId}, SickNoteId: {SickNoteId}", visitId, sickNoteData.SickNoteId);
            }

            ViewData["PatientId"] = patientId; // Still needed for the view for context

            return PartialView("_ConsultationSickNoteTab", sickNoteData);
        }

        /// <summary>
        /// Saves doctor's notes for a specific visit.
        /// Refactored to use IConsultationService.UpdateDoctorNotesAsync.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveDoctorNotes(int visitId, string doctorNotes)
        {
            _logger.LogInformation("Controller: Attempting to save doctor notes for VisitId: {VisitId}", visitId);

            if (visitId <= 0)
            {
                _logger.LogWarning("Controller: SaveDoctorNotes called with invalid VisitId: {VisitId}.", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid Visit ID for saving notes.\"}");
                return Ok(); // Return OK for HTMX to handle the toast
            }

            bool success = await _consultationService.UpdateDoctorNotesAsync(visitId, doctorNotes);

            if (success)
            {
                _logger.LogInformation("Controller: Successfully saved doctor notes for VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Successfully saved doctor notes!\"}");
                return Ok();
            }
            else
            {
                _logger.LogError("Controller: Failed to save doctor notes for VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save doctor notes.\"}");
                return Ok(); // Return OK for HTMX to handle the toast
            }
        }

        /// <summary>
        /// Searches for ICD-10 codes based on a search term.
        /// Uses IConsultationService.SearchIcd10CodesAsync (FR-DN-010).
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>A partial view with search results.</returns>
        [HttpGet] // Changed to Get for search, easier for hx-trigger="keyup changed delay:300ms"
        public async Task<IActionResult> SearchIcd10Codes(string searchTerm)
        {
            _logger.LogInformation("Controller: Searching ICD-10 codes with term: '{SearchTerm}'", searchTerm);
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return PartialView("_Icd10CodeSearchResults", Enumerable.Empty<Icd10CodeDto>());
            }

            var results = await _consultationService.SearchIcd10CodesAsync(searchTerm);
            return PartialView("_Icd10CodeSearchResults", results);
        }

        /// <summary>
        /// Searches for procedures based on a search term.
        /// Uses IConsultationService.SearchProceduresAsync (FR-DN-011).
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>A partial view with search results.</returns>
        [HttpGet] // Changed to Get for search
        public async Task<IActionResult> SearchProcedures(string searchTerm)
        {
            _logger.LogInformation("Controller: Searching procedures with term: '{SearchTerm}'", searchTerm);
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return PartialView("_ProcedureSearchResults", Enumerable.Empty<ProcedureDto>());
            }

            var results = await _consultationService.SearchProceduresAsync(searchTerm);
            return PartialView("_ProcedureSearchResults", results);
        }

        /// <summary>
        /// Saves the selected ICD-10 diagnoses to the visit.
        /// Uses IConsultationService.SaveVisitDiagnosisAsync.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="icd10CodeIds">Comma-separated string of ICD-10 code IDs.</param>
        /// <returns>An empty OK result with HTMX triggers for toasts and refresh, or an error message.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDiagnosis(int visitId, int patientId, string? icd10CodeIds)
        {
            _logger.LogInformation("Controller: Attempting to save diagnosis for VisitId: {VisitId}, PatientId: {PatientId} with codes: {Codes}", visitId, patientId, icd10CodeIds);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: SaveDiagnosis called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid visit or patient ID for saving diagnosis.\"}");
                return Ok();
            }

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Controller: SaveDiagnosis failed: Could not determine current user ID.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot save diagnosis.\"}");
                return Ok();
            }

            var parsedIcd10Ids = ParseIntList(icd10CodeIds);
            bool success = await _consultationService.SaveVisitDiagnosisAsync(visitId, patientId, parsedIcd10Ids, currentUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Successfully saved diagnosis for VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Diagnosis saved successfully!\"}");
            }
            else
            {
                _logger.LogError("Controller: Failed to save diagnosis for VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save diagnosis. Please try again.\"}");
            }
            return Ok();
        }

        /// <summary>
        /// Saves the selected procedures to the visit.
        /// Uses IConsultationService.SaveVisitProceduresAsync.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="procedureIds">Comma-separated string of procedure IDs.</param>
        /// <returns>An empty OK result with HTMX triggers for toasts and refresh, or an error message.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProcedures(int visitId, int patientId, string? procedureIds)
        {
            _logger.LogInformation("Controller: Attempting to save procedures for VisitId: {VisitId}, PatientId: {PatientId} with codes: {Codes}", visitId, patientId, procedureIds);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: SaveProcedures called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid visit or patient ID for saving procedures.\"}");
                return Ok();
            }

            var currentUserId = await GetInternalUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogError("Controller: SaveProcedures failed: Could not determine current user ID.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot save procedures.\"}");
                return Ok();
            }

            var parsedProcedureIds = ParseIntList(procedureIds);
            bool success = await _consultationService.SaveVisitProceduresAsync(visitId, patientId, parsedProcedureIds, currentUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Successfully saved procedures for VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Procedures saved successfully!\"}");
            }
            else
            {
                _logger.LogError("Controller: Failed to save procedures for VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save procedures. Please try again.\"}");
            }
            return Ok();
        }

        /// <summary>
        /// Searches for medications based on a term. Called via HTMX from the Medications tab.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchMedications(string searchTerm, int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Searching medications with term '{SearchTerm}' for VisitId {VisitId}", searchTerm, visitId);
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return PartialView("_MedicationSearchResults", Enumerable.Empty<MedicationSearchResultDto>());
            }

            var results = await _prescriptionService.SearchMedicationsAsync(searchTerm);
            ViewData["VisitId"] = visitId;
            ViewData["PatientId"] = patientId;
            return PartialView("_MedicationSearchResults", results);
        }

        /// <summary>
        /// Adds a medication item to the current prescription for the visit.
        /// Called via HTMX. Returns an updated list of current prescription items.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescriptionItem([FromForm] AddPrescriptionItemInputDto inputDto)
        {
            _logger.LogInformation("Controller: Attempting to add prescription item for VisitId {VisitId}, MedicationId {MedicationId}", inputDto.VisitId, inputDto.MedicationId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: AddPrescriptionItem called with invalid ModelState for VisitId {VisitId}.", inputDto.VisitId);
                Response.Headers.Append("HX-Retarget", "#medication-add-form-status");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                return PartialView("_ValidationErrorMessages", ModelState);
            }

            var prescribingUserId = await GetInternalUserId();
            if (!prescribingUserId.HasValue)
            {
                _logger.LogError("Controller: AddPrescriptionItem - Could not identify or link prescribing user.");
                Response.Headers.Append("HX-Retarget", "#medication-add-form-status");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                return Content("<div class='text-error text-sm'>Error: User session invalid.</div>", "text/html");
            }


            var updatedItems = await _prescriptionService.AddPrescriptionItemAsync(inputDto, prescribingUserId.Value);
            ViewData["VisitIdForRemove"] = inputDto.VisitId;

            // Trigger event on success with detail about item presence
            var hasItems = updatedItems != null && updatedItems.Any();
            var hasItemsReturn = !hasItems ? "false" : "true";
            Response.Headers.Append("HX-Trigger", $"{{ \"prescription-items-updated\": {{ \"hasNoItems\": {hasItemsReturn} }}, \"clear-add-prescription-form\": true }}");

            return PartialView("_CurrentPrescriptionItems", updatedItems);
        }

        /// <summary>
        /// Removes a medication item from the current prescription for the visit.
        /// Called via HTMX. Returns an updated list of current prescription items.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemovePrescriptionItem(int prescriptionItemId, int visitId)
        {
            _logger.LogInformation("Controller: Attempting to remove prescription item ID {PrescriptionItemId} for VisitId {VisitId}", prescriptionItemId, visitId);
            if (prescriptionItemId <= 0 || visitId <= 0) return BadRequest("Invalid identifiers.");

            var updatedItems = await _prescriptionService.RemovePrescriptionItemAsync(prescriptionItemId, visitId);
            ViewData["VisitIdForRemove"] = visitId;

            // Trigger event on success with detail about item presence
            var hasItems = updatedItems != null && updatedItems.Any();
            var hasItemsReturn = !hasItems ? "false" : "true";
            Response.Headers.Append("HX-Trigger", $"{{ \"prescription-items-updated\": {{ \"hasNoItems\": {hasItemsReturn} }} }}");

            return PartialView("_CurrentPrescriptionItems", updatedItems);
        }

        /// <summary>
        /// Finalizes the prescription and marks items as sent to pharmacy.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPrescriptionToPharmacy(int visitId)
        {
            _logger.LogInformation("Controller: Attempting to send prescription to pharmacy for VisitId {VisitId}", visitId);
            if (visitId <= 0) return BadRequest("Invalid Visit ID.");

            var sentByUserId = await GetInternalUserId();
            if (!sentByUserId.HasValue)
            {
                _logger.LogError("Controller: SendPrescriptionToPharmacy - Could not identify or link sending user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid for sending prescription.\"}");
                return Ok();
            }

            bool success = await _prescriptionService.SendPrescriptionToPharmacyAsync(visitId, sentByUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Prescription for VisitId {VisitId} successfully sent to pharmacy.", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Prescription sent to pharmacy!\"}");
                var viewModel = await _prescriptionService.GetMedicationsViewModelAsync(visitId, 0); // patientId might not be needed just for reload
                ViewData["VisitIdForRemove"] = visitId;
                return PartialView("_ConsultationMedicationsTab", viewModel);
            }
            else
            {
                _logger.LogWarning("Controller: Failed to send prescription to pharmacy for VisitId {VisitId}.", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to send prescription. No items or error.\"}");
                return Ok();
            }
        }

        /// <summary>
        /// Saves (creates or updates) the sick note for a visit.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSickNote([FromForm] SickNoteInputDto sickNoteInput)
        {
            _logger.LogInformation("Controller: Attempting to save sick note for VisitId: {VisitId}, SickNoteId: {SickNoteId}",
                sickNoteInput.VisitId, sickNoteInput.SickNoteId);

            // Fetch patientId for ViewData, now using consultation service if needed, or directly from VisitId if available
            var patientIdForView = (await _consultationService.GetConsultationViewModelAsync(sickNoteInput.VisitId, 0))?.PatientBanner?.PatientId ?? 0;
            ViewData["PatientId"] = patientIdForView;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: SaveSickNote called with invalid ModelState for VisitId {VisitId}.", sickNoteInput.VisitId);
                if (sickNoteInput.SickNoteId.HasValue && sickNoteInput.SickNoteId > 0 && string.IsNullOrEmpty(sickNoteInput.IssuedByUserName))
                {
                    var existingNote = await _sickNoteService.GetSickNoteForVisitAsync(sickNoteInput.VisitId);
                    sickNoteInput.IssuedByUserName = existingNote?.IssuedByUserName;
                    sickNoteInput.IssuedAt = existingNote?.IssuedAt;
                }
                return PartialView("_ConsultationSickNoteTab", sickNoteInput);
            }

            var internalUserId = await GetInternalUserId();
            if (!internalUserId.HasValue)
            {
                _logger.LogError("Controller: SaveSickNote - Could not identify or link performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot save sick note.\"}");
                return PartialView("_ConsultationSickNoteTab", sickNoteInput); // Re-render with error
            }

            var savedSickNote = await _sickNoteService.SaveSickNoteAsync(sickNoteInput, internalUserId.Value);

            if (savedSickNote != null)
            {
                _logger.LogInformation("Controller: Sick note successfully saved for VisitId: {VisitId}, SickNoteId: {SickNoteId}",
                    savedSickNote.VisitId, savedSickNote.SickNoteId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Sick note saved successfully!\"}");
                return PartialView("_ConsultationSickNoteTab", savedSickNote);
            }
            else
            {
                _logger.LogError("Controller: Failed to save sick note for VisitId: {VisitId}", sickNoteInput.VisitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save sick note. Please try again.\"}");
                if (sickNoteInput.SickNoteId.HasValue && sickNoteInput.SickNoteId > 0 && string.IsNullOrEmpty(sickNoteInput.IssuedByUserName))
                {
                    var existingNote = await _sickNoteService.GetSickNoteForVisitAsync(sickNoteInput.VisitId);
                    sickNoteInput.IssuedByUserName = existingNote?.IssuedByUserName;
                    sickNoteInput.IssuedAt = existingNote?.IssuedAt;
                }
                return PartialView("_ConsultationSickNoteTab", sickNoteInput);
            }
        }

        /// <summary>
        /// Finalizes the current consultation, updating the visit status.
        /// Refactored to use IConsultationService.FinalizeConsultationAsync.
        /// </summary>
        /// <param name="visitId">The ID of the visit to finalize.</param>
        /// <param name="patientId">The ID of the patient (for context/logging).</param>
        [HttpPost]
        public async Task<IActionResult> FinalizeConsultation(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: Attempting to finalize consultation for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0)
            {
                _logger.LogWarning("FinalizeConsultation called with invalid VisitId: {VisitId}", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Invalid visit ID for finalization.\"}");
                return Ok();
            }

            var internalUserId = await GetInternalUserId();
            if (!internalUserId.HasValue)
            {
                _logger.LogError("Controller: FinalizeConsultation - Could not identify or link performing user for VisitId: {VisitId}.", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Cannot finalize consultation.\"}");
                return Ok();
            }

            bool success = await _consultationService.FinalizeConsultationAsync(visitId, internalUserId.Value);

            if (success)
            {
                _logger.LogInformation("Controller: Consultation for VisitId: {VisitId} successfully finalized.", visitId);
                Response.Headers.Append("HX-Redirect", Url.Action("DoctorDashboard", "Dashboard"));
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Consultation finalized successfully!\"}");
                return Ok();
            }
            else
            {
                _logger.LogError("Controller: Failed to finalize consultation for VisitId: {VisitId}.", visitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to finalize consultation. Please try again.\"}");
                return Ok();
            }
        }

        // Helper to get the internal user ID from claims
        private async Task<int?> GetInternalUserId()
        {
            var userIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                // Fallback to Logto sub if custom claim not found, though middleware should ensure it.
                var logtoSub = User.FindFirstValue("sub");
                if (!string.IsNullOrEmpty(logtoSub))
                {
                    return await _userRepository.GetUserIdByLogtoSubAsync(logtoSub);
                }
                return null;
            }
            return userId;
        }

        // Helper to parse a comma-separated string of integers
        private List<int> ParseIntList(string? commaSeparatedIds)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedIds))
            {
                return new List<int>();
            }
            return commaSeparatedIds.Split(',')
                                    .Select(s => s.Trim())
                                    .Where(s => int.TryParse(s, out _))
                                    .Select(int.Parse)
                                    .ToList();
        }
    }
}
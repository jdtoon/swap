using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.patient;
using carestream.core.dtos.visit;
using carestream.core.dtos.checkin;
using System.Security.Claims;
using System.Web;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for handling Sick Report functionalities, including patient lookup and check-in.
    /// </summary>
    [Authorize(Roles = "PatientAdmin")]
    public class SickReportController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SickReportController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SickReportController"/> class.
        /// </summary>
        /// <param name="patientService">The patient service.</param>
        /// <param name="userRepository">The user repository (for looking up internal user ID from Logto sub).</param>
        /// <param name="logger">The logger for this controller.</param>
        public SickReportController(
            IPatientService patientService,
            IUserRepository userRepository,
            ILogger<SickReportController> logger
            /* Removed: ,IPatientAdminService patientAdminService */) // Removed from constructor
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /SickReport/Index
        /// Displays the main partial view for the Sick Report feature,
        /// which includes the patient lookup form and an area for results.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Displaying initial Sick Report lookup view.");
            Response.Headers.Append("HX-Trigger-After-Settle", "refreshPatientQueue");

            return PartialView();
        }

        /// <summary>
        /// GET: /SickReport/LookupFormPartial
        /// Returns the initial state of the lookup form and empty result area.
        /// </summary>
        [HttpGet]
        public IActionResult LookupFormPartial()
        {
            _logger.LogInformation("Fetching initial patient lookup form partial.");
            return PartialView("_LookupFormAndResultAreaPartial");
        }

        /// <summary>
        /// POST: /SickReport/LookupPatient
        /// Handles the patient lookup request based on a Force Number.
        /// Returns the _PatientFound.cshtml or _PatientNotFound.cshtml partial.
        /// </summary>
        /// <param name="forceNumber">The force number to look up.</param>
        /// <returns>The appropriate partial view for the lookup result.</returns>
        [HttpPost]
        public async Task<IActionResult> LookupPatient(string forceNumber, bool showActions = true)
        {
            _logger.LogInformation("Attempting patient lookup for ForceNumber: {ForceNumber}", forceNumber);
            PatientDetailDto? patient = null;
            ActiveVisitDto? activeVisit = null;

            if (!string.IsNullOrWhiteSpace(forceNumber))
            {
                patient = await _patientService.GetPatientByForceNumberAsync(forceNumber);
                if (patient != null)
                {
                    _logger.LogInformation("Patient found for ForceNumber: {ForceNumber}, PatientId: {PatientId}", forceNumber, patient.PatientId);
                    activeVisit = await _patientService.GetActiveVisitForPatientAsync(patient.PatientId);
                    ViewData["ActiveVisitData"] = activeVisit;
                    patient.ShowActionButtons = showActions;
                    return PartialView("_PatientFound", patient);
                }
                else
                {
                    _logger.LogWarning("No patient found for ForceNumber: {ForceNumber}", forceNumber);
                    return PartialView("_PatientNotFound", forceNumber);
                }
            }
            else
            {
                _logger.LogWarning("LookupPatient called with empty or whitespace ForceNumber.");
                return PartialView("_PatientNotFound", null);
            }
        }

        /// <summary>
        /// Helper to get the internal user ID from Logto's sub claim.
        /// </summary>
        /// <param name="patientIdForErrorDto">Patient ID used for error DTO context.</param>
        /// <returns>A tuple indicating success, the user ID, and an error DTO if failed.</returns>
        private (bool success, int userId, CheckinConfirmationDto? errorDto) GetPerformingUserId(int patientIdForErrorDto)
        {
            var logtoSub = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(logtoSub))
            {
                _logger.LogError("Could not find 'sub' claim for performing user for PatientId: {PatientIdForErrorDto}.", patientIdForErrorDto);
                return (false, 0, new CheckinConfirmationDto { Success = false, PatientName = $"Patient {patientIdForErrorDto}", ErrorMessage = "User session error. Please try logging in again." });
            }
            var internalUserId = User.FindFirstValue("carestream_user_id"); // Get internal user ID from claim
            if (string.IsNullOrEmpty(internalUserId) || !int.TryParse(internalUserId, out int parsedUserId))
            {
                _logger.LogError("Logto user sub '{LogtoSub}' not linked to internal user for PatientId: {PatientIdForErrorDto}. Or claim missing.", logtoSub, patientIdForErrorDto);
                return (false, 0, new CheckinConfirmationDto { Success = false, PatientName = $"Patient {patientIdForErrorDto}", ErrorMessage = "User account not fully configured. Contact administrator." });
            }
            return (true, parsedUserId, null);
        }

        /// <summary>
        /// POST: /SickReport/CreateNewVisitAndCheckin
        /// Creates a new visit for the patient and checks them in.
        /// </summary>
        /// <param name="dto">The DTO containing the patient ID, brief reason, and additional notes.</param>
        /// <returns>A partial view confirming the check-in or the form with validation errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewVisitAndCheckin([FromForm] CheckinInputDto dto)
        {
            _logger.LogInformation("Attempting to create new visit with input: {@Dto}", dto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for new check-in for PatientId: {PatientId}", dto.PatientId);
                var patient = await _patientService.GetPatientDetailByIdAsync(dto.PatientId);
                if (patient == null)
                {
                    _logger.LogError("Patient not found for PatientId: {PatientId} during validation re-render.", dto.PatientId);
                    Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"An unexpected error occurred: Patient not found for re-render.\"}");
                    return PartialView("~/Views/Shared/_ErrorPartial"); // Use shared error partial
                }
                var activeVisit = await _patientService.GetActiveVisitForPatientAsync(dto.PatientId);
                ViewData["ActiveVisitData"] = activeVisit;
                ViewData["CheckinInputDto"] = dto;
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct the errors.\"}");
                return PartialView("_PatientFound", patient);
            }

            var userResult = GetPerformingUserId(dto.PatientId);
            if (!userResult.success)
            {
                return PartialView("_CheckinConfirmation", userResult.errorDto);
            }

            var confirmation = await _patientService.CreateNewVisitAndCheckinAsync(dto.PatientId, userResult.userId, dto.BriefReason, dto.AdditionalNotes);

            confirmation.BriefReason = dto.BriefReason;
            confirmation.AdditionalNotes = dto.AdditionalNotes;

            if (confirmation.Success)
            {
                Response.Headers.Append("HX-Trigger-After-Settle", "refreshPatientQueue"); // NEW trigger name
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Patient checked in successfully!\"}");
            }
            else
            {
                Response.Headers.Append("HX-Trigger", HttpUtility.JavaScriptStringEncode($"{{ \"showToastError\": \"{confirmation.ErrorMessage}\" }}"));
            }
            return PartialView("_CheckinConfirmation", confirmation);
        }

        /// <summary>
        /// POST: /SickReport/ResumeActiveVisit
        /// Resumes an existing active visit for the patient.
        /// </summary>
        /// <param name="visitId">The ID of the visit to resume.</param>
        /// <param name="patientId">The ID of the patient associated with the visit.</param>
        /// <returns>A partial view confirming the visit resumption.</returns>
        [HttpPost]
        public async Task<IActionResult> ResumeActiveVisit(int visitId, int patientId)
        {
            var userResult = GetPerformingUserId(patientId);
            if (!userResult.success)
            {
                return PartialView("_CheckinConfirmation", userResult.errorDto);
            }
            _logger.LogInformation("ResumeActiveVisit for VisitId: {VisitId}, PatientId: {PatientId} by UserId: {UserId}", visitId, patientId, userResult.userId);
            var confirmation = await _patientService.ResumeActiveVisitAsync(visitId, patientId, userResult.userId);
            if (confirmation.Success)
            {
                Response.Headers.Append("HX-Trigger-After-Settle", "refreshPatientQueue"); // NEW trigger name
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Visit resumed successfully!\"}");
            }
            else
            {
                Response.Headers.Append("HX-Trigger", HttpUtility.JavaScriptStringEncode($"{{ \"showToastError\": \"{confirmation.ErrorMessage}\" }}"));
            }
            return PartialView("_CheckinConfirmation", confirmation);
        }

        /// <summary>
        /// POST: /SickReport/CloseAndStartNewVisit
        /// Closes an old active visit and starts a new one for the patient.
        /// </summary>
        /// <param name="oldVisitId">The ID of the old visit to close.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view confirming the new visit creation.</returns>
        [HttpPost]
        public async Task<IActionResult> CloseAndStartNewVisit(int oldVisitId, int patientId)
        {
            var userResult = GetPerformingUserId(patientId);
            if (!userResult.success)
            {
                return PartialView("_CheckinConfirmation", userResult.errorDto);
            }
            _logger.LogInformation("CloseAndStartNewVisit for OldVisitId: {OldVisitId}, PatientId: {PatientId} by UserId: {UserId}", oldVisitId, patientId, userResult.userId);
            var confirmation = await _patientService.CloseAndStartNewVisitAsync(oldVisitId, patientId, userResult.userId);
            if (confirmation.Success)
            {
                Response.Headers.Append("HX-Trigger-After-Settle", "refreshPatientQueue"); // NEW trigger name
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Old visit closed, new visit started successfully!\"}");
            }
            else
            {
                Response.Headers.Append("HX-Trigger", HttpUtility.JavaScriptStringEncode($"{{ \"showToastError\": \"{confirmation.ErrorMessage}\" }}"));
            }
            return PartialView("_CheckinConfirmation", confirmation);
        }

        /// <summary>
        /// GET: /SickReport/ContactDisplayPartial/{patientId}
        /// Fetches and returns the partial view for displaying patient contact information.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view displaying contact details.</returns>
        [HttpGet]
        public async Task<IActionResult> ContactDisplayPartial(int patientId)
        {
            _logger.LogInformation("Fetching contact display partial for PatientId: {PatientId}", patientId);
            var patientDetail = await _patientService.GetPatientDetailByIdAsync(patientId);

            if (patientDetail == null)
            {
                _logger.LogWarning("ContactDisplayPartial: Patient not found for ID: {PatientId}", patientId);
                return Content("<div class=\"text-error\">Error: Patient not found.</div>");
            }
            return PartialView("_PatientContactDisplayPartial", patientDetail);
        }

        /// <summary>
        /// GET: /SickReport/EditContactInfoForm/{patientId}
        /// Fetches and returns the partial view for editing patient contact information.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view containing the edit form.</returns>
        [HttpGet]
        public async Task<IActionResult> EditContactInfoForm(int patientId)
        {
            _logger.LogInformation("Fetching edit contact info form for PatientId: {PatientId}", patientId);
            var contactInfo = await _patientService.GetPatientContactInfoForEditAsync(patientId);
            if (contactInfo == null)
            {
                _logger.LogWarning("EditContactInfoForm: Contact info not found for PatientId: {PatientId}", patientId);
                return Content("<div class=\"text-error\">Error: Contact information not found.</div>");
            }
            return PartialView("_EditPatientContactInfoFormPartial", contactInfo);
        }

        /// <summary>
        /// POST: /SickReport/UpdateContactInfo
        /// Handles the submission of the patient contact information edit form.
        /// </summary>
        /// <param name="dto">The DTO containing the updated contact information.</param>
        /// <returns>The updated display partial or the form with validation errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContactInfo([FromForm] EditPatientContactInfoDto dto)
        {
            _logger.LogInformation("Attempting to update contact info for PatientId: {PatientId}", dto.PatientId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for contact info update for PatientId: {PatientId}", dto.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct the errors.\"}");
                return PartialView("_EditPatientContactInfoFormPartial", dto);
            }

            bool success = await _patientService.UpdatePatientContactInfoAsync(dto);

            if (success)
            {
                _logger.LogInformation("Successfully updated contact info for PatientId: {PatientId}", dto.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Contact information updated successfully!\"}");
                var updatedPatientDetail = await _patientService.GetPatientDetailByIdAsync(dto.PatientId);
                return PartialView("_PatientContactDisplayPartial", updatedPatientDetail);
            }
            else
            {
                _logger.LogError("Failed to update contact info for PatientId: {PatientId}", dto.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to update contact information. Please try again.\"}");
                ModelState.AddModelError("", "An unexpected error occurred during update. Please try again.");
                return PartialView("_EditPatientContactInfoFormPartial", dto);
            }
        }

        /// <summary>
        /// GET: /SickReport/EmergencyContactDisplayPartial/{patientId}
        /// Fetches and returns the partial view for displaying patient emergency contact information.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view displaying emergency contact details.</returns>
        [HttpGet]
        public async Task<IActionResult> EmergencyContactDisplayPartial(int patientId)
        {
            _logger.LogInformation("Fetching emergency contact display partial for PatientId: {PatientId}", patientId);
            var patientDetail = await _patientService.GetPatientDetailByIdAsync(patientId);

            if (patientDetail == null)
            {
                _logger.LogWarning("EmergencyContactDisplayPartial: Patient not found for ID: {PatientId}", patientId);
                return Content("<div class=\"text-error\">Error: Patient not found.</div>");
            }
            return PartialView("_PatientEmergencyContactDisplayPartial", patientDetail);
        }

        /// <summary>
        /// GET: /SickReport/EditEmergencyContactInfoForm/{patientId}
        /// Fetches and returns the partial view for editing patient emergency contact information.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view containing the edit form.</returns>
        [HttpGet]
        public async Task<IActionResult> EditEmergencyContactInfoForm(int patientId)
        {
            _logger.LogInformation("Fetching edit emergency contact info form for PatientId: {PatientId}", patientId);
            var emergencyContactInfo = await _patientService.GetPatientEmergencyContactInfoForEditAsync(patientId);
            if (emergencyContactInfo == null)
            {
                _logger.LogWarning("EditEmergencyContactInfoForm: Emergency contact info not found for PatientId: {PatientId}", patientId);
                return Content("<div class=\"text-error\">Error: Emergency contact information not found.</div>");
            }
            return PartialView("_EditPatientEmergencyContactInfoFormPartial", emergencyContactInfo);
        }

        /// <summary>
        /// POST: /SickReport/UpdateEmergencyContactInfo
        /// Handles the submission of the patient emergency contact information edit form.
        /// </summary>
        /// <param name="dto">The DTO containing the updated emergency contact information.</param>
        /// <returns>The updated display partial or the form with validation errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmergencyContactInfo([FromForm] EditPatientEmergencyContactInfoDto dto)
        {
            _logger.LogInformation("Attempting to update emergency contact info for PatientId: {PatientId}", dto.PatientId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for emergency contact info update for PatientId: {PatientId}", dto.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct the errors.\"}");
                return PartialView("_EditPatientEmergencyContactInfoFormPartial", dto);
            }

            bool success = await _patientService.UpdatePatientEmergencyContactInfoAsync(dto);

            if (success)
            {
                _logger.LogInformation("Successfully updated emergency contact info for PatientId: {PatientId}", dto.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Emergency contact information updated successfully!\"}");
                var updatedPatientDetail = await _patientService.GetPatientDetailByIdAsync(dto.PatientId);
                return PartialView("_PatientEmergencyContactDisplayPartial", updatedPatientDetail);
            }
            else
            {
                _logger.LogError("Failed to update emergency contact info for PatientId: {PatientId}", dto.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to update emergency contact information. Please try again.\"}");
                ModelState.AddModelError("", "An unexpected error occurred during update. Please try again.");
                return PartialView("_EditPatientEmergencyContactInfoFormPartial", dto);
            }
        }
    }
}
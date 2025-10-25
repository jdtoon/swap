using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.dtos.patient;
using carestream.core.dtos.shared;
using System.Security.Claims; // Added for FilterAndPaginationOptions

namespace carestream.web.controllers
{
    /// <summary>
    /// Controller for managing patient-specific data and actions.
    /// </summary>
    [Authorize(Roles = "PatientAdmin,SystemAdmin")] // Expanded roles for general patient management
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly IPatientAdminService _patientAdminService; // NEW: Inject IPatientAdminService
        private readonly ILogger<PatientController> _logger;

        public PatientController(IPatientService patientService, IPatientAdminService patientAdminService, ILogger<PatientController> logger)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _patientAdminService = patientAdminService ?? throw new ArgumentNullException(nameof(patientAdminService)); // NEW: Assign service
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Patient/AllPatients
        /// Displays a paginated list of all patients for administrative viewing (FR-PA-004).
        /// This action serves as the main entry point for the patient list.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A partial view displaying the patient list.</returns>
        [HttpGet]
        public async Task<IActionResult> AllPatients([FromQuery] FilterAndPaginationOptions options)
        {
            _logger.LogInformation("Controller: AllPatients requested with options: {@Options}", options);
            if (options.PageNumber < 1) options.PageNumber = 1;
            if (options.PageSize < 1) options.PageSize = 25; // Default page size for patient list

            var viewModel = await _patientAdminService.GetAllPatientsForAdminAsync(options);
            viewModel.Pagination.HxGetUrl = Url.Action("AllPatients", "Patient") ?? "";
            viewModel.Pagination.HxTarget = "#patient-list-container"; // Target for pagination refresh
            viewModel.Pagination.HxSwap = "innerHTML";

            return PartialView("_PatientListPartial", viewModel);
        }

        /// <summary>
        /// GET: /Patient/RegisterNewPatientForm
        /// Displays the form for registering a new patient (FR-PA-013).
        /// </summary>
        /// <returns>A partial view with the new patient registration form.</returns>
        [HttpGet]
        public IActionResult RegisterNewPatientForm()
        {
            _logger.LogInformation("Controller: RegisterNewPatientForm requested.");
            return PartialView("_RegisterPatientFormPartial", new CreatePatientInputDto());
        }

        /// <summary>
        /// POST: /Patient/RegisterNewPatient
        /// Handles the submission of the new patient registration form (FR-PA-013).
        /// </summary>
        /// <param name="inputDto">The DTO containing the new patient's details.</param>
        /// <returns>A partial view confirming registration or re-rendering the form with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterNewPatient([FromForm] CreatePatientInputDto inputDto)
        {
            _logger.LogInformation("Controller: Attempting to register new patient with ForceNumber: {ForceNumber}", inputDto.ForceNumber);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: RegisterNewPatient called with invalid ModelState for ForceNumber: {ForceNumber}. Errors: {Errors}",
                    inputDto.ForceNumber, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                Response.Headers.Append("HX-Retarget", "#register-patient-form-container"); // Target the form's container
                Response.Headers.Append("HX-Reswap", "innerHTML"); // Re-render the form inside it
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                return PartialView("_RegisterPatientFormPartial", inputDto);
            }

            // Get performing user's internal ID
            var userIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int performingUserId))
            {
                _logger.LogError("Controller: RegisterNewPatient failed: Could not find 'carestream_user_id' claim for performing user.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"User session invalid. Unable to register patient.\"}");
                Response.Headers.Append("HX-Retarget", "#register-patient-form-container"); // Re-render to show general error if needed
                Response.Headers.Append("HX-Reswap", "innerHTML");
                return PartialView("_RegisterPatientFormPartial", inputDto); // Return form with general error message
            }

            var result = await _patientAdminService.RegisterNewPatientAsync(inputDto, performingUserId);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Controller: Patient registered successfully. PatientId: {PatientId}, ForceNumber: {ForceNumber}", result.PatientId, inputDto.ForceNumber);
                Response.Headers.Append("HX-Redirect", Url.Action("AllPatients", "Patient")); 
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Patient registered successfully! You can now check them in.\" }");
                return Ok();
            }
            else
            {
                _logger.LogWarning("Controller: Patient registration failed for ForceNumber: {ForceNumber}. Message: {Message}", inputDto.ForceNumber, result.Message);
                ModelState.AddModelError(string.Empty, result.Message); // Add general error message to model state
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"" + (result.IsDuplicateForceNumber ? "Patient with this Force Number already exists." : "Failed to register patient. Please try again.") + "\"}");
                Response.Headers.Append("HX-Retarget", "#register-patient-form-container"); // Target the form's container
                Response.Headers.Append("HX-Reswap", "innerHTML"); // Re-render the form inside it
                return PartialView("_RegisterPatientFormPartial", inputDto); // Return form with specific error message
            }
        }

        /// <summary>
        /// GET: /Patient/EditPersonalInfoForm/{patientId}
        /// Returns a partial view with a form to edit a patient's personal information.
        /// This is intended to be loaded via HTMX into a target div.
        /// </summary>
        /// <param name="patientId">The ID of the patient to edit.</param>
        [HttpGet("Patient/EditPersonalInfoForm/{patientId:int}")] // Attribute routing
        public async Task<IActionResult> EditPersonalInfoForm(int patientId)
        {
            _logger.LogInformation("Fetching personal info edit form for PatientId: {PatientId}", patientId);
            if (patientId <= 0)
            {
                return PartialView("_ErrorPartial", "Invalid Patient ID provided.");
            }

            var patientInfo = await _patientService.GetPatientPersonalInfoForEditAsync(patientId);
            if (patientInfo == null)
            {
                _logger.LogWarning("Patient not found for editing personal info. PatientId: {PatientId}", patientId);
                return PartialView("~/Views/Shared/_ErrorPartial", $"Patient with ID {patientId} not found."); // Use shared error partial
            }

            return PartialView("_EditPersonalInfoFormPartial", patientInfo);
        }

        /// <summary>
        /// POST: /Patient/UpdatePersonalInfo
        /// Handles the submission of the edited personal information form.
        /// </summary>
        /// <param name="inputDto">The DTO containing the edited patient information.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePersonalInfo([FromForm] EditPatientPersonalInfoDto inputDto)
        {
            _logger.LogInformation("Attempting to update personal info for PatientId: {PatientId}", inputDto.PatientId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid ModelState for UpdatePersonalInfo, PatientId: {PatientId}. Errors: {Errors}",
                    inputDto.PatientId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                // Re-render the form with validation errors
                // The hx-target on the form should handle placing this back.
                return PartialView("_EditPersonalInfoFormPartial", inputDto);
            }

            bool success = await _patientService.UpdatePatientPersonalInfoAsync(inputDto);

            if (success)
            {
                _logger.LogInformation("Successfully updated personal info for PatientId: {PatientId}", inputDto.PatientId);
                var updatedPatientDetail = await _patientService.GetPatientByForceNumberAsync(inputDto.ForceNumber);
                if (updatedPatientDetail == null)
                {
                    return PartialView("~/Views/Shared/_ErrorPartial", "Failed to retrieve updated patient details after successful update.");
                }
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Personal information updated!\", \"refreshPatientDetailsDisplay\": " + inputDto.PatientId + " }");
                return PartialView("_PatientPersonalDisplayPartial", updatedPatientDetail);
            }
            else
            {
                _logger.LogError("Failed to update personal info for PatientId: {PatientId}", inputDto.PatientId);
                ModelState.AddModelError(string.Empty, "An error occurred while updating patient information. Please try again.");
                return PartialView("_EditPersonalInfoFormPartial", inputDto); // Re-render form with a general error
            }
        }

        [HttpGet("Patient/PersonalDisplayPartial/{patientId:int}")]
        public async Task<IActionResult> PersonalDisplayPartial(int patientId)
        {
            var patientDetail = await _patientService.GetPatientByForceNumberAsync((await _patientService.GetPatientPersonalInfoForEditAsync(patientId))?.ForceNumber ?? ""); // Re-fetch to ensure fresh data
            if (patientDetail == null) return NotFound(); // Or an error partial
            return PartialView("_PatientPersonalDisplayPartial", patientDetail);
        }
    }
}
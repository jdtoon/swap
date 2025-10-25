using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services; // For IVitalsService
using carestream.core.dtos.vitals;       // For VitalsCaptureInputDto
using System.Security.Claims;
using carestream.core.interfaces.repositories; // For User.FindFirstValue

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller responsible for handling actions related to patient vital signs.
    /// Accessible by users with the "Nurse" role.
    /// </summary>
    [Authorize(Roles = "Nurse")] // Secure this controller for Nurses
    public class VitalsController : Controller
    {
        private readonly IVitalsService _vitalsService;
        private readonly ILogger<VitalsController> _logger;
        private readonly IUserRepository _userRepository; // To get internal user ID

        /// <summary>
        /// Initializes a new instance of the <see cref="VitalsController"/> class.
        /// </summary>
        /// <param name="vitalsService">The vitals service.</param>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="logger">The logger for this controller.</param>
        public VitalsController(
            IVitalsService vitalsService,
            carestream.core.interfaces.repositories.IUserRepository userRepository, // Explicit namespace
            ILogger<VitalsController> logger)
        {
            _vitalsService = vitalsService ?? throw new ArgumentNullException(nameof(vitalsService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Vitals/StartVitalsCapture
        /// Displays the form for capturing vital signs for a given visit and patient.
        /// This action is typically triggered from the Nurse's Vitals Queue.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <returns>A partial view with the vitals capture form.</returns>
        [HttpGet]
        public async Task<IActionResult> StartVitalsCapture(int visitId, int patientId)
        {
            _logger.LogInformation("Attempting to start vitals capture for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("StartVitalsCapture called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                // TODO: Return a more user-friendly error partial view
                TempData["ErrorMessage"] = "Invalid visit or patient identifier provided for vitals capture.";
                return PartialView("_VitalsCaptureError"); // A simple error partial
            }

            // Call service to get the DTO pre-populated with patient context and any existing vitals
            var model = await _vitalsService.GetVitalsCaptureModelAsync(visitId, patientId);

            if (model == null)
            {
                _logger.LogWarning("Could not retrieve vitals capture model for VisitId: {VisitId}, PatientId: {PatientId}. Patient or visit might not exist or is not accessible.", visitId, patientId);
                TempData["ErrorMessage"] = "Could not load patient details for vitals capture. Please try again or select another patient.";
                return PartialView("_VitalsCaptureError");
            }

            _logger.LogInformation("Successfully prepared VitalsCaptureModel for VisitId: {VisitId}. Displaying form.", visitId);
            return PartialView("_VitalsCaptureForm", model);
        }

        /// <summary>
        /// POST: /Vitals/SaveVitals
        /// Handles the submission of the captured vital signs form.
        /// </summary>
        /// <param name="vitalsInput">The DTO containing the captured vitals from the form.</param>
        /// <returns>
        /// A partial view indicating success or failure, or redirects/returns another partial
        /// to update the UI (e.g., back to the Vitals Queue or a confirmation message).
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Recommended for POST actions from forms
        public async Task<IActionResult> SaveVitals(VitalsCaptureInputDto vitalsInput)
        {
            _logger.LogInformation("Attempting to save vitals for VisitId: {VisitId}", vitalsInput?.VisitId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("SaveVitals called with invalid model state for VisitId: {VisitId}.", vitalsInput?.VisitId);
                // Re-display the form with validation errors
                // Need to re-populate PatientName/Rank if they are not part of the POSTed model
                // but are needed for display.
                if (vitalsInput != null)
                {
                    var repopulatedModel = await _vitalsService.GetVitalsCaptureModelAsync(vitalsInput.VisitId, vitalsInput.PatientId);
                    if (repopulatedModel != null)
                    { // repopulate context fields
                        vitalsInput.PatientName = repopulatedModel.PatientName;
                        vitalsInput.PatientRank = repopulatedModel.PatientRank;
                    }
                }
                return PartialView("_VitalsCaptureForm", vitalsInput);
            }

            // Get performing user's internal ID
            var logtoSub = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(logtoSub))
            {
                _logger.LogError("SaveVitals failed: Could not find 'sub' claim for performing user for VisitId: {VisitId}", vitalsInput!.VisitId);
                ModelState.AddModelError("", "User session invalid. Unable to save vitals.");
                return PartialView("_VitalsCaptureForm", vitalsInput); // Or a generic error partial
            }
            var performingUserId = await _userRepository.GetUserIdByLogtoSubAsync(logtoSub);
            if (!performingUserId.HasValue)
            {
                _logger.LogError("SaveVitals failed: Logto user '{LogtoSub}' not linked. VisitId: {VisitId}", logtoSub, vitalsInput!.VisitId);
                ModelState.AddModelError("", "User account not fully configured. Unable to save vitals.");
                return PartialView("_VitalsCaptureForm", vitalsInput); // Or a generic error partial
            }


            bool success = await _vitalsService.SaveVitalsAsync(vitalsInput!, performingUserId.Value);

            if (success)
            {
                _logger.LogInformation("Vitals successfully saved for VisitId: {VisitId}. Returning confirmation.", vitalsInput!.VisitId);
                // TODO: Prepare a proper confirmation DTO/ViewModel if needed
                TempData["SuccessMessage"] = $"Vitals for {vitalsInput.PatientName ?? "patient"} (Visit ID: {vitalsInput.VisitId}) saved successfully. Patient is now ready for doctor.";
                // This HTMX response will replace the #main-content with the Nurse Dashboard
                // We want the Nurse Dashboard to refresh and show the updated queue
                Response.Headers["HX-Push-Url"] = Url.Action("NurseDashboard", "Dashboard"); // Update browser URL
                return PartialView("~/Views/Dashboard/NurseDashboard.cshtml", await HttpContext.RequestServices.GetRequiredService<INurseDashboardService>().GetDashboardViewModelAsync());
            }
            else
            {
                _logger.LogError("Failed to save vitals in service layer for VisitId: {VisitId}.", vitalsInput!.VisitId);
                ModelState.AddModelError("", "An error occurred while saving vitals. Please try again.");
                // Re-display form with error
                if (vitalsInput != null) // Repopulate context as above
                {
                    var repopulatedModel = await _vitalsService.GetVitalsCaptureModelAsync(vitalsInput.VisitId, vitalsInput.PatientId);
                    if (repopulatedModel != null)
                    {
                        vitalsInput.PatientName = repopulatedModel.PatientName;
                        vitalsInput.PatientRank = repopulatedModel.PatientRank;
                    }
                }
                return PartialView("_VitalsCaptureForm", vitalsInput);
            }
        }
    }
}
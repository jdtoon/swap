using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories; // Needed for IUserRepository
using carestream.core.dtos.consultation; // For VisitAssessmentDto, CreateUpdateVisitAssessmentDto
using System.Security.Claims;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing patient visit assessments.
    /// Accessible by Doctor and Nurse roles.
    /// </summary>
    [Authorize(Roles = "Doctor,Nurse")]
    public class VisitAssessmentController : Controller
    {
        private readonly IVisitAssessmentService _visitAssessmentService;
        private readonly IUserRepository _userRepository; // To get internal user ID for recording actions
        private readonly ILogger<VisitAssessmentController> _logger;

        public VisitAssessmentController(
            IVisitAssessmentService visitAssessmentService,
            IUserRepository userRepository,
            ILogger<VisitAssessmentController> logger)
        {
            _visitAssessmentService = visitAssessmentService ?? throw new ArgumentNullException(nameof(visitAssessmentService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /VisitAssessment/Index/{visitId}/{patientId}
        /// Displays the main container for a patient's visit assessment form.
        /// This is designed to be loaded into a tab within the consultation.
        /// </summary>
        /// <param name="visitId">The ID of the visit for which to display/manage the assessment.</param>
        /// <param name="patientId">The ID of the patient (for context in the view).</param>
        /// <returns>A partial view for the visit assessment form.</returns>
        [HttpGet]
        public async Task<IActionResult> Index(int visitId, int patientId)
        {
            _logger.LogInformation("Controller: VisitAssessment/Index requested for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Controller: VisitAssessment/Index called with invalid VisitId ({VisitId}) or PatientId ({PatientId}).", visitId, patientId);
                return PartialView("~/Views/Shared/_ErrorPartial", "Invalid Visit or Patient ID for assessment.");
            }

            var assessment = await _visitAssessmentService.GetVisitAssessmentAsync(visitId);
            if (assessment == null)
            {
                _logger.LogInformation("No existing assessment found for VisitId: {VisitId}. Initializing new DTO.", visitId);
                assessment = new VisitAssessmentDto(); // Initialize default DTO
            }

            // Prepare CreateUpdateVisitAssessmentDto for the form
            var formModel = new CreateUpdateVisitAssessmentDto
            {
                VisitAssessmentId = assessment.VisitAssessmentId,
                VisitId = visitId,
                PatientId = patientId, // Essential for validation and service calls
                PhysicalExamFindings = assessment.PhysicalExamFindings,
                CardiovascularNotes = assessment.CardiovascularNotes,
                RespiratoryNotes = assessment.RespiratoryNotes,
                MusculoskeletalNotes = assessment.MusculoskeletalNotes,
                NeurologicalNotes = assessment.NeurologicalNotes,
                PsychologicalNotes = assessment.PsychologicalNotes,
                OtherSystemsNotes = assessment.OtherSystemsNotes,
                MedicalClassification = assessment.MedicalClassification,
                DeploymentStatus = assessment.DeploymentStatus,
                ValidityPeriodMonths = assessment.ValidityPeriodMonths,
                Restrictions = assessment.Restrictions
            };

            ViewData["PatientId"] = patientId; // Pass PatientId for context in the view (e.g., banner)
            ViewData["VisitId"] = visitId; // Pass VisitId for context in the view
            return PartialView("_AssessmentFormPartial", formModel); // Renders Views/VisitAssessment/_AssessmentFormPartial.cshtml
        }


        /// <summary>
        /// POST: /VisitAssessment/SaveAssessment
        /// Handles the creation or update of a visit assessment.
        /// </summary>
        /// <param name="dto">The DTO containing the assessment data.</param>
        /// <returns>A partial view indicating success or failure, or re-renders the form with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAssessment([FromForm] CreateUpdateVisitAssessmentDto dto)
        {
            _logger.LogInformation("Controller: Saving visit assessment for VisitId: {VisitId}, AssessmentId: {AssessmentId}", dto.VisitId, dto.VisitAssessmentId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Controller: Validation failed for visit assessment for VisitId: {VisitId}, AssessmentId: {AssessmentId}.", dto.VisitId, dto.VisitAssessmentId);
                Response.Headers.Append("HX-Retarget", "#visit-assessment-form-container"); // Target the form's container
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
                ViewData["PatientId"] = dto.PatientId; // Re-pass context IDs
                ViewData["VisitId"] = dto.VisitId;
                return PartialView("_AssessmentFormPartial", dto);
            }

            var adminUserIdString = User.FindFirstValue("carestream_user_id");
            if (string.IsNullOrEmpty(adminUserIdString) || !int.TryParse(adminUserIdString, out int adminUserId))
            {
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Admin user identity error. Please re-login.\"}");
                return Content("", "text/html");
            }

            dto.AssessedByUserId = adminUserId;
            bool success = await _visitAssessmentService.SaveVisitAssessmentAsync(dto);

            if (success)
            {
                _logger.LogInformation("Controller: Visit assessment {AssessmentId} {Action} successfully for VisitId {VisitId}.", dto.VisitAssessmentId, dto.VisitAssessmentId > 0 ? "updated" : "created", dto.VisitId);
                Response.Headers.Append("HX-Trigger", "{\"showToastSuccess\": \"Assessment saved successfully!\"}");
                // On success, we might want to reload the same form to show saved data/disable fields, or redirect.
                // For now, returning the form itself will re-render it, and show success message.
                // Or you could return an empty Content() and let HTMX re-GET the tab content.
                return PartialView("_AssessmentFormPartial", dto); // Re-render the form to show saved state
            }
            else
            {
                _logger.LogError("Controller: Failed to save visit assessment for VisitId: {VisitId}, AssessmentId: {AssessmentId}.", dto.VisitId, dto.VisitAssessmentId);
                Response.Headers.Append("HX-Retarget", "#visit-assessment-form-container");
                Response.Headers.Append("HX-Reswap", "innerHTML");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to save assessment. Please try again.\"}");
                ModelState.AddModelError("", "Failed to save assessment due to a system error.");
                ViewData["PatientId"] = dto.PatientId;
                ViewData["VisitId"] = dto.VisitId;
                return PartialView("_AssessmentFormPartial", dto);
            }
        }

        // Helper to get the internal user ID from claims (copied from other controllers)
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
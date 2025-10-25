using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services; // For IDD50ReportService
using carestream.core.dtos.patient; // For PatientDetailDto (if needed for lookup)

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for handling various reporting functionalities, including DD50.
    /// </summary>
    [Authorize(Roles = "PatientAdmin,Doctor")] // As per FRS, DD50 is for PatientAdmin/Doctor
    public class ReportsController : Controller
    {
        private readonly IDD50ReportService _dd50ReportService;
        private readonly IPatientService _patientService; // To lookup patient and find visit
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IDD50ReportService dd50ReportService,
            IPatientService patientService,
            ILogger<ReportsController> logger)
        {
            _dd50ReportService = dd50ReportService ?? throw new ArgumentNullException(nameof(dd50ReportService));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Reports/GenerateDD50
        /// Displays the initial page for generating the DD50 Medical Examination Report (FR-DD50-001, FR-DD50-002).
        /// Includes a form for patient lookup by Force Number.
        /// </summary>
        /// <returns>A partial view for the DD50 report generation page.</returns>
        [HttpGet]
        public IActionResult GenerateDD50()
        {
            _logger.LogInformation("Controller: Reports/GenerateDD50 requested.");
            return PartialView(); // Renders Views/Reports/GenerateDD50.cshtml
        }

        /// <summary>
        /// POST: /Reports/DisplayDD50Report
        /// Handles the lookup and displays the generated DD50 report for a patient (FR-DD50-003, FR-DD50-004).
        /// Finds the latest active/relevant visit for the patient to generate the report.
        /// </summary>
        /// <param name="forceNumber">The Force Number of the patient.</param>
        /// <returns>A partial view displaying the DD50 report, or an error message.</returns>
        [HttpPost]
        public async Task<IActionResult> DisplayDD50Report(string forceNumber)
        {
            _logger.LogInformation("Controller: DisplayDD50Report requested for ForceNumber: {ForceNumber}", forceNumber);

            if (string.IsNullOrWhiteSpace(forceNumber))
            {
                _logger.LogWarning("Controller: DisplayDD50Report called with empty ForceNumber.");
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"DisplayDD50Report called with empty ForceNumber.\"}");
                return Ok();
            }

            PatientDetailDto? patient = await _patientService.GetPatientByForceNumberAsync(forceNumber);
            if (patient == null)
            {
                _logger.LogWarning("Controller: Patient with ForceNumber {ForceNumber} not found for DD50 report.", forceNumber);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Patient with Force Number " + forceNumber + " not found.\"}");
                return Ok();
            }

            // Find the most recent 'completed' or 'relevant' visit to generate the report from
            // Assuming DD50 is generated from a final consultation or a specific visit.
            // For now, let's try to get any visit, ideally the last completed one that would have full data.
            // If the FRS implies always using the *latest* visit regardless of status, adjust logic.
            var latestVisit = await _patientService.GetActiveVisitForPatientAsync(patient.PatientId); // Or a specific method for 'DD50-ready' visits

            if (latestVisit == null || latestVisit.VisitId <= 0)
            {
                _logger.LogWarning("Controller: No relevant visit found for PatientId {PatientId} to generate DD50 report.", patient.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"No relevant medical visit found for patient " + patient.FullName + " to generate DD50 report.\"}");
                return Ok();
            }

            var dd50Report = await _dd50ReportService.GenerateDD50ReportAsync(latestVisit.VisitId);

            if (dd50Report == null)
            {
                _logger.LogError("Controller: Failed to generate DD50 report for VisitId: {VisitId}, PatientId: {PatientId}.", latestVisit.VisitId, patient.PatientId);
                Response.Headers.Append("HX-Trigger", "{\"showToastError\": \"Failed to generate DD50 report. Please try again or contact support.\"}");
                return Ok();
            }

            _logger.LogInformation("Controller: Successfully generated DD50 report for VisitId: {VisitId}, PatientId: {PatientId}.", latestVisit.VisitId, patient.PatientId);
            return PartialView("_DD50ReportDisplayPartial", dd50Report);
        }
    }
}
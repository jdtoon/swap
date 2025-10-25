using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for handling various role-specific dashboards.
    /// </summary>
    [Authorize] // Authorize at controller level
    public class DashboardController : Controller
    {
        private readonly IPatientAdminDashboardService _patientAdminDashboardService;
        private readonly IDashboardRepository _patientAdminRepoDirect; // Renamed for clarity if only used by PA Dash
        private readonly INurseDashboardService _nurseDashboardService;
        private readonly IDoctorDashboardService _doctorDashboardService; // *** NEW: Inject Doctor Service ***
        private readonly ILogger<DashboardController> _logger;
        private readonly IPharmacyService _pharmacyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardController"/> class.
        /// </summary>
        public DashboardController(
            IPatientAdminDashboardService patientAdminDashboardService,
            IDashboardRepository patientAdminRepoDirect, // For Patient Admin specific direct calls
            INurseDashboardService nurseDashboardService,
            IDoctorDashboardService doctorDashboardService, // *** NEW: Add to constructor ***
            ILogger<DashboardController> logger,
            IPharmacyService pharmacyService)
        {
            _patientAdminDashboardService = patientAdminDashboardService ?? throw new ArgumentNullException(nameof(patientAdminDashboardService));
            _patientAdminRepoDirect = patientAdminRepoDirect ?? throw new ArgumentNullException(nameof(patientAdminRepoDirect));
            _nurseDashboardService = nurseDashboardService ?? throw new ArgumentNullException(nameof(nurseDashboardService));
            _doctorDashboardService = doctorDashboardService ?? throw new ArgumentNullException(nameof(doctorDashboardService)); // *** NEW: Assign ***
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pharmacyService = pharmacyService ?? throw new ArgumentNullException(nameof(pharmacyService)); ;
        }

        // --- Patient Admin Dashboard Actions ---
        /// <summary>
        /// Displays the main container view for the Patient Admin dashboard,
        /// which then loads its content via HTMX partials.
        /// </summary>
        [Authorize(Roles = "PatientAdmin")]
        [HttpGet]
        public IActionResult PatientAdminDashboard()
        {
            _logger.LogInformation("PatientAdminDashboard action called.");
            return PartialView(); // Renders PatientAdminDashboard.cshtml (the container)
        }

        /// <summary>
        /// Returns a partial view for the statistics section of the Patient Admin dashboard.
        /// </summary>
        [Authorize(Roles = "PatientAdmin")]
        [HttpGet]
        public async Task<IActionResult> StatsPartial()
        {
            _logger.LogInformation("StatsPartial (PatientAdmin) action called.");
            // Using direct repo call as per previous setup for this specific partial
            var stats = await _patientAdminRepoDirect.GetDashboardStatsAsync();
            return PartialView("_StatsPartial", stats);
        }

        /// <summary>
        /// Returns a partial view for the recent patients section of the Patient Admin dashboard.
        /// </summary>
        [Authorize(Roles = "PatientAdmin")]
        [HttpGet]
        public async Task<IActionResult> RecentPatientsPartial()
        {
            _logger.LogInformation("RecentPatientsPartial (PatientAdmin) action called.");
            var patients = await _patientAdminRepoDirect.GetRecentPatientsAsync();
            return PartialView("_RecentPatientsPartial", patients);
        }

        /// <summary>
        /// Returns a partial view for the recent staff reports section of the Patient Admin dashboard.
        /// </summary>
        [Authorize(Roles = "PatientAdmin")]
        [HttpGet]
        public async Task<IActionResult> RecentReportsPartial()
        {
            _logger.LogInformation("RecentReportsPartial (PatientAdmin) action called.");
            var reports = await _patientAdminRepoDirect.GetRecentStaffReportsAsync();
            return PartialView("_RecentReportsPartial", reports);
        }


        // --- Nurse Dashboard Action ---
        /// <summary>
        /// Displays the Nurse dashboard.
        /// </summary>
        [Authorize(Roles = "Nurse")]
        [HttpGet]
        public async Task<IActionResult> NurseDashboard()
        {
            _logger.LogInformation("NurseDashboard action called.");
            var viewModel = await _nurseDashboardService.GetDashboardViewModelAsync();
            return PartialView(viewModel); // Renders NurseDashboard.cshtml
        }


        // --- Doctor Dashboard Action ---
        /// <summary>
        /// Displays the Doctor dashboard.
        /// </summary>
        [Authorize(Roles = "Doctor")]
        [HttpGet]
        public async Task<IActionResult> DoctorDashboard() // Make async Task<IActionResult>
        {
            _logger.LogInformation("[CONTROLLER] DoctorDashboard action called.");
            var viewModel = await _doctorDashboardService.GetDashboardViewModelAsync(); // Call the service
            return PartialView(viewModel); // Pass ViewModel to DoctorDashboard.cshtml
        }


        /// <summary>
        /// Displays the Pharmacist dashboard with pending prescriptions and statistics.
        /// Supports pagination for the pending prescriptions list.
        /// </summary>
        /// <param name="pageNumber">The current page number for pending prescriptions (1-based).</param>
        /// <param name="pageSize">The number of items per page for pending prescriptions.</param>
        [Authorize(Roles = "Pharmacist")]
        [HttpGet]
        public async Task<IActionResult> PharmacistDashboard(int pageNumber = 1, int pageSize = 15) // Default page size
        {
            _logger.LogInformation("[CONTROLLER] PharmacistDashboard action called. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 15; // Ensure valid page size

            var viewModel = await _pharmacyService.GetDashboardViewModelAsync(pageNumber, pageSize);

            // TODO: Later, when implementing full pagination UI component,
            // the viewModel should contain TotalItems, PageCount etc.
            // For now, we just pass the list.
            // ViewData["PageNumber"] = pageNumber;
            // ViewData["PageSize"] = pageSize;
            // ViewData["TotalItems"] = viewModel.Stats.PendingPrescriptionsCount; // Assuming Stats has this

            return PartialView(viewModel); // Renders Views/Dashboard/PharmacistDashboard.cshtml
        }
    }
}
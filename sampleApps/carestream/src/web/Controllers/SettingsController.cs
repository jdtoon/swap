using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services; // For ISystemHealthService

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for managing application settings and displaying system health.
    /// Accessible by all authorized users (Settings link).
    /// </summary>
    [Authorize] // As per FRS, accessible by all roles (general settings and system health)
    public class SettingsController : Controller
    {
        private readonly ISystemHealthService _systemHealthService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ISystemHealthService systemHealthService,
            ILogger<SettingsController> logger)
        {
            _systemHealthService = systemHealthService ?? throw new ArgumentNullException(nameof(systemHealthService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Settings/Index
        /// Displays the main container for the settings page, which includes system health information (FR-SET-001).
        /// </summary>
        /// <returns>A partial view for the settings page.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Controller: Settings/Index requested.");
            // The system health partial will be loaded by HTMX
            return PartialView(); // Renders Views/Settings/Index.cshtml
        }

        /// <summary>
        /// GET: /Settings/SystemHealthPartial
        /// Fetches and returns the partial view containing the system health status indicators (FR-SET-002).
        /// </summary>
        /// <returns>A partial view displaying system health.</returns>
        [HttpGet]
        public async Task<IActionResult> SystemHealthPartial()
        {
            _logger.LogInformation("Controller: Fetching SystemHealthPartial.");
            var viewModel = await _systemHealthService.GetSystemHealthDashboardAsync();
            return PartialView("_SystemHealthPartial", viewModel);
        }
    }
}
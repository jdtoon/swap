using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using carestream.core.interfaces.services; // For IEmergencyContactService
using Microsoft.Extensions.Logging; // For ILogger
using System.Threading.Tasks; // For Task

namespace carestream.web.Controllers
{
    /// <summary>
    /// Controller for handling the display of emergency contact information.
    /// </summary>
    [Authorize(Roles = "PatientAdmin")] // As per FRS, accessible by PatientAdmin
    public class EmergencyContactController : Controller
    {
        private readonly IEmergencyContactService _emergencyContactService;
        private readonly ILogger<EmergencyContactController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmergencyContactController"/> class.
        /// </summary>
        /// <param name="emergencyContactService">The service for retrieving emergency contact data.</param>
        /// <param name="logger">The logger for this controller.</param>
        public EmergencyContactController(
            IEmergencyContactService emergencyContactService,
            ILogger<EmergencyContactController> logger)
        {
            _emergencyContactService = emergencyContactService ?? throw new ArgumentNullException(nameof(emergencyContactService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /EmergencyContact/Index
        /// Displays the list of emergency contacts and important notices.
        /// This action serves as the main entry point for the Emergency Contact feature.
        /// </summary>
        /// <returns>A partial view displaying the emergency contact information.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Request for Emergency Contact display initiated.");
            var viewModel = await _emergencyContactService.GetEmergencyContactsAsync();
            return PartialView("~/Views/EmergencyContact/Index.cshtml", viewModel);
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.emergency; // For EmergencyContactViewModel, EmergencyContactDto
using carestream.core.interfaces.services; // For IEmergencyContactService
using Microsoft.Extensions.Logging; // For ILogger

namespace carestream.core.services
{
    /// <summary>
    /// Provides service logic for retrieving emergency contact information.
    /// Initially returns hardcoded data.
    /// </summary>
    public class EmergencyContactService : IEmergencyContactService
    {
        private readonly ILogger<EmergencyContactService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmergencyContactService"/> class.
        /// </summary>
        /// <param name="logger">The logger for this service.</param>
        public EmergencyContactService(ILogger<EmergencyContactService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<EmergencyContactViewModel> GetEmergencyContactsAsync()
        {
            _logger.LogInformation("Retrieving emergency contact information.");

            // --- Hardcoded Data (as per plan for initial implementation) ---
            var contacts = new List<EmergencyContactDto>
            {
                new EmergencyContactDto { Name = "1 Military Hospital - Pretoria", PhoneNumber = "0123140412", Description = "Pretoria", Type = "Hospital" },
                new EmergencyContactDto { Name = "2 Military Hospital - Cape Town", PhoneNumber = "0217996000", Description = "Cape Town", Type = "Hospital" },
                new EmergencyContactDto { Name = "3 Military Hospital - Bloemfontein", PhoneNumber = "0514021000", Description = "Bloemfontein", Type = "Hospital" },
                new EmergencyContactDto { Name = "Military Police Division", PhoneNumber = "0123555000", Type = "Police" },
                new EmergencyContactDto { Name = "SAPS Emergency", PhoneNumber = "10111", Type = "Police" },
                new EmergencyContactDto { Name = "SAMHS Emergency Response", PhoneNumber = "0123140412", Type = "Military" }
            };

            var notices = new List<string>
            {
                "For life-threatening emergencies, dial the nearest Military Hospital immediately.",
                "Keep these numbers saved in your phone for quick access.",
                "When calling, clearly state your name, location, and the nature of the emergency.",
                "Stay on the line until the operator tells you to hang up."
            };
            // ---------------------------------------------------------------

            var viewModel = new EmergencyContactViewModel
            {
                Contacts = contacts,
                ImportantNotices = notices
            };

            _logger.LogDebug("Successfully retrieved {Count} emergency contacts.", contacts.Count);
            return await Task.FromResult(viewModel); // Return as completed task
        }
    }
}
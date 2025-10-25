using System.Collections.Generic;

namespace carestream.core.dtos.emergency
{
    /// <summary>
    /// Represents the view model for the Emergency Contact display.
    /// </summary>
    public class EmergencyContactViewModel
    {
        /// <summary>
        /// Gets or sets the list of emergency contacts to display.
        /// </summary>
        public List<EmergencyContactDto> Contacts { get; set; } = new List<EmergencyContactDto>();

        /// <summary>
        /// Gets or sets important notices or instructions regarding emergency procedures.
        /// </summary>
        public List<string> ImportantNotices { get; set; } = new List<string>();
    }
}
namespace carestream.core.dtos.emergency
{
    /// <summary>
    /// Represents a single emergency contact entry.
    /// </summary>
    public class EmergencyContactDto
    {
        /// <summary>
        /// Gets or sets the name of the emergency contact (e.g., "1 Military Hospital - Pretoria").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the phone number of the emergency contact.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional description or location for the contact.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the type or category of the contact (e.g., "Hospital", "Police").
        /// </summary>
        public string? Type { get; set; }
    }
}
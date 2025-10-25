namespace carestream.core.dtos.patient
{
    /// <summary>
    /// Represents basic patient identification information.
    /// </summary>
    public class PatientBasicInfoDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the patient.
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the patient's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient's rank.
        /// </summary>
        public string? Rank { get; set; }
        /// <summary>
        /// Gets or sets the patient's force number.
        /// </summary>
        public string? ForceNumber { get; set; }
        /// <summary>
        /// Gets or sets the patient's date of birth.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }
        /// <summary>
        /// Gets or sets the patient's gender.
        /// </summary>
        public string? Gender { get; set; }

        // Helper for constructing full name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
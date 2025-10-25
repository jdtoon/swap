namespace carestream.core.dtos.patient
{
    /// <summary>
    /// Represents the result of a patient registration operation.
    /// </summary>
    public class PatientRegistrationResultDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the patient registration was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the newly registered patient, if successful.
        /// </summary>
        public int? PatientId { get; set; }

        /// <summary>
        /// Gets or sets a message describing the result of the registration (e.g., success message, error details).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the force number provided was a duplicate.
        /// </summary>
        public bool IsDuplicateForceNumber { get; set; }
    }
}
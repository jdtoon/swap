namespace carestream.core.dtos.doctor
{
    /// <summary>
    /// Data Transfer Object for an item in the Doctor's patient queue.
    /// Represents a patient whose vitals have been completed and is ready for consultation.
    /// </summary>
    public class DoctorQueueItemDto
    {
        /// <summary>
        /// Gets or sets the ID of the visit.
        /// </summary>
        public int VisitId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the patient.
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the patient's full name.
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient's rank.
        /// </summary>
        public string? Rank { get; set; }

        /// <summary>
        /// Gets or sets the patient's age (calculated from Date of Birth).
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// Gets or sets the patient's gender.
        /// </summary>
        public string? Gender { get; set; } // As per FR-DN-002

        /// <summary>
        /// Gets or sets the timestamp when the patient became 'ReadyForDoctor'.
        /// This would typically be the `visits.status_updated_at` or `vitals.recorded_at`.
        /// </summary>
        public DateTime ReadyForDoctorTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the patient's priority for consultation.
        /// (e.g., "Normal", "Urgent", "High" - needs to be stored or derived).
        /// </summary>
        public string Priority { get; set; } = "Normal"; // Placeholder
    }
}
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.patient
{
    /// <summary>
    /// DTO for creating or updating patient medical history entries.
    /// </summary>
    public class CreateUpdatePatientMedicalHistoryDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the history entry. Required for update, 0 for create.
        /// </summary>
        public int HistoryId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the patient this history entry belongs to.
        /// This is crucial for associating the history with the correct patient.
        /// </summary>
        [Required(ErrorMessage = "Patient ID is required.")] // Added validation
        public int PatientId { get; set; } // NEWLY ADDED FIELD

        /// <summary>
        /// Gets or sets the type of the medical history entry (e.g., "Allergy", "Condition").
        /// </summary>
        [Required(ErrorMessage = "History type is required.")]
        [StringLength(100, ErrorMessage = "History type cannot exceed 100 characters.")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the medical history entry.
        /// </summary>
        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional onset date of the condition or event.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? OnsetDate { get; set; }

        /// <summary>
        /// Gets or sets the optional resolution date of the condition or event.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? ResolutionDate { get; set; }

        /// <summary>
        /// Gets or sets the optional severity level of the condition (e.g., "Mild", "Severe").
        /// </summary>
        [StringLength(50, ErrorMessage = "Severity cannot exceed 50 characters.")]
        public string? Severity { get; set; }

        /// <summary>
        /// Gets or sets any additional notes related to the history entry.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the history entry is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
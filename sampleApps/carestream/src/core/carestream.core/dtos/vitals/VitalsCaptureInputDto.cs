using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.vitals
{
    /// <summary>
    /// Data Transfer Object for capturing vital signs.
    /// Used as the model for the vitals capture form and for POST requests.
    /// </summary>
    public class VitalsCaptureInputDto
    {
        /// <summary>
        /// Gets or sets the ID of the visit for which vitals are being captured.
        /// This is crucial for associating vitals with the correct patient encounter.
        /// </summary>
        [Required(ErrorMessage = "Visit ID is required.")] // Already required, but adding custom message
        public int VisitId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the patient for whom vitals are being captured.
        /// Useful for context and display, though VisitId is the primary key for the record.
        /// </summary>
        [Required(ErrorMessage = "Patient ID is required.")] // Already required, but adding custom message
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the patient's name (for display purposes on the capture form).
        /// </summary>
        public string? PatientName { get; set; }

        /// <summary>
        /// Gets or sets the patient's rank (for display purposes).
        /// </summary>
        public string? PatientRank { get; set; }

        // FR-VS-006: Blood Pressure (Systolic/Diastolic)
        /// <summary>
        /// Gets or sets the systolic blood pressure reading.
        /// </summary>
        [Required(ErrorMessage = "Systolic Blood Pressure is required.")] // MADE REQUIRED
        [Range(30, 300, ErrorMessage = "Systolic BP must be between 30 and 300.")]
        public int? BloodPressureSystolic { get; set; }

        /// <summary>
        /// Gets or sets the diastolic blood pressure reading.
        /// </summary>
        [Required(ErrorMessage = "Diastolic Blood Pressure is required.")] // MADE REQUIRED
        [Range(20, 200, ErrorMessage = "Diastolic BP must be between 20 and 200.")]
        public int? BloodPressureDiastolic { get; set; }

        /// <summary>
        /// Gets or sets the heart rate in beats per minute.
        /// </summary>
        [Required(ErrorMessage = "Heart Rate is required.")] // MADE REQUIRED
        [Range(20, 300, ErrorMessage = "Heart Rate must be between 20 and 300.")]
        public int? HeartRate { get; set; }

        /// <summary>
        /// Gets or sets the body temperature (e.g., in Celsius or Fahrenheit - unit context needed).
        /// </summary>
        [Required(ErrorMessage = "Temperature is required.")] // MADE REQUIRED
        [Range(30.0, 45.0, ErrorMessage = "Temperature seems out of typical human range (assuming Celsius). Adjust if Fahrenheit.")]
        public decimal? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the respiratory rate in breaths per minute.
        /// </summary>
        [Required(ErrorMessage = "Respiratory Rate is required.")] // MADE REQUIRED
        [Range(5, 60, ErrorMessage = "Respiratory Rate must be between 5 and 60.")]
        public int? RespiratoryRate { get; set; }

        /// <summary>
        /// Gets or sets the oxygen saturation percentage (SpO2).
        /// </summary>
        [Required(ErrorMessage = "Oxygen Saturation is required.")] // MADE REQUIRED
        [Range(50, 100, ErrorMessage = "Oxygen Saturation must be between 50 and 100.")]
        public int? OxygenSaturation { get; set; }

        /// <summary>
        /// Gets or sets the pain level, typically on a 0-10 scale.
        /// </summary>
        [Range(0, 10, ErrorMessage = "Pain Level must be between 0 and 10.")]
        public int? PainLevel { get; set; } // Kept optional

        // FR-VS-007: Urinalysis results (Kept optional)
        /// <summary>
        /// Gets or sets the color observed in the urinalysis.
        /// </summary>
        public string? UrinalysisColor { get; set; }
        // ... other urinalysis fields (remain optional) ...
        public string? UrinalysisClarity { get; set; }
        public decimal? UrinalysisSpecificGravity { get; set; }
        public decimal? UrinalysisPh { get; set; }
        public string? UrinalysisProtein { get; set; }
        public string? UrinalysisGlucose { get; set; }


        // FR-VS-008: Clinical Notes (Kept optional)
        /// <summary>
        /// Gets or sets any clinical notes added by the nurse during vitals capture.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Clinical notes cannot exceed 2000 characters.")]
        public string? ClinicalNotes { get; set; }

        // FR-VS-009: Checkboxes (Kept as bool, default to false, effectively optional to check)
        /// <summary>
        /// Gets or sets a value indicating whether this patient requires follow-up.
        /// </summary>
        public bool RequiresFollowUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this patient should be marked as urgent.
        /// </summary>
        public bool MarkAsUrgent { get; set; }

        /// <summary>
        /// Timestamp of when the vitals were recorded by the nurse.
        /// </summary>
        public DateTimeOffset? RecordedAt { get; set; }

        /// <summary>
        /// ID of the user (nurse) who recorded these vitals.
        /// </summary>
        public int? RecordedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who recorded the vitals (for display).
        /// Populated when retrieving vitals.
        /// </summary>
        public string? RecordedByUserName { get; set; }
    }
}
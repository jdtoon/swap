using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.patient
{
    /// <summary>
    /// DTO for displaying patient medical history entries.
    /// </summary>
    public class PatientMedicalHistoryDto
    {
        public int HistoryId { get; set; }
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty; // Consider enum: PatientHistoryType

        [Required]
        public string Description { get; set; } = string.Empty; // TEXT column in DB

        public DateTime? OnsetDate { get; set; }
        public DateTime? ResolutionDate { get; set; }

        [StringLength(50)]
        public string? Severity { get; set; } // Consider enum: PatientHistorySeverity

        public string? Notes { get; set; } // TEXT column in DB

        public DateTimeOffset RecordedAt { get; set; }
        public int? RecordedByUserId { get; set; }
        public string? RecordedByUserName { get; set; } // Populated by join

        public bool IsActive { get; set; }
    }
}
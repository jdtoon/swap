using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// DTO for creating or updating visit assessment information.
    /// </summary>
    public class CreateUpdateVisitAssessmentDto
    {
        public int VisitAssessmentId { get; set; } // For update, 0 or null for create

        [Required]
        public int VisitId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int AssessedByUserId { get; set; } // User who performs the assessment

        public string? PhysicalExamFindings { get; set; }
        public string? CardiovascularNotes { get; set; }
        public string? RespiratoryNotes { get; set; }
        public string? MusculoskeletalNotes { get; set; }
        public string? NeurologicalNotes { get; set; }
        public string? PsychologicalNotes { get; set; }
        public string? OtherSystemsNotes { get; set; }

        [StringLength(100)]
        public string? MedicalClassification { get; set; }

        [StringLength(100)]
        public string? DeploymentStatus { get; set; }

        public int? ValidityPeriodMonths { get; set; }

        public string? Restrictions { get; set; }
    }
}
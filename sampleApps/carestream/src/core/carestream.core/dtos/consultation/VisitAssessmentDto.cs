using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// DTO for displaying visit assessment information.
    /// </summary>
    public class VisitAssessmentDto
    {
        public int VisitAssessmentId { get; set; }
        public int VisitId { get; set; }
        public int PatientId { get; set; }

        public DateTimeOffset AssessmentDate { get; set; }
        public int? AssessedByUserId { get; set; }
        public string? AssessedByUserName { get; set; } // Populated by join

        public string? PhysicalExamFindings { get; set; } // TEXT column
        public string? CardiovascularNotes { get; set; } // TEXT column
        public string? RespiratoryNotes { get; set; } // TEXT column
        public string? MusculoskeletalNotes { get; set; } // TEXT column
        public string? NeurologicalNotes { get; set; } // TEXT column
        public string? PsychologicalNotes { get; set; } // TEXT column
        public string? OtherSystemsNotes { get; set; } // TEXT column

        [StringLength(100)]
        public string? MedicalClassification { get; set; } // Consider enum: MedicalClassification

        [StringLength(100)]
        public string? DeploymentStatus { get; set; } // Consider enum: DeploymentStatus

        public int? ValidityPeriodMonths { get; set; } // INT column
        public string? Restrictions { get; set; } // TEXT column
    }
}
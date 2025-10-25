using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    public class SickNoteInputDto
    {
        public int? SickNoteId { get; set; } // Nullable for new, has value for existing

        [Required]
        public int VisitId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        public string? Diagnosis { get; set; }

        [StringLength(2000)]
        public string? Recommendations { get; set; }

        // For display/audit, populated by the system
        public DateTime? IssuedAt { get; set; } // When it was last saved/issued
        public string? IssuedByUserName { get; set; } // User who saved it

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            {
                yield return new ValidationResult("End Date cannot be before Start Date.", new[] { nameof(EndDate) });
            }
            // At least one of the main fields (dates, diagnosis, recommendations) should be present to save a note
            if (!StartDate.HasValue && !EndDate.HasValue && string.IsNullOrWhiteSpace(Diagnosis) && string.IsNullOrWhiteSpace(Recommendations))
            {
                // This validation might be too strict if users want to clear a note.
                // Adjust based on desired behavior.
                // yield return new ValidationResult("At least one sick note field (Dates, Diagnosis, or Recommendations) must be provided.", new[] { nameof(Diagnosis) });
            }
        }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// DTO for displaying patient referral information.
    /// </summary>
    public class ReferralDto
    {
        public int ReferralId { get; set; }
        public int VisitId { get; set; }
        public int PatientId { get; set; }

        public int ReferredByUserId { get; set; }
        public string? ReferredByUserName { get; set; } // Populated by join

        public int? ReferredToDepartmentId { get; set; }
        public string? ReferredToDepartmentName { get; set; } // Populated by join

        public int? ReferredToFacilityId { get; set; }
        public string? ReferredToFacilityName { get; set; } // Populated by join

        [Required]
        public string ReferralReason { get; set; } = string.Empty; // TEXT column

        public string? ReferralNotes { get; set; } // TEXT column

        public DateTimeOffset ReferralDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Consider enum: ReferralStatus

        public DateTimeOffset? CompletedDate { get; set; }
        public int? CompletedByUserId { get; set; }
        public string? CompletedByUserName { get; set; } // Populated by join
    }
}
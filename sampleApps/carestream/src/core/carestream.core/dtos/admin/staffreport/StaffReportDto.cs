using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.staffreport
{
    /// <summary>
    /// DTO for displaying staff report information.
    /// </summary>
    public class StaffReportDto
    {
        public int ReportId { get; set; }

        [Required]
        public int AuthorUserId { get; set; }
        public string? AuthorUserName { get; set; } // Populated by join
        public string? AuthorUserRank { get; set; } // Populated by join

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Priority { get; set; } // Consider enum: StaffReportPriority

        [Required]
        public string Content { get; set; } = string.Empty; // TEXT column

        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public int FacilityId { get; set; }
        public string? FacilityName { get; set; } // Populated by join

        public int? DepartmentId { get; set; } // Can be nullable
        public string? DepartmentName { get; set; } // Populated by join
    }
}
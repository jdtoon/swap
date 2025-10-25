using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.staffreport
{
    /// <summary>
    /// DTO for creating or updating staff report information.
    /// </summary>
    public class CreateUpdateStaffReportDto
    {
        public int ReportId { get; set; } // For update, 0 for create

        [Required]
        public int AuthorUserId { get; set; } // User who creates/updates the report

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Priority { get; set; } // Consider enum: StaffReportPriority

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int FacilityId { get; set; } // The facility this report belongs to

        public int? DepartmentId { get; set; } // Optional department linkage
    }
}
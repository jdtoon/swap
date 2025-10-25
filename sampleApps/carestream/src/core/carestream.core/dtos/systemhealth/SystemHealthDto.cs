using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.systemhealth
{
    /// <summary>
    /// DTO for representing the health status of a single system component.
    /// </summary>
    public class SystemHealthDto
    {
        [Required]
        [StringLength(100)]
        public string Component { get; set; } = string.Empty; // e.g., "Database", "API", "Storage"

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Unknown"; // e.g., "Healthy", "Unhealthy", "Degraded"

        public DateTimeOffset LastChecked { get; set; }

        public string? Details { get; set; } // Optional additional information
    }
}
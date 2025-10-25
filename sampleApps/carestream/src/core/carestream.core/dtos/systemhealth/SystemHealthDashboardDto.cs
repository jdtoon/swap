using System.Collections.Generic;
using carestream.core.dtos.admin.systemhealth; // For SystemHealthDto

namespace carestream.core.dtos.admin.systemhealth
{
    /// <summary>
    /// Represents a comprehensive DTO for the system health dashboard,
    /// aggregating health statuses of various components.
    /// </summary>
    public class SystemHealthDashboardDto
    {
        /// <summary>
        /// Gets or sets the list of individual system health components and their statuses.
        /// </summary>
        public List<SystemHealthDto> ComponentStatuses { get; set; } = new List<SystemHealthDto>();

        /// <summary>
        /// Gets or sets an overall system status. This can be derived from component statuses.
        /// e.g., "Operational", "Degraded", "Critical".
        /// </summary>
        public string OverallStatus { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets a general message or summary about the system's health.
        /// </summary>
        public string Message { get; set; } = "System health check pending or unknown.";
    }
}
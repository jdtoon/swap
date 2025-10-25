using System;

namespace carestream.core.dtos.patientadmin
{
    /// <summary>
    /// Data Transfer Object representing a single patient in the queue.
    /// </summary>
    public class PatientQueueItemDto
    {
        /// <summary>
        /// Gets or sets the unique ID of the visit associated with this queue item.
        /// </summary>
        public int VisitId { get; set; }

        /// <summary>
        /// Gets or sets the unique ID of the patient.
        /// </summary>
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the full name of the patient.
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patient's military rank.
        /// </summary>
        public string? Rank { get; set; }

        /// <summary>
        /// Gets or sets the patient's force number or other unique identifier.
        /// </summary>
        public string? ForceNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient's current status in the queue (e.g., "Waiting for Vitals", "Ready for Doctor").
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp relevant to the patient's current queue status (e.g., check-in time, or when became "Ready for Doctor").
        /// </summary>
        public DateTime RelevantTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the priority of the patient's visit (e.g., "Normal", "Urgent", "High").
        /// </summary>
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Gets or sets the name or description of who the patient is currently assigned to or with (e.g., "Dr. Smith", "Vitals Room").
        /// </summary>
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the patient's age (calculated from DateOfBirth).
        /// </summary>
        public int? Age { get; set; } // Added for board view

        /// <summary>
        /// Gets or sets the formatted string for how long the patient has been waiting (e.g., "15 min", "2h 30m").
        /// </summary>
        public string WaitTimeDisplay { get; set; } = string.Empty; // NEW PROPERTY

        /// <summary>
        /// Gets or sets the CSS class for coloring the wait time based on duration (e.g., "text-error", "text-warning").
        /// </summary>
        public string WaitTimeColorClass { get; set; } = string.Empty; // NEW PROPERTY
    }
}
namespace carestream.core.dtos.checkin
{
    /// <summary>
    /// Data Transfer Object for confirming patient check-in.
    /// </summary>
    public class CheckinConfirmationDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the check-in operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the name of the patient for confirmation display.
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated wait time for the patient (e.g., "15-20 min").
        /// </summary>
        public string EstimatedWaitTime { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target department/queue for notification (e.g., "Vitals Queue").
        /// </summary>
        public string NotificationTarget { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets recommended next steps for the patient.
        /// </summary>
        public string NextSteps { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an error message if the check-in failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the ID of the visit that was created or resumed.
        /// </summary>
        public int? VisitId { get; set; }

        /// <summary>
        /// Gets or sets the brief reason for the visit entered during check-in.
        /// </summary>
        public string? BriefReason { get; set; }

        /// <summary>
        /// Gets or sets additional notes entered during check-in.
        /// </summary>
        public string? AdditionalNotes { get; set; }
    }
}
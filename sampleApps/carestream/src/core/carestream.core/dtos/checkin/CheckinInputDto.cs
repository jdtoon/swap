using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.checkin
{
    /// <summary>
    /// Data Transfer Object for capturing check-in input from the UI.
    /// </summary>
    public class CheckinInputDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the patient being checked in.
        /// </summary>
        [Required(ErrorMessage = "Patient ID is required for check-in.")]
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets a brief reason for the patient's visit. This field is required.
        /// </summary>
        [Required(ErrorMessage = "Brief Reason for Visit is required.")]
        [StringLength(255, ErrorMessage = "Brief Reason cannot exceed 255 characters.")]
        public string BriefReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional additional notes for the patient's visit.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Additional Notes cannot exceed 1000 characters.")]
        public string? AdditionalNotes { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// DTO for creating or updating patient referral information.
    /// </summary>
    public class CreateUpdateReferralDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the referral. Used for update, 0 for create.
        /// </summary>
        public int ReferralId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the visit this referral is associated with.
        /// </summary>
        [Required(ErrorMessage = "Visit ID is required.")]
        public int VisitId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the patient this referral is for.
        /// </summary>
        [Required(ErrorMessage = "Patient ID is required.")]
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created this referral.
        /// </summary>
        [Required(ErrorMessage = "Referring user ID is required.")]
        public int ReferredByUserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the department being referred to (optional).
        /// </summary>
        public int? ReferredToDepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the facility being referred to (optional, can be cross-facility).
        /// </summary>
        public int? ReferredToFacilityId { get; set; }

        /// <summary>
        /// Gets or sets the reason for the referral.
        /// </summary>
        [Required(ErrorMessage = "Reason for referral is required.")]
        [StringLength(1000, ErrorMessage = "Reason for referral cannot exceed 1000 characters.")]
        public string ReferralReason { get; set; } = string.Empty; // Corrected property name

        /// <summary>
        /// Gets or sets any additional notes for the referral.
        /// </summary>
        public string? ReferralNotes { get; set; } // Corrected property name

        /// <summary>
        /// Gets or sets the current status of the referral (e.g., "Pending", "Accepted", "Completed").
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Initial status, will be set by service/controller
    }
}
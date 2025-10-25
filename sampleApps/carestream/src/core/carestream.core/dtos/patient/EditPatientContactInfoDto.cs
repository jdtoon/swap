using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.patient
{
    /// <summary>
    /// Data Transfer Object for editing a patient's contact information.
    /// </summary>
    public class EditPatientContactInfoDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the patient.
        /// </summary>
        [Required]
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the patient's primary email address.
        /// </summary>
        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
        public string? EmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the patient's primary phone number.
        /// </summary>
        [Display(Name = "Primary Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string? PrimaryPhoneNumber { get; set; }
    }
}
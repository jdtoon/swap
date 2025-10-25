using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.patient
{
    /// <summary>
    /// Data Transfer Object for editing a patient's emergency contact information.
    /// </summary>
    public class EditPatientEmergencyContactInfoDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the patient.
        /// </summary>
        [Required]
        public int PatientId { get; set; }

        /// <summary>
        /// Gets or sets the name of the patient's emergency contact.
        /// </summary>
        [Display(Name = "Emergency Contact Name")]
        [StringLength(255, ErrorMessage = "Emergency contact name cannot exceed 255 characters.")]
        public string? EmergencyContactName { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the patient's emergency contact.
        /// </summary>
        [Display(Name = "Emergency Contact Phone")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(50, ErrorMessage = "Emergency contact phone number cannot exceed 50 characters.")]
        public string? EmergencyContactPhone { get; set; }
    }
}
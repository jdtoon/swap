using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.patient
{
    public class EditPatientPersonalInfoDto
    {
        [Required]
        public int PatientId { get; set; }

        [Display(Name = "Force Number")]
        public string ForceNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Rank { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(150)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(150)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(50)]
        public string? Gender { get; set; }

        [StringLength(100)]
        public string? Unit { get; set; }
    }
}

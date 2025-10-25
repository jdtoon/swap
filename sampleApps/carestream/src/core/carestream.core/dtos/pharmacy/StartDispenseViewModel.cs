using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.pharmacy
{
    public class StartDispenseViewModel
    {
        [Required]
        public int VisitId { get; set; }
        public string PatientName { get; set; } = string.Empty; // For display context
        public string PrescriptionIdentifier { get; set; } = string.Empty; // For display context

        public List<DispenseItemDto> ItemsToDispense { get; set; } = new List<DispenseItemDto>();

        [Display(Name = "Pharmacist Verification Code")]
        // [Required(ErrorMessage = "Verification code is required to confirm dispense.")] // Add when ready to validate
        public string? PharmacistVerificationCode { get; set; }

        public string? DispensingNotes { get; set; } // Optional notes by pharmacist for this dispense event
    }
}
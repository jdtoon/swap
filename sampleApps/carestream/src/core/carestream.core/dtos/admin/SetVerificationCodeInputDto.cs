using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin
{
    public class SetVerificationCodeInputDto
    {
        [Required]
        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty; // For display in modal

        [Required(ErrorMessage = "New verification code is required.")]
        [RegularExpression(@"^\d{4,6}$", ErrorMessage = "Verification code must be 4 to 6 digits.")] // Example: 4-6 digits
        [Display(Name = "New Verification Code")]
        public string NewVerificationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmation code is required.")]
        [Compare(nameof(NewVerificationCode), ErrorMessage = "Verification code and confirmation code do not match.")]
        [Display(Name = "Confirm New Code")]
        public string ConfirmNewVerificationCode { get; set; } = string.Empty;
    }
}
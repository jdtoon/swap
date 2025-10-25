using System.ComponentModel.DataAnnotations;

namespace ttw.Dtos.Identity
{
    public class TwoFactorDto
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string? TwoFactorCode { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }
}
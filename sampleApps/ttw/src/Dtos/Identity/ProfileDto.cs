using System.ComponentModel.DataAnnotations;

namespace ttw.Dtos.Identity
{
    public class ProfileDto
    {
        public string? Username { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
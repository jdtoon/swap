using System.ComponentModel.DataAnnotations;

namespace ttw.Dtos.Identity
{
    public class EmailDto
    {
        public string? Email { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string? NewEmail { get; set; }
    }
}
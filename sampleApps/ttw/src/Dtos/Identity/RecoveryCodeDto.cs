using System.ComponentModel.DataAnnotations;

namespace ttw.Dtos.Identity
{
    public class RecoveryCodeDto
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string? RecoveryCode { get; set; }
    }
}
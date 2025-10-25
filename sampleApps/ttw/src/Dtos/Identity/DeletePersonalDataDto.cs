using System.ComponentModel.DataAnnotations;

namespace ttw.Dtos.Identity
{
    public class DeletePersonalDataDto
    {
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
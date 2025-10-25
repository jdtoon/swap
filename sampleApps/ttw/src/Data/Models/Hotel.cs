using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ttw.Data.Models
{
    public class Hotel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Hotel")]
        [StringLength(50)]
        public required string Name { get; set; }

        [NotMapped]
        public int MapCityId { get; set; }

        public required virtual City City { get; set; }
    }
}
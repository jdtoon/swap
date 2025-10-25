using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ttw.Data.Models
{
    public class City
    {
        public City()
        {
            Hotels = new HashSet<Hotel>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "City")]
        [StringLength(50)]
        public required string Name { get; set; }

        [StringLength(200)]
        public required string Image { get; set; }

        public virtual ICollection<Hotel> Hotels { get; set; }
    }
}
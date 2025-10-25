using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ttw.Data.Models
{
    public class Currency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Currency")]
        [StringLength(50)]
        public required string Name { get; set; }

        [Display(Name = "Long Name")]
        [StringLength(100)]
        public string LongName { get; set; } = null!;

        [Display(Name = "Exchange Rate")]
        public decimal Rate { get; set; }

        [Display(Name = "Round")]
        public int RoundOff { get; set; }

        [Display(Name = "Markup")]
        public int Markup { get; set; }
    }
}
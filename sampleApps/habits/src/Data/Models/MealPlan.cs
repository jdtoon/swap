using System.ComponentModel.DataAnnotations;

namespace habits.Data.Models
{
    public class MealPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string MondayMeal { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TuesdayMeal { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string WednesdayMeal { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ThursdayMeal { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FridayMeal { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string SaturdayMeal { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string SundayMeal { get; set; } = string.Empty;
    }
} 
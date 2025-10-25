using habits.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace habits.Dtos
{
    public class MealPlanDto
    {
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

        public static MealPlanDto FromModel(MealPlan mealPlan)
        {
            return new MealPlanDto
            {
                Id = mealPlan.Id,
                MondayMeal = mealPlan.MondayMeal,
                TuesdayMeal = mealPlan.TuesdayMeal,
                WednesdayMeal = mealPlan.WednesdayMeal,
                ThursdayMeal = mealPlan.ThursdayMeal,
                FridayMeal = mealPlan.FridayMeal,
                SaturdayMeal = mealPlan.SaturdayMeal,
                SundayMeal = mealPlan.SundayMeal
            };
        }

        public static MealPlan ToModel(MealPlanDto dto)
        {
            return new MealPlan
            {
                Id = dto.Id,
                MondayMeal = dto.MondayMeal,
                TuesdayMeal = dto.TuesdayMeal,
                WednesdayMeal = dto.WednesdayMeal,
                ThursdayMeal = dto.ThursdayMeal,
                FridayMeal = dto.FridayMeal,
                SaturdayMeal = dto.SaturdayMeal,
                SundayMeal = dto.SundayMeal,
            };
        }
    }
} 
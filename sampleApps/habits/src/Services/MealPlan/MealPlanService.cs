using habits.Data;
using habits.Dtos;
using Microsoft.EntityFrameworkCore;

namespace habits.Services.MealPlan
{
    public class MealPlanService : IMealPlanService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MealPlanService> _logger;

        public MealPlanService(ApplicationDbContext context, ILogger<MealPlanService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MealPlanDto> GetMealPlanAsync()
        {
            var mealPlan = await _context.MealPlan.FirstOrDefaultAsync();

            if (mealPlan == null)
            {
                mealPlan = new Data.Models.MealPlan
                {
                    MondayMeal = "",
                    TuesdayMeal = "",
                    WednesdayMeal = "",
                    ThursdayMeal = "",
                    FridayMeal = "",
                    SaturdayMeal = "",
                    SundayMeal = ""
                };
                _context.MealPlan.Add(mealPlan);
                await _context.SaveChangesAsync();
            }

            return MealPlanDto.FromModel(mealPlan);
        }

        public async Task<bool> UpdateMealAsync(int id, string day, string meal)
        {
            var mealPlan = await _context.MealPlan.FirstOrDefaultAsync(x => x.Id == id);

            if (mealPlan == null)
                return false;

            switch (day.ToLower())
            {
                case "monday":
                    mealPlan.MondayMeal = meal;
                    break;
                case "tuesday":
                    mealPlan.TuesdayMeal = meal;
                    break;
                case "wednesday":
                    mealPlan.WednesdayMeal = meal;
                    break;
                case "thursday":
                    mealPlan.ThursdayMeal = meal;
                    break;
                case "friday":
                    mealPlan.FridayMeal = meal;
                    break;
                case "saturday":
                    mealPlan.SaturdayMeal = meal;
                    break;
                case "sunday":
                    mealPlan.SundayMeal = meal;
                    break;
                default:
                    return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
} 
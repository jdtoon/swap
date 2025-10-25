using habits.Dtos;

namespace habits.Services.MealPlan
{
    public interface IMealPlanService
    {
        Task<MealPlanDto> GetMealPlanAsync();
        Task<bool> UpdateMealAsync(int id, string day, string meal);
    }
} 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Services.MealPlan;

namespace habits.Controllers
{
    [Authorize]
    public class MealPlanController : Controller
    {
        private readonly ILogger<MealPlanController> _logger;
        private readonly IMealPlanService _mealPlanService;

        public MealPlanController(ILogger<MealPlanController> logger, IMealPlanService mealPlanService)
        {
            _logger = logger;
            _mealPlanService = mealPlanService;
        }

        public IActionResult Index()
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();
            return View();
        }

        public async Task<IActionResult> GetMealPlan()
        {
            var mealPlan = await _mealPlanService.GetMealPlanAsync();
            return PartialView("_MealPlan", mealPlan);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMeal(int id, string day, string meal)
        {
            var success = await _mealPlanService.UpdateMealAsync(id, day, meal);

            if (!success)
                return NotFound();

            return Ok();
        }
    }
} 
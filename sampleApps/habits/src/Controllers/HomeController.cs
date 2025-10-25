using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Services.Calendar;
using habits.Services.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace habits.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        private readonly ICalendarService _calendarService;

        public HomeController(ILogger<HomeController> logger,
                              IUserService userService,
                              ICalendarService calendarService)
        {
            _logger = logger;
            _userService = userService;
            _calendarService = calendarService;
        }

        public IActionResult Index()
        {
            var isHtmx = Request.Headers.ContainsKey("HX-Request");

            _logger.LogDebug("Home page requested. Authenticated: {IsAuthenticated}", 
                User.Identity?.IsAuthenticated);

            if (!User.Identity!.IsAuthenticated)
            {
                _logger.LogInformation("Unauthenticated user redirected to login");
                if (isHtmx)
                {
                    Response.Headers.Append("HX-Redirect", "/Identity/Account/Login");
                    return Redirect("/Identity/Account/Login");
                }
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.IsHTMXRequest = isHtmx;
            return isHtmx ? PartialView() : View();
        }

        [Authorize(Roles = "admin")]
        public IActionResult Admin()
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();

            return View();
        }

        public IActionResult Default()
        {
            return PartialView("_Default");
        }

        public IActionResult GetUserDisplay()
        {
            return PartialView("_UserDisplay", _userService.GetUserDisplay(HttpContext.User.Identity!.Name!));
        }

        public IActionResult GetSearchBar()
        {
            return PartialView("_SearchBar");
        }

        public IActionResult GetBurgerMenu()
        {
            return PartialView("_BurgerMenu");
        }

        public IActionResult GetBurgerMenuHome()
        {
            return PartialView("_BurgerMenuHome");
        }

        public IActionResult GetLoginPartial()
        {
            return PartialView("_LoginUser");
        }

        public IActionResult GetPageHeading(string heading)
        {
            ViewData["PageHeading"] = heading;
            return PartialView("_PageHeading");
        }

        public IActionResult GetUpcomingEvents()
        {
            return PartialView("_UpcomingEvents", _calendarService.GetUpcomingEvents());
        }

        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public IActionResult ClearDiv()
        {
            return Content("");
        }
    }
}
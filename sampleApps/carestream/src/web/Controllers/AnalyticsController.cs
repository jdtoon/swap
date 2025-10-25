using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carestream.web.Controllers
{
    [Authorize]
    public class AnalyticsController : Controller
    {
        // GET: /Analytics/Index
        [HttpGet]
        public IActionResult Index()
        {
            return PartialView();
        }
    }
}
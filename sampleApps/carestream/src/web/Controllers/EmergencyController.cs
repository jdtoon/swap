using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carestream.web.Controllers
{
    [Authorize]
    public class EmergencyController : Controller
    {
        // GET: /Emergency/Index
        [HttpGet]
        public IActionResult Index()
        {
            return PartialView();
        }
    }
}
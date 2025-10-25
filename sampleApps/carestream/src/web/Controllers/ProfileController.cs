using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carestream.web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        // GET: /Profile/Index
        [HttpGet]
        public IActionResult Index()
        {
            return PartialView();
        }
    }
}
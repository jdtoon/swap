using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace carestream.web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}  


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace carestream.web.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        // GET: /Inventory/Index
        [HttpGet]
        public IActionResult Index()
        {
            return PartialView();
        }
    }
}
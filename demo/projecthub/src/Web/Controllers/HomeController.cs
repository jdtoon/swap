using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace ProjectHub.Web.Controllers;

public class HomeController : SwapController
{
    [HttpGet]
    public IActionResult Index() => SwapView();
}

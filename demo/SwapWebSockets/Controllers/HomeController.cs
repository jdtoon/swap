using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using SwapWebSockets.Events;

namespace SwapWebSockets.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SwapView();
    }

    [HttpPost]
    public IActionResult ClickMe()
    {
        return this.SwapResponse()
            .WithSuccessToast("Hello from Swap.Htmx!")
            .WithTrigger(AppEvents.User.Clicked)
            .Build();
    }

    [HttpGet]
    public IActionResult Message()
    {
        return PartialView("_Message");
    }
}

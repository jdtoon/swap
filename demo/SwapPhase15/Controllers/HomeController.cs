using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Attributes;
using SwapPhase15.Events;

namespace SwapPhase15.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SwapView();
    }

    [HttpPost]
    [SwapForm]
    public IActionResult SubmitForm(string message)
    {
        // If we get here, validation passed
        return this.SwapResponse()
            .WithSuccessToast("Form submitted successfully!")
            .WithTrigger(AppEvents.User.Clicked, message)
            .Build();
    }

    [HttpPost]
    public IActionResult ClickMe()
    {
        return this.SwapResponse()
            .WithSuccessToast("Hello from Swap.Htmx!")
            .WithTrigger(AppEvents.User.Clicked, "button clicked")
            .Build();
    }

    [HttpGet]
    public IActionResult Message()
    {
        return PartialView("_Message");
    }
}

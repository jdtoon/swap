using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Attributes;
using SwapPhase15.Events;

namespace SwapPhase15.Controllers;

public class HomeController : Controller
{
    private static int _totalIncrements = 0;
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
            .WithTrigger(AppEvents.User.Clicked, new UserClickedEvent { Message = message, Source = "form" })
            .Build();
    }

    [HttpPost]
    [SwapForm]
    public IActionResult SubmitFailForm([System.ComponentModel.DataAnnotations.Required] string email)
    {
        return this.SwapResponse().WithSuccessToast("This shouldn't happen!").Build();
    }

    [HttpPost]
    public IActionResult ClickMe()
    {
        return this.SwapResponse()
            .WithSuccessToast("Hello from Swap.Htmx!")
            .WithTrigger(AppEvents.User.Clicked, new UserClickedEvent { Message = "button clicked", Source = "button" })
            .Build();
    }

    [HttpPost]
    public IActionResult IncrementCounter(int count = 0)
    {
        var newCount = count + 1;
        _totalIncrements++;
        // The view _Counter will be swapped into #counter-section (innerHTML)
        // The handler will update the stats section via OOB
        return this.SwapResponse()
            .WithView("_Counter", newCount)
            .WithTrigger(AppEvents.Counter.Updated, new CounterUpdatedEvent { Count = newCount })
            .Build();
    }

    [HttpGet]
    public IActionResult UpdateStats()
    {
        return PartialView("_Stats", _totalIncrements);
    }
}

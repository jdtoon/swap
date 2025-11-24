using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Realtime;
using SwapRedisDemo.Events;

namespace SwapRedisDemo.Controllers;

public class HomeController : Controller
{
    private readonly ISseEventBridge _sse;

    public HomeController(ISseEventBridge sse)
    {
        _sse = sse;
    }

    public IActionResult Index()
    {
        return this.SwapView();
    }

    [HttpPost]
    public async Task<IActionResult> Broadcast(string message)
    {
        await _sse.HandleSseEventAsync("sse:redis-test", new { Message = message, Time = DateTime.Now });
        
        return this.SwapResponse()
            .WithSuccessToast("Broadcast sent via Redis!")
            .Build();
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

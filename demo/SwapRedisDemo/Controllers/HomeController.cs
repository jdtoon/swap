using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Realtime;
using SwapRedisDemo.Events;

namespace SwapRedisDemo.Controllers;

public class HomeController : Controller
{
    private readonly ISseEventBridge _sse;
    private readonly ISseConnectionRegistry _registry;

    public HomeController(ISseEventBridge sse, ISseConnectionRegistry registry)
    {
        _sse = sse;
        _registry = registry;
    }

    public IActionResult Index()
    {
        return this.SwapView();
    }

    [HttpPost]
    public async Task<IActionResult> Broadcast()
    {
        var time = DateTime.Now.ToString("HH:mm:ss.fff");
        var html = $"<div id=\"time-display\" hx-swap-oob=\"true\">{time}</div>";
        
        // Use the registry to broadcast the HTML directly
        await _registry.BroadcastAsync("sse:broadcast:redis-test", html);
        
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

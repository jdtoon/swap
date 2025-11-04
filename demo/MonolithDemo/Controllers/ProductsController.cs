using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Events;
using System.Text.Json;
using Swap.Htmx;

namespace MonolithDemo.Controllers;

public class ProductsController : Controller
{
    private readonly ISwapEventBus _events;

    public ProductsController(ISwapEventBus events)
    {
        _events = events;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
    var id = 7;
    await _events.EmitAsync(SwapEvents.Entity.CreatedKey(MonolithDemo.AppEntities.Product), new { id });
        return Content($"Created {id}");
    }

    [HttpPost]
    public async Task<IActionResult> CreateWithTrigger()
    {
    HtmxEvents.Trigger(Response, MonolithDemo.DemoEvents.Pre, "alpha");
    var id = 8;
    await _events.EmitAsync(SwapEvents.Entity.CreatedKey(MonolithDemo.AppEntities.Product), new { id });
        return Content($"Created {id} with pre-trigger");
    }

    [HttpPost]
    public async Task<IActionResult> CreateDuplicateEmits()
    {
        // Emit same event twice to verify last payload wins
    await _events.EmitAsync(SwapEvents.Entity.CreatedKey(MonolithDemo.AppEntities.Product), new { id = 1 });
    await _events.EmitAsync(SwapEvents.Entity.CreatedKey(MonolithDemo.AppEntities.Product), new { id = 2 });
        return Content("Created duplicate emits");
    }

    [HttpPost]
    public async Task<IActionResult> EmitDirectUiEventCollision()
    {
        // Pre-set UI event in HX-Trigger, then emit same event via bus to ensure override
        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new System.Collections.Generic.Dictionary<string, object?>
        {
            [SwapEvents.UI.RefreshListKey] = new { v = "alpha" }
        });
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { v = "beta" });
        return Content("Collision test");
    }

    [HttpPost]
    public async Task<IActionResult> MalformedPreTrigger()
    {
        // Pre-set a malformed HX-Trigger value; middleware should not crash and should still emit our event
        Response.Headers["HX-Trigger"] = "not-json";
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { status = "ok" });
        return Content("Malformed pre-trigger handled");
    }

    [HttpPost]
    public async Task<IActionResult> ExtremeEmit()
    {
        for (int i = 1; i <= 100; i++)
        {
            await _events.EmitAsync(new EventKey($"{MonolithDemo.AppEntities.UiComponentPrefix}{i}"), new { index = i });
        }
        return Content("Extreme emit complete");
    }

    [HttpPost]
    public IActionResult Noop()
    {
        // No events emitted
        return Content("No events");
    }

    [HttpPost]
    public IActionResult NoopWithPreTrigger()
    {
        // Pre-set HX-Trigger but emit no events; should be preserved as-is
    HtmxEvents.Trigger(Response, MonolithDemo.DemoEvents.PreOnly, "gamma");
        return Content("No events but pre-trigger");
    }

    [HttpPost]
    public async Task<IActionResult> DuplicateUiEmits()
    {
        // Emit same UI event twice; last payload should win
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { v = "one" });
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { v = "two" });
        return Content("Duplicate UI emits");
    }

    [HttpPost]
    public async Task<IActionResult> EmitThenBadRequest()
    {
        // Emit an event but return a 400 status to observe current behavior on non-2xx
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { state = "bad" });
        return BadRequest("Bad request after emit");
    }

    [HttpPost]
    public async Task<IActionResult> EmitThenRedirect()
    {
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { state = "redirect" });
        Response.Headers["HX-Redirect"] = "/Products/Noop";
        return Content("HX-Redirect set");
    }

    [HttpPost]
    public async Task<IActionResult> WriteThenEmit()
    {
        await Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("partial"));
        await Response.Body.FlushAsync();
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { after = "write" });
        return Content("done");
    }

    [HttpPost]
    public async Task<IActionResult> EmitThenThrow()
    {
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { state = "error" });
        throw new InvalidOperationException("boom");
    }

    [HttpPost]
    public async Task<IActionResult> EmitNestedCollision()
    {
        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new System.Collections.Generic.Dictionary<string, object?>
        {
            [SwapEvents.UI.RefreshListKey] = new { nested = new { x = 1 }, v = "alpha", keep = "y" }
        });
        await _events.EmitAsync(SwapEvents.UI.RefreshListKey, new { nested = new { x = 2 }, v = "beta" });
        return Content("nested collision");
    }
}

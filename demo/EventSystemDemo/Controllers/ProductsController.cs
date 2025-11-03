using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Events;

namespace EventSystemDemo.Controllers;

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
        // Simulate creating a product with id 42
        var id = 42;
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id });

        // Return a simple partial content to keep focus on headers
        return Content($"Created {id}");
    }

    [HttpPost]
    public async Task<IActionResult> CreateWithTrigger()
    {
        // Pre-set an HX-Trigger header to verify merge behavior
        Response.Headers["HX-Trigger"] = "{\"pre\":\"alpha\"}";

        var id = 101;
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id });

        return Content($"Created {id} with pre-trigger");
    }

    [HttpPost]
    public async Task<IActionResult> CreateDuplicateEmits()
    {
        // Emit same event twice to verify last payload wins
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 1 });
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 2 });
        return Content("Created duplicate emits");
    }

    [HttpPost]
    public async Task<IActionResult> EmitDirectUiEventCollision()
    {
        // Controller sets HX-Trigger for refreshList, then event bus emits same event with different payload
        Response.Headers["HX-Trigger"] = "{\"ui.refreshList\":{\"v\":\"alpha\"}}";
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { v = "beta" });
        return Content("Collision test");
    }

    [HttpPost]
    public async Task<IActionResult> MalformedPreTrigger()
    {
        // Pre-set a malformed HX-Trigger value; middleware should not crash and should still emit our event
        Response.Headers["HX-Trigger"] = "not-json";
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { status = "ok" });
        return Content("Malformed pre-trigger handled");
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
        Response.Headers["HX-Trigger"] = "{\"preOnly\":\"gamma\"}";
        return Content("No events but pre-trigger");
    }

    [HttpPost]
    public async Task<IActionResult> DuplicateUiEmits()
    {
        // Emit same UI event twice; last payload should win
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { v = "one" });
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { v = "two" });
        return Content("Duplicate UI emits");
    }

    [HttpPost]
    public async Task<IActionResult> ExtremeEmit()
    {
        // Emit a large number of UI events to simulate extreme component builds
        for (int i = 1; i <= 100; i++)
        {
            await _events.EmitAsync($"ui.component{i}", new { index = i });
        }
        return Content("Extreme emit complete");
    }

    [HttpPost]
    public async Task<IActionResult> EmitThenBadRequest()
    {
        // Emit an event but return a 400 status to observe current behavior on non-2xx
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { state = "bad" });
        return BadRequest("Bad request after emit");
    }

    [HttpPost]
    public async Task<IActionResult> EmitThenRedirect()
    {
        // Emit an event and then instruct client to redirect via HX-Redirect
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { state = "redirect" });
        Response.Headers["HX-Redirect"] = "/Products/Noop";
        return Content("HX-Redirect set");
    }

    [HttpPost]
    public async Task<IActionResult> WriteThenEmit()
    {
        // Write to the response body first (this starts the response and triggers OnStarting)
        await Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("partial"));
        await Response.Body.FlushAsync();

        // Emit after the response has started; current behavior will NOT include this in HX-Trigger
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { after = "write" });

        // Finish the response
        return Content("done");
    }

    [HttpPost]
    public async Task<IActionResult> EmitThenThrow()
    {
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { state = "error" });
        throw new InvalidOperationException("boom");
    }

    [HttpPost]
    public async Task<IActionResult> EmitNestedCollision()
    {
        // Pre-set nested payload for ui.refreshList
        Response.Headers["HX-Trigger"] = "{\"ui.refreshList\":{\"nested\":{\"x\":1},\"v\":\"alpha\",\"keep\":\"y\"}}";
        // Emit nested payload for same key; last-write-wins should replace entire object for that key
        await _events.EmitAsync(SwapEvents.UI.RefreshList, new { nested = new { x = 2 }, v = "beta" });
        return Content("nested collision");
    }
}

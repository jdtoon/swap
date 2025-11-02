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
    public IActionResult Noop()
    {
        // No events emitted
        return Content("No events");
    }
}

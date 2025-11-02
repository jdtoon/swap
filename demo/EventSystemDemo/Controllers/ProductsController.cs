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
}

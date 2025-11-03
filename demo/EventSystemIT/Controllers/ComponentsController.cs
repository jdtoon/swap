using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Events;
using EventSystemIT.Events;

namespace EventSystemIT.Controllers;

public class ComponentsController : SwapController
{
    private readonly ISwapEventBus _events;

    // Demo state for two reusable tiles
    private static int CounterA = 0;
    private static int CounterB = 0;

    public ComponentsController(ISwapEventBus events)
    {
        _events = events;
    }

    [HttpGet]
    public IActionResult Generic()
    {
        return SwapView();
    }

    // Content endpoint rendered inside each reusable tile instance
    [HttpGet]
    public IActionResult Counter(string name)
    {
        var value = string.Equals(name, "b", StringComparison.OrdinalIgnoreCase) ? CounterB : CounterA;
        ViewData["Name"] = (name ?? "a").ToLowerInvariant();
        return PartialView("~/Views/Components/_CounterContent.cshtml", value);
    }

    [HttpPost]
    public IActionResult Increment(string name)
    {
        var isB = string.Equals(name, "b", StringComparison.OrdinalIgnoreCase);
        if (isB) CounterB++; else CounterA++;

        // Emit domain event mapped to a UI event unique to the tile instance
        if (isB)
        {
            _events.Emit(EventNames.Domain.ComponentBUpdated, new { value = CounterB });
            Response.ShowSuccessToast($"Tile B incremented to {CounterB}");
        }
        else
        {
            _events.Emit(EventNames.Domain.ComponentAUpdated, new { value = CounterA });
            Response.ShowSuccessToast($"Tile A incremented to {CounterA}");
        }

        // Let the chain trigger the tile refresh; content is reloaded by the container's hx-trigger
        return NoContent();
    }
}

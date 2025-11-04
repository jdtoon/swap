using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using ModularMonolithDemo.Modules.Demo.Module;
using ModularMonolithDemo.Modules.Demo.Contracts;
using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Demo.Web.Controllers;

[Route("Components")] 
public class ComponentsController : SwapController
{
    private readonly IComponentsService _components;
    private readonly ISwapEventBus _bus;
    public ComponentsController(IComponentsService components, ISwapEventBus bus) { _components = components; _bus = bus; }

    [HttpGet("Generic")]
    public IActionResult Generic() => SwapView("~/Views/Components/Generic.cshtml");

    [HttpGet("Counter")]
    public async Task<IActionResult> Counter(string name)
    {
        var value = await _components.GetCounterAsync(name);
        ViewData["Name"] = (name ?? "a").ToLowerInvariant();
        return PartialView("~/Views/Components/_CounterContent.cshtml", value);
    }

    [HttpPost("Increment")]
    public async Task<IActionResult> Increment(string name)
    {
        var value = await _components.IncrementAsync(name);
        Response.ShowSuccessToast($"Tile {(string.Equals(name, "b", StringComparison.OrdinalIgnoreCase) ? "B" : "A")} incremented to {value}");
        var key = (name ?? "a").Equals("b", StringComparison.OrdinalIgnoreCase)
            ? EventNames.Ui.ComponentBRefresh
            : EventNames.Ui.ComponentARefresh;
        _bus.Emit(key);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using ModularMonolithDemo.Modules.Todos.Contracts;
using Swap.Htmx.Events;
using Swap.Modularity.Abstractions;

namespace ModularMonolithDemo.Modules.Todos.Web.Controllers;

[Route("todos/ui")]
public class TodosUiController : Controller
{
    private readonly ITodoService _service;
    private readonly ISwapEventBus _bus;
    private readonly IEventChainRegistrar _events;
    public TodosUiController(ITodoService service, ISwapEventBus bus, IEventChainRegistrar events)
    {
        _service = service; _bus = bus; _events = events;
    }

    [HttpGet("list")]
    public IActionResult List()
    {
    var items = _service.GetAll().ToList();
    return PartialView("~/Views/TodosUi/_List.cshtml", items);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromForm] string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return BadRequest();
        var item = _service.Add(title);
        _bus.Emit(TodoEvents.Domain.Created, new { id = item.Id });
        await _events.PublishAsync(TodoEvents.Domain.Created, new { id = item.Id }, HttpContext.RequestServices);
        return NoContent();
    }

    [HttpPost("toggle/{id:int}")]
    public async Task<IActionResult> Toggle([FromRoute] int id)
    {
        var item = _service.Toggle(id);
        if (item is null) return NotFound();
        _bus.Emit(TodoEvents.Domain.Toggled, new { id });
        await _events.PublishAsync(TodoEvents.Domain.Toggled, new { id }, HttpContext.RequestServices);
        return PartialView("~/Views/TodosUi/_Item.cshtml", item);
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (_service.Delete(id))
        {
            _bus.Emit(TodoEvents.Domain.Deleted, new { id });
            await _events.PublishAsync(TodoEvents.Domain.Deleted, new { id }, HttpContext.RequestServices);
            return NoContent();
        }
        return NotFound();
    }
}

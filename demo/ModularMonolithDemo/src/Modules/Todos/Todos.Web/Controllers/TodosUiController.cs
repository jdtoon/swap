using Microsoft.AspNetCore.Mvc;
using ModularMonolithDemo.Modules.Todos.Module;
using ModularMonolithDemo.Modules.Todos.Contracts;
using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Todos.Web.Controllers;

[Route("todos/ui")]
public class TodosUiController : Controller
{
    private readonly ITodoService _service;
    private readonly ISwapEventBus _bus;
    public TodosUiController(ITodoService service, ISwapEventBus bus)
    {
        _service = service; _bus = bus;
    }

    [HttpGet("list")]
    public IActionResult List()
    {
    var items = _service.GetAll().ToList();
    return PartialView("~/Views/TodosUi/_List.cshtml", items);
    }

    [HttpPost("add")]
    public IActionResult Add([FromForm] string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return BadRequest();
        var item = _service.Add(title);
        _bus.Emit(TodoEvents.Domain.Created, new { id = item.Id });
        return NoContent();
    }

    [HttpPost("toggle/{id:int}")]
    public IActionResult Toggle([FromRoute] int id)
    {
        var item = _service.Toggle(id);
        if (item is null) return NotFound();
    _bus.Emit(TodoEvents.Domain.Toggled, new { id });
    return PartialView("~/Views/TodosUi/_Item.cshtml", item);
    }

    [HttpDelete("delete/{id:int}")]
    public IActionResult Delete([FromRoute] int id)
    {
        if (_service.Delete(id))
        {
            _bus.Emit(TodoEvents.Domain.Deleted, new { id });
            return NoContent();
        }
        return NotFound();
    }
}

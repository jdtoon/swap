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
        return ViewComponent("TodosList");
    }

    [HttpPost("add")]
    public IActionResult Add([FromForm] string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return BadRequest();
        var item = _service.Add(title);
        _bus.Emit(TodoEvents.TodoCreated, new { id = item.Id });
        return Ok();
    }

    [HttpPost("toggle/{id:int}")]
    public IActionResult Toggle([FromRoute] int id)
    {
        var item = _service.Toggle(id);
        if (item is null) return NotFound();
        _bus.Emit(TodoEvents.TodoToggled, new { id });
        return PartialView("~/Modules/Todos/Todos.Web/Views/Shared/Components/TodosList/_TodoItem.cshtml", item);
    }

    [HttpDelete("delete/{id:int}")]
    public IActionResult Delete([FromRoute] int id)
    {
        if (_service.Delete(id))
        {
            _bus.Emit(TodoEvents.TodoDeleted, new { id });
            return Ok();
        }
        return NotFound();
    }
}

using Microsoft.AspNetCore.Mvc;
using TaskFlow.Data;
using TaskFlow.Models;
using Swap.Htmx.Events;
using Swap.Htmx;
using TaskFlow.Events;

namespace TaskFlow.Controllers;

public class HomeController : SwapController
{
    private readonly AppDbContext _context;
    private readonly ISwapEventBus _events;

    public HomeController(AppDbContext context, ISwapEventBus events)
    {
        _context = context;
        _events = events;
    }

    public IActionResult Index()
    {
        // Index page has NO data - just static content
        // The todo list loads separately via HTMX
        return SwapView();
    }

    // Separate endpoint for the todo list component
    [HttpGet]
    public IActionResult TodoList()
    {
        var todos = _context.TodoItems.ToList();
        return PartialView("_TodoList", todos);
    }

    [HttpPost]
    public IActionResult AddTodo(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest();
        }

        var todo = new TodoItem
        {
            Title = title,
            IsComplete = false
        };

        _context.TodoItems.Add(todo);
        _context.SaveChanges();

        // Emit domain event (chained to UI events via Events/SwapEventChains.cs)
        _events.Emit(EventNames.Domain.TodoCreated, new { id = todo.Id, title = todo.Title });

        // Show a success toast overlay via HX-Trigger
        Response.ShowSuccessToast($"Created: {todo.Title}");

    // Form is self-owned; let the list refresh via EventNames.Ui.TodoRefreshList chain
        return NoContent();
    }

    [HttpPost]
    public IActionResult ToggleTodo(int id)
    {
        var todo = _context.TodoItems.Find(id);
        if (todo == null)
        {
            return NotFound();
        }

        todo.IsComplete = !todo.IsComplete;
        _context.SaveChanges();

        // Emit a toggle event so stats can refresh via chain
        _events.Emit(EventNames.Domain.TodoToggled, new { id });

        return PartialView("_TodoList", _context.TodoItems.ToList());
    }

    [HttpPost]
    public IActionResult ToggleItem(int id)
    {
        var todo = _context.TodoItems.Find(id);
        if (todo == null)
        {
            return NotFound();
        }

        todo.IsComplete = !todo.IsComplete;
        _context.SaveChanges();

        _events.Emit(EventNames.Domain.TodoToggled, new { id });

        // Return just this single item
        return PartialView("_TodoItem", todo);
    }

    [HttpDelete]
    public IActionResult DeleteItem(int id)
    {
        var todo = _context.TodoItems.Find(id);
        if (todo == null)
        {
            return NotFound();
        }

        _context.TodoItems.Remove(todo);
        _context.SaveChanges();

        _events.Emit(EventNames.Domain.TodoDeleted, new { id = id });

        Response.ShowSuccessToast($"Deleted todo #{id}");

        // Item is self-owned; client will delete the li via hx-swap="delete"
        return Ok();
    }
}

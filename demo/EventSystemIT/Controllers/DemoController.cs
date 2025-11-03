using Microsoft.AspNetCore.Mvc;
using EventSystemIT.Data;
using Swap.Htmx;
using Swap.Htmx.Events;
using EventSystemIT.Events;

namespace EventSystemIT.Controllers;

public class DemoController : SwapController
{
    private readonly AppDbContext _context;
    private static readonly List<string> Activity = new();
    private static readonly List<string> Notes = new();
    private readonly ISwapEventBus _events;

    public DemoController(AppDbContext context, ISwapEventBus events)
    {
        _context = context;
        _events = events;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return SwapView();
    }

    [HttpGet]
    public IActionResult Stats()
    {
        var total = _context.TodoItems.Count();
        var completed = _context.TodoItems.Count(t => t.IsComplete);
        var pending = total - completed;
        return PartialView("~/Views/Demo/_Stats.cshtml", new EventSystemIT.Dtos.StatsModel
        {
            Total = total,
            Completed = completed,
            Pending = pending
        });
    }

    [HttpGet]
    public IActionResult Toasts()
    {
        var latest = _context.TodoItems.OrderByDescending(t => t.Id).Select(t => t.Title).FirstOrDefault();
        var message = latest == null ? "No recent activity" : $"Created: {latest}";
        return PartialView("~/Views/Demo/_Toasts.cshtml", message);
    }

    [HttpGet]
    public IActionResult ActivityLog()
    {
        var items = _context.TodoItems
            .OrderByDescending(t => t.Id)
            .Select(t => $"Created: {t.Title}")
            .Take(10)
            .ToList();
        return PartialView("~/Views/Demo/_ActivityLog.cshtml", items);
    }

    // --- Dynamic components demo ---
    [HttpGet]
    public IActionResult Dynamic()
    {
        return SwapView("~/Views/Demo/Dynamic.cshtml");
    }

    [HttpGet]
    public IActionResult Summary()
    {
        var count = Notes.Count;
        return PartialView("~/Views/Demo/_Summary.cshtml", count);
    }

    [HttpGet]
    public IActionResult Details()
    {
        return PartialView("~/Views/Demo/_Details.cshtml", Notes.ToList());
    }

    [HttpPost]
    public IActionResult AddNote(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest();
        }

        Notes.Add(text);
        Response.ShowSuccessToast($"Note added: {text}");
        _events.Emit(EventNames.Domain.ProjectNoteAdded, new { text });

        // Let the event chain trigger a single refresh of Details + Summary to avoid double swaps
        return NoContent();
    }

    // --- Bulk operations demo ---
    [HttpGet]
    public IActionResult Bulk()
    {
        return SwapView("~/Views/Demo/Bulk.cshtml");
    }

    [HttpGet]
    public IActionResult BulkTodos()
    {
        var todos = _context.TodoItems.ToList();
        return PartialView("~/Views/Demo/_BulkTodos.cshtml", todos);
    }

    [HttpPost]
    public IActionResult BulkComplete([FromForm] int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            Response.ShowInfoToast("No items selected");
            // Let the list container stay as-is; no content swap
            return NoContent();
        }

        var todos = _context.TodoItems.Where(t => ids.Contains(t.Id)).ToList();
        foreach (var t in todos)
        {
            t.IsComplete = true;
        }
        _context.SaveChanges();

        Response.ShowSuccessToast($"Completed {todos.Count} todos");
        _events.Emit(EventNames.Domain.BulkCompleted, new { count = todos.Count });

        // Chain will refresh list + stats; avoid double swaps
        return NoContent();
    }
}

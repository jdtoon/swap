using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using SwapPhase15.Events;

namespace SwapPhase15.Controllers;

public class TaskBoardController : Controller
{
    // Simulating a database
    private static readonly List<TaskItem> _tasks = new()
    {
        new(1, "Review PR #102", "Pending"),
        new(2, "Update Documentation", "Pending"),
        new(3, "Fix CSS Grid Issue", "Pending"),
        new(4, "Optimize Database Queries", "Pending"),
        new(5, "Write Unit Tests", "Pending"),
    };

    private static int _completedCount = 0;

    [HttpGet]
    public IActionResult Index()
    {
        var model = new TaskBoardViewModel
        {
            Tasks = _tasks.Where(t => t.Status == "Pending").ToList(),
            CompletedCount = _completedCount,
            TotalTasks = _tasks.Count + _completedCount
        };
        return this.SwapView(model);
    }

    [HttpPost]
    public IActionResult Complete(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return NotFound();

        // 1. Update "Database"
        _tasks.Remove(task);
        _completedCount++;

        // 2. Trigger Event
        // Notice: The controller knows NOTHING about the UI updates (stats, logs, removing the row).
        // It just says "This happened".
        return this.SwapResponse()
            .WithTrigger(TaskEvents.Task.Completed, new TaskCompletedEvent 
            { 
                TaskId = task.Id, 
                TaskTitle = task.Title,
                RemainingTasks = _tasks.Count,
                TotalTasks = _tasks.Count + _completedCount
            })
            .Build();
    }
    
    [HttpPost]
    public IActionResult Reset()
    {
        _tasks.Clear();
        _tasks.AddRange(new List<TaskItem>
        {
            new(1, "Review PR #102", "Pending"),
            new(2, "Update Documentation", "Pending"),
            new(3, "Fix CSS Grid Issue", "Pending"),
            new(4, "Optimize Database Queries", "Pending"),
            new(5, "Write Unit Tests", "Pending"),
        });
        _completedCount = 0;
        return RedirectToAction(nameof(Index));
    }
}

public record TaskItem(int Id, string Title, string Status);

public class TaskBoardViewModel
{
    public List<TaskItem> Tasks { get; set; } = new();
    public int CompletedCount { get; set; }
    public int TotalTasks { get; set; }
    public int Progress => TotalTasks == 0 ? 0 : (int)((double)CompletedCount / TotalTasks * 100);
}

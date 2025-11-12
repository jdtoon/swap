using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Dtos;
using TaskFlow.Events;
using Swap.Htmx;
using Swap.Htmx.Events;

namespace TaskFlow.Controllers;

public class TasksController : SwapController
{
    private readonly AppDbContext _context;
    private readonly ISwapEventBus _events;

    public TasksController(AppDbContext context, ISwapEventBus events)
    {
        _context = context;
        _events = events;
    }

    // Main task board view
    [HttpGet]
    public IActionResult Index()
    {
        return SwapView();
    }

    // Get task statistics (for OOB swap)
    [HttpGet]
    public IActionResult Stats()
    {
        var stats = new TaskStatsDto
        {
            TotalTasks = _context.Tasks.Count(),
            TodoCount = _context.Tasks.Count(t => t.Status == TaskItemStatus.Todo),
            InProgressCount = _context.Tasks.Count(t => t.Status == TaskItemStatus.InProgress),
            DoneCount = _context.Tasks.Count(t => t.Status == TaskItemStatus.Done),
            HighPriorityCount = _context.Tasks.Count(t => t.Priority >= TaskPriority.High),
            OverdueCount = _context.Tasks.Count(t => t.DueDate < DateTime.UtcNow && t.Status != TaskItemStatus.Done)
        };
        return PartialView("_Stats", stats);
    }

    // Get tasks by status (for kanban columns)
    [HttpGet]
    public IActionResult TaskColumn(TaskItemStatus status)
    {
        var tasks = _context.Tasks
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToList();
        
        ViewData["Status"] = status;
        return PartialView("_TaskColumn", tasks);
    }

    // Get single task card
    [HttpGet]
    public IActionResult TaskCard(int id)
    {
        var task = _context.Tasks.Find(id);
        if (task == null) return NotFound();
        
        return PartialView("_TaskCard", task);
    }

    // Show create task form
    [HttpGet]
    public IActionResult CreateForm()
    {
        return PartialView("_CreateTaskForm");
    }

    // Create new task
    [HttpPost]
    public IActionResult Create(CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            Response.ShowErrorToast("Please fill in all required fields");
            return PartialView("_CreateTaskForm", dto);
        }

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = (TaskPriority)dto.Priority,
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            Status = TaskItemStatus.Todo,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Log activity
        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Task Created",
            Details = $"{task.Title} (Priority: {task.Priority})",
            Timestamp = DateTime.UtcNow,
            Icon = "fas fa-plus-circle",
            ColorClass = "is-success"
        });
        _context.SaveChanges();

        _events.Emit(EventNames.Domain.TaskCreated, new { id = task.Id, title = task.Title });

        // Return empty form to clear it
        return PartialView("_CreateTaskForm");
    }

    // Show edit task form
    [HttpGet]
    public IActionResult EditForm(int id)
    {
        var task = _context.Tasks.Find(id);
        if (task == null) return NotFound();

        var dto = new UpdateTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = (int)task.Status,
            Priority = (int)task.Priority,
            AssignedTo = task.AssignedTo,
            DueDate = task.DueDate
        };

        return PartialView("_EditTaskForm", dto);
    }

    // Update task
    [HttpPut]
    public IActionResult Update(int id, UpdateTaskDto dto)
    {
        var task = _context.Tasks.Find(id);
        if (task == null) return NotFound();

        if (!ModelState.IsValid)
        {
            Response.ShowErrorToast("Please fill in all required fields");
            return PartialView("_EditTaskForm", dto);
        }

        var statusChanged = task.Status != (TaskItemStatus)dto.Status;
        var priorityChanged = task.Priority != (TaskPriority)dto.Priority;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = (TaskItemStatus)dto.Status;
        task.Priority = (TaskPriority)dto.Priority;
        task.AssignedTo = dto.AssignedTo;
        task.DueDate = dto.DueDate;

        if (task.Status == TaskItemStatus.Done && task.CompletedAt == null)
        {
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (task.Status != TaskItemStatus.Done)
        {
            task.CompletedAt = null;
        }

        _context.SaveChanges();

        // Log activity
        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Task Updated",
            Details = $"{task.Title}",
            Timestamp = DateTime.UtcNow,
            Icon = "fas fa-edit",
            ColorClass = "is-info"
        });
        _context.SaveChanges();

        if (statusChanged)
        {
            _events.Emit(EventNames.Domain.TaskStatusChanged, new { id = task.Id, status = task.Status });
        }
        else if (priorityChanged)
        {
            _events.Emit(EventNames.Domain.TaskPriorityChanged, new { id = task.Id, priority = task.Priority });
        }
        else
        {
            _events.Emit(EventNames.Domain.TaskUpdated, new { id = task.Id });
        }

        // Return the updated card
        return PartialView("_TaskCard", task);
    }

    // Quick status change (for drag & drop or quick actions)
    [HttpPatch]
    public IActionResult ChangeStatus(int id, TaskItemStatus newStatus)
    {
        var task = _context.Tasks.Find(id);
        if (task == null) return NotFound();

        var oldStatus = task.Status;
        task.Status = newStatus;

        if (newStatus == TaskItemStatus.Done && task.CompletedAt == null)
        {
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (newStatus != TaskItemStatus.Done)
        {
            task.CompletedAt = null;
        }

        _context.SaveChanges();

        // Log activity
        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Status Changed",
            Details = $"{task.Title}: {oldStatus} → {newStatus}",
            Timestamp = DateTime.UtcNow,
            Icon = "fas fa-arrows-alt",
            ColorClass = "is-warning"
        });
        _context.SaveChanges();

        _events.Emit(EventNames.Domain.TaskStatusChanged, new { id = task.Id, oldStatus, newStatus });

        return PartialView("_TaskCard", task);
    }

    // Delete task
    [HttpDelete]
    public IActionResult Delete(int id)
    {
        var task = _context.Tasks.Find(id);
        if (task == null) return NotFound();

        var title = task.Title;
        _context.Tasks.Remove(task);

        // Log activity
        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "Task Deleted",
            Details = title,
            Timestamp = DateTime.UtcNow,
            Icon = "fas fa-trash",
            ColorClass = "is-danger"
        });
        _context.SaveChanges();

        _events.Emit(EventNames.Domain.TaskDeleted, new { id, title });

        return NoContent();
    }

    // Get activity feed
    [HttpGet]
    public IActionResult ActivityFeed()
    {
        var activities = _context.ActivityLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();
        
        return PartialView("_ActivityFeed", activities);
    }

    // Search/Filter tasks
    [HttpGet]
    public IActionResult Search(TaskFilterDto filter)
    {
        var query = _context.Tasks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(t => t.Title.Contains(filter.Search) || t.Description.Contains(filter.Search));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == (TaskItemStatus)filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == (TaskPriority)filter.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            query = query.Where(t => t.AssignedTo.Contains(filter.AssignedTo));
        }

        var tasks = query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToList();

        return PartialView("_SearchResults", tasks);
    }
}

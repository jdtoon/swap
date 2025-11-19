using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using Swap.Htmx.Models;
using Swap.Htmx.ServerSentEvents;
using TaskFlow.Services;
using TaskFlow.Events;
using TaskFlow.Views;
using TaskItem = TaskFlow.Models.TaskItem;
using TaskStatus = TaskFlow.Models.TaskStatus;
using TaskPriority = TaskFlow.Models.TaskPriority;
using TaskInput = TaskFlow.Models.TaskInput;

namespace TaskFlow.Controllers;

/// <summary>
/// Demonstrates SSE, all swap modes, payload access, and complex event chains
/// </summary>
public class TasksController : SwapController
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly ITeamService _teamService;
    private readonly IActivityService _activityService;

    public TasksController(
        ITaskService taskService,
        IProjectService projectService,
        ITeamService teamService,
        IActivityService activityService)
    {
        _taskService = taskService;
        _projectService = projectService;
        _teamService = teamService;
        _activityService = activityService;
    }

    // ================================================================================
    // SSE ENDPOINT - Real-time task updates
    // ================================================================================

    [HttpGet("/tasks/stream")]
    public IActionResult TaskStream()
    {
        return ServerSentEvents(async (conn, ct) =>
        {
            // Subscribe to task board update events
            conn.WithEvents(
                TaskSseEvents.BoardUpdate,
                TaskSseEvents.TodoColumnUpdate,
                TaskSseEvents.InProgressColumnUpdate,
                TaskSseEvents.ReviewColumnUpdate,
                TaskSseEvents.DoneColumnUpdate
            );

            // Keep connection alive with heartbeats
            await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
        });
    }

    // ================================================================================
    // KANBAN BOARD VIEW
    // ================================================================================

    [HttpGet("/tasks")]
    public IActionResult Index()
    {
        var tasks = _taskService.GetAll();
        var projects = _projectService.GetAll();
        var teamMembers = _teamService.GetAll();

        var model = new
        {
            Tasks = tasks,
            Projects = projects,
            TeamMembers = teamMembers
        };

        return SwapView(TaskViews.Index, model);
    }

    // ================================================================================
    // CREATE TASK - Demonstrates payload access in event chains
    // ================================================================================

    [HttpPost("/tasks")]
    public IActionResult Create([FromForm] TaskInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return SwapResponse()
                .WithToast("Task title is required", ToastType.Error)
                .Build();
        }

        var task = _taskService.Create(input, "demo-user");

        // Log activity
        _activityService.LogActivity(
            description: $"Created task: {task.Title}",
            taskId: task.Id,
            projectId: task.ProjectId,
            userId: "demo-user"
        );

        // Trigger event with payload - NEW in 0.5.0
        // Event chain will receive task object and avoid re-fetching
        return SwapResponse()
            .AlsoUpdate(TaskElements.Column(task.Status), TaskViews.TaskColumn, _taskService.GetByStatus(task.Status))
            .WithTrigger(TaskEvents.Created, task) // Pass task as payload!
            .Build();
    }

    // ================================================================================
    // UPDATE TASK STATUS - Demonstrates payload access + deep event chains
    // ================================================================================

    [HttpPatch("/tasks/{id}/status")]
    public IActionResult UpdateStatus(int id, [FromForm] TaskStatus status)
    {
        var task = _taskService.Get(id);
        if (task == null)
        {
            return SwapResponse()
                .WithToast("Task not found", ToastType.Error)
                .Build();
        }

        var oldStatus = task.Status;
        _taskService.UpdateStatus(id, status);
        task = _taskService.Get(id)!; // Re-fetch updated task

        // Log activity
        _activityService.LogActivity(
            description: $"Moved task '{task.Title}' from {oldStatus} to {status}",
            taskId: task.Id,
            projectId: task.ProjectId,
            userId: "demo-user"
        );

        // Multi-column update using OOB helpers - NEW in 0.5.0
        return SwapResponse()
            // Remove from old column
            .AlsoUpdate(TaskElements.Column(oldStatus), TaskViews.TaskColumn, _taskService.GetByStatus(oldStatus))
            // Add to new column (demonstrates AlsoUpdateById)
            .AlsoUpdate(TaskElements.Column(status), TaskViews.TaskColumn, _taskService.GetByStatus(status))
            // Update project progress
            .AlsoUpdate(
                ProjectElements.Progress(task.ProjectId),
                ProjectViews.ProgressBar,
                _projectService.GetProgress(task.ProjectId)
            )
            // Trigger event with payload for cascade
            .WithTrigger(TaskEvents.StatusChanged, task)
            .Build();
    }

    // ================================================================================
    // ASSIGN TASK - Demonstrates warning toasts for overload detection
    // ================================================================================

    [HttpPatch("/tasks/{id}/assign")]
    public IActionResult Assign(int id, [FromForm] string assigneeId)
    {
        var task = _taskService.Get(id);
        if (task == null)
        {
            return SwapResponse()
                .WithToast("Task not found", ToastType.Error)
                .Build();
        }

        // Check if team member is overloaded (>= 10 active tasks)
        var member = _teamService.Get(assigneeId);
        if (member == null)
        {
            return SwapResponse()
                .WithToast("Team member not found", ToastType.Error)
                .Build();
        }

        var activeTaskCount = _teamService.GetActiveTaskCount(assigneeId);
        if (activeTaskCount >= 10)
        {
            // Demonstrates WARNING toast
            return SwapResponse()
                .WithTrigger(TaskEvents.AssignmentFailed)
                .Build();
        }

        _taskService.Assign(id, assigneeId);
        task = _taskService.Get(id)!;

        // Log activity
        _activityService.LogActivity(
            description: $"Assigned '{task.Title}' to {member.Name}",
            taskId: task.Id,
            projectId: task.ProjectId,
            userId: "demo-user"
        );

        // Deep event chain: Task.Assigned → cascades to Notification + Activity
        return SwapResponse()
            .AlsoUpdate(TaskElements.Card(id), TaskViews.TaskCard, task)
            .AlsoUpdate(DashboardElements.TeamList, DashboardViews.TeamList, _teamService.GetAll())
            .WithTrigger(TaskEvents.Assigned, task)
            .Build();
    }

    // ================================================================================
    // DELETE TASK - Demonstrates DELETE swap mode
    // ================================================================================

    [HttpDelete("/tasks/{id}")]
    public IActionResult Delete(int id)
    {
        var task = _taskService.Get(id);
        if (task == null)
        {
            return SwapResponse()
                .WithToast("Task not found", ToastType.Error)
                .Build();
        }

        var projectId = task.ProjectId;
        var status = task.Status;

        _taskService.Delete(id);

        // Log activity
        _activityService.LogActivity(
            description: $"Deleted task: {task.Title}",
            projectId: projectId,
            userId: "demo-user"
        );

        // Demonstrates DELETE swap mode (removes element from DOM)
        return SwapResponse()
            .AlsoUpdate(TaskElements.Card(id), "_Empty", null, SwapMode.Delete)
            .AlsoUpdate(
                ProjectElements.Progress(projectId),
                ProjectViews.ProgressBar,
                _projectService.GetProgress(projectId)
            )
            .WithTrigger(TaskEvents.Deleted)
            .Build();
    }

    // ================================================================================
    // GET TASK DETAILS
    // ================================================================================

    [HttpGet("/tasks/{id}")]
    public IActionResult Details(int id)
    {
        var task = _taskService.Get(id);
        if (task == null)
        {
            return NotFound();
        }

        var project = _projectService.Get(task.ProjectId);
        var assignee = task.AssigneeId != null ? _teamService.Get(task.AssigneeId) : null;

        var model = new
        {
            Task = task,
            Project = project,
            Assignee = assignee
        };

        return PartialView(TaskViews.Details, model);
    }

    // ================================================================================
    // UPDATE PRIORITY
    // ================================================================================

    [HttpPatch("/tasks/{id}/priority")]
    public IActionResult UpdatePriority(int id, [FromForm] TaskPriority priority)
    {
        var task = _taskService.Get(id);
        if (task == null)
        {
            return SwapResponse()
                .WithToast("Task not found", ToastType.Error)
                .Build();
        }

        _taskService.UpdatePriority(id, priority);
        task = _taskService.Get(id)!;

        // Log activity
        _activityService.LogActivity(
            description: $"Changed priority of '{task.Title}' to {priority}",
            taskId: task.Id,
            projectId: task.ProjectId,
            userId: "demo-user"
        );

        return SwapResponse()
            .AlsoUpdate(TaskElements.Card(id), TaskViews.TaskCard, task)
            .WithTrigger(TaskEvents.PriorityChanged, task)
            .WithToast("Priority updated", ToastType.Info)
            .Build();
    }

    // ================================================================================
    // CHECK OVERDUE TASKS - Manual trigger for demonstration
    // ================================================================================

    [HttpPost("/tasks/check-overdue")]
    public IActionResult CheckOverdueTasks()
    {
        var overdueTasks = _taskService.GetOverdue();
        
        if (!overdueTasks.Any())
        {
            return SwapResponse()
                .WithToast("No overdue tasks", ToastType.Info)
                .Build();
        }

        // Trigger overdue warning for first overdue task as demo
        var task = overdueTasks.First();
        
        return SwapResponse()
            .WithTrigger(TaskEvents.Overdue, task)
            .Build();
    }

    // ================================================================================
    // SIMULATE CONFLICT - Demonstrates warning toast for concurrent editing
    // ================================================================================

    [HttpPost("/tasks/{id}/simulate-conflict")]
    public IActionResult SimulateConflict(int id)
    {
        var task = _taskService.Get(id);
        if (task == null)
        {
            return SwapResponse()
                .WithToast("Task not found", ToastType.Error)
                .Build();
        }

        // Trigger conflict detected warning (simulated for demo)
        return SwapResponse()
            .WithTrigger(TaskEvents.ConflictDetected, task)
            .Build();
    }
}

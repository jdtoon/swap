using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
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
        return Sse(stream =>
        {
            // Demonstrates SSE event bridge for real-time updates
            // This endpoint is kept alive and pushes updates when tasks change
            // Note: In production, use proper pub/sub (Redis, SignalR, etc.)
            
            // Heartbeat to keep connection alive
            stream.WriteEvent(new SseEvent
            {
                Data = "connected",
                Event = "heartbeat"
            });

            // Listen for task changes and push updates
            // Actual implementation would subscribe to event bus or message queue
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

        return View(TaskViews.Index, model);
    }

    // ================================================================================
    // CREATE TASK - Demonstrates payload access in event chains
    // ================================================================================

    [HttpPost("/tasks")]
    public IActionResult Create([FromForm] TaskInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return this.Toast("Task title is required", ToastType.Error);
        }

        var task = _taskService.Create(input);

        // Log activity
        _activityService.LogActivity(
            description: $"Created task: {task.Title}",
            taskId: task.Id,
            projectId: task.ProjectId,
            userId: "demo-user"
        );

        // Trigger event with payload - NEW in 0.5.0
        // Event chain will receive task object and avoid re-fetching
        return this.SwapBuilder()
            .RefreshPartial(TaskElements.Column(task.Status), TaskViews.TaskColumn, task.Status)
            .TriggerEvent(TaskEvents.Created, task) // Pass task as payload!
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
            return this.Toast("Task not found", ToastType.Error);
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
        return this.SwapBuilder()
            // Remove from old column
            .RefreshPartial(TaskElements.Column(oldStatus), TaskViews.TaskColumn, oldStatus)
            // Add to new column (demonstrates AlsoUpdateById)
            .AlsoUpdateById(TaskElements.Column(status), TaskViews.TaskColumn, status)
            // Update project progress
            .AlsoUpdateById(
                ProjectElements.Progress(task.ProjectId),
                ProjectViews.ProgressBar,
                _projectService.GetProgress(task.ProjectId)
            )
            // Trigger event with payload for cascade
            .TriggerEvent(TaskEvents.StatusChanged, task)
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
            return this.Toast("Task not found", ToastType.Error);
        }

        // Check if team member is overloaded (>= 10 active tasks)
        var member = _teamService.Get(assigneeId);
        if (member == null)
        {
            return this.Toast("Team member not found", ToastType.Error);
        }

        var activeTaskCount = _teamService.GetActiveTaskCount(assigneeId);
        if (activeTaskCount >= 10)
        {
            // Demonstrates WARNING toast
            return this.SwapBuilder()
                .TriggerEvent(TaskEvents.AssignmentFailed)
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
        return this.SwapBuilder()
            .RefreshPartial(TaskElements.Card(id), TaskViews.TaskCard, task)
            .AlsoUpdateById(DashboardElements.TeamList, DashboardViews.TeamList, _teamService.GetAll())
            .TriggerEvent(TaskEvents.Assigned, task)
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
            return this.Toast("Task not found", ToastType.Error);
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
        return this.SwapBuilder()
            .DeleteElement(TaskElements.Card(id))
            .AlsoUpdateById(
                ProjectElements.Progress(projectId),
                ProjectViews.ProgressBar,
                _projectService.GetProgress(projectId)
            )
            .TriggerEvent(TaskEvents.Deleted)
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
            return this.Toast("Task not found", ToastType.Error);
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

        return this.SwapBuilder()
            .RefreshPartial(TaskElements.Card(id), TaskViews.TaskCard, task)
            .TriggerEvent(TaskEvents.PriorityChanged, task)
            .Toast("Priority updated", ToastType.Info)
            .Build();
    }
}

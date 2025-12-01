using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Models;
using Swap.Htmx.State;
using SwapDashboard.Events;
using SwapDashboard.Handlers;
using SwapDashboard.Models;
using SwapDashboard.Services;
using TaskStatus = SwapDashboard.Models.TaskStatus;

namespace SwapDashboard.Controllers;

/// <summary>
/// Dashboard controller demonstrating complex front-end orchestration.
/// 
/// KEY PRINCIPLE: The controller just does business logic and fires events.
/// Handlers (in DashboardHandlers.cs) decide what partials to update.
/// This means adding a new UI component that reacts to task events
/// is just adding a new handler class - no controller changes needed.
/// </summary>
public class DashboardController : SwapController
{
    private readonly IProjectService _projects;
    private readonly ITaskService _tasks;
    private readonly ITeamService _team;
    private readonly IActivityService _activities;
    private readonly INotificationService _notifications;

    public DashboardController(
        IProjectService projects,
        ITaskService tasks,
        ITeamService team,
        IActivityService activities,
        INotificationService notifications)
    {
        _projects = projects;
        _tasks = tasks;
        _team = team;
        _activities = activities;
        _notifications = notifications;
    }

    /// <summary>
    /// Main dashboard page - loads all components.
    /// </summary>
    public IActionResult Index([FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        var vm = BuildViewModel(state);
        return SwapView(vm);
    }

    /// <summary>
    /// Create a new task.
    /// Fires TaskCreatedEvent - handlers update all affected partials.
    /// </summary>
    [HttpPost]
    public IActionResult CreateTask(
        int projectId, 
        string title, 
        string? description, 
        TaskPriority priority, 
        int? assigneeId,
        [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var task = _tasks.Create(projectId, title, description ?? "", priority, assigneeId);
        
        _activities.Add("task_created", "created task", null, "You", task.Id, task.Title, projectId);
        _notifications.Add("Task created", $"New task: {task.Title}", "success");

        // Fire the SPECIFIC event - EventKey + payload
        // DashboardEvents.Task.Created is generated from [SwapEventSource]
        return SwapEvent(DashboardEvents.Task.Created, new TaskCreatedEvent(task.Id, projectId, task.Title))
            .WithCreatedToast("Task", task.Title)
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Complete a task.
    /// This single action triggers updates to:
    /// - Stats panel (via StatsHandler)
    /// - Project list (via ProjectListHandler)
    /// - Kanban board (via KanbanHandler)
    /// - Activity feed (via ActivityFeedHandler)
    /// - Task detail (via TaskDetailHandler)
    /// - Progress bar (via ProgressBarHandler)
    /// - Overdue widget (via OverdueHandler)
    /// - Task counter (via TaskCounterHandler)
    /// - Notification badge (via NotificationBadgeHandler)
    /// - Team workload (via TeamWorkloadHandler)
    /// ALL from one event - the controller doesn't know or care about any of these!
    /// </summary>
    [HttpPost]
    public IActionResult CompleteTask(int id, [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var task = _tasks.GetById(id);
        if (task == null) return NotFound();

        _tasks.UpdateStatus(id, TaskStatus.Done);
        
        _activities.Add("task_completed", "completed task", null, "You", task.Id, task.Title, task.ProjectId);
        _notifications.Add("Task completed", $"Completed: {task.Title}", "success");

        // Single event → 10+ handlers → 10+ partial updates → 1 HTTP response
        return SwapEvent(DashboardEvents.Task.Completed, new TaskCompletedEvent(task.Id, task.ProjectId, task.Title))
            .WithSuccessToast($"Completed: {task.Title}")
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Move task to a different status (kanban drag-drop).
    /// </summary>
    [HttpPost]
    public IActionResult MoveTask(int id, TaskStatus status, [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var task = _tasks.GetById(id);
        if (task == null) return NotFound();

        _tasks.UpdateStatus(id, status);
        _activities.Add("task_status", $"moved to {status}", null, "You", task.Id, task.Title, task.ProjectId);

        return SwapEvent(DashboardEvents.Task.Moved, new TaskMovedEvent(task.Id, task.ProjectId, task.Title))
            .WithInfoToast($"Moved to {status}")
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Assign task to team member.
    /// </summary>
    [HttpPost]
    public IActionResult AssignTask(int id, int? assigneeId, [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var task = _tasks.GetById(id);
        if (task == null) return NotFound();

        _tasks.Assign(id, assigneeId);
        var assignee = assigneeId.HasValue ? _team.GetById(assigneeId.Value) : null;
        
        _activities.Add("task_assigned", $"assigned to {assignee?.Name ?? "unassigned"}", null, "You", task.Id, task.Title, task.ProjectId);
        if (assignee != null)
        {
            _notifications.Add("Task assigned", $"{task.Title} assigned to {assignee.Name}", "info");
        }

        return SwapEvent(DashboardEvents.Task.Assigned, new TaskAssignedEvent(task.Id, task.ProjectId, task.Title, assigneeId))
            .WithInfoToast(assignee != null ? $"Assigned to {assignee.Name}" : "Unassigned")
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Delete a task.
    /// </summary>
    [HttpPost]
    public IActionResult DeleteTask(int id, [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var task = _tasks.GetById(id);
        if (task == null) return NotFound();

        var projectId = task.ProjectId;
        var title = task.Title;
        
        _tasks.Delete(id);
        _activities.Add("task_deleted", "deleted task", null, "You", null, title, projectId);

        return SwapEvent(DashboardEvents.Task.Deleted, new TaskDeletedEvent(id, projectId, title))
            .WithDeletedToast("Task", title)
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Select a project - updates the main content area.
    /// </summary>
    [HttpGet]
    public IActionResult SelectProject(int id, [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        state.SelectedProjectId = id;
        
        var project = _projects.GetById(id);
        var tasks = _tasks.GetByProject(id);
        var kanban = _tasks.GetKanban(id);
        var stats = _tasks.GetStats(id);

        // Build proper view model for _MainContent
        var vm = new DashboardViewModel
        {
            State = state,
            Projects = _projects.GetAll(),
            SelectedProject = project,
            Tasks = tasks,
            TeamMembers = _team.GetAll(),
            Activities = _activities.GetRecent(10),
            Notifications = _notifications.GetAll(),
            Stats = stats,
            UnreadNotificationCount = _notifications.GetUnreadCount()
        };

        return SwapResponse()
            .WithView("_MainContent", vm)
            .WithState(state)
            .AlsoUpdate("project-header", "_ProjectHeader", project)
            .AlsoUpdate("stats-panel", "_StatsPanel", stats)
            .AlsoUpdate("kanban-todo", "_KanbanColumn", new KanbanColumnModel("To Do", "todo", kanban.TodoTasks, "bg-gray-100"))
            .AlsoUpdate("kanban-inprogress", "_KanbanColumn", new KanbanColumnModel("In Progress", "inprogress", kanban.InProgressTasks, "bg-blue-100"))
            .AlsoUpdate("kanban-review", "_KanbanColumn", new KanbanColumnModel("Review", "review", kanban.ReviewTasks, "bg-yellow-100"))
            .AlsoUpdate("kanban-done", "_KanbanColumn", new KanbanColumnModel("Done", "done", kanban.DoneTasks, "bg-green-100"))
            .AlsoUpdate("progress-bar", "_ProgressBar", stats)
            .WithTrigger(DashboardEvents.Project.Selected, new { ProjectId = id })
            .Build();
    }

    /// <summary>
    /// Load task detail panel.
    /// </summary>
    [HttpGet]
    public IActionResult TaskDetail(int id)
    {
        var task = _tasks.GetById(id);
        if (task == null) return NotFound();

        return SwapResponse()
            .WithView("_TaskDetailPanel", task)
            .Build();
    }

    /// <summary>
    /// Show the create task modal.
    /// </summary>
    [HttpGet]
    public IActionResult CreateTaskModal([FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var vm = new CreateTaskViewModel
        {
            ProjectId = state.SelectedProjectId ?? _projects.GetAll().FirstOrDefault()?.Id ?? 0,
            Projects = _projects.GetAll(),
            TeamMembers = _team.GetAll()
        };
        
        return SwapResponse()
            .WithView("_CreateTaskModal", vm)
            .Build();
    }

    /// <summary>
    /// Filter tasks by status/priority/assignee.
    /// State is read from hidden fields, filter values from dropdowns.
    /// </summary>
    [HttpGet]
    public IActionResult FilterTasks(
        string? StatusFilter,
        string? PriorityFilter,
        int? SelectedMemberId,
        [FromSwapState] DashboardState state)
    {
        // Update state with filter values
        if (!string.IsNullOrEmpty(StatusFilter))
            state.StatusFilter = StatusFilter;
        if (!string.IsNullOrEmpty(PriorityFilter))
            state.PriorityFilter = PriorityFilter;
        if (SelectedMemberId.HasValue)
            state.SelectedMemberId = SelectedMemberId;
        
        var tasks = _tasks.GetFiltered(
            state.SelectedProjectId, 
            state.StatusFilter, 
            state.PriorityFilter, 
            state.SelectedMemberId,
            state.SearchTerm);
        
        var kanban = new KanbanViewModel
        {
            TodoTasks = tasks.Where(t => t.Status == TaskStatus.Todo).ToList(),
            InProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            ReviewTasks = tasks.Where(t => t.Status == TaskStatus.Review).ToList(),
            DoneTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList()
        };

        return SwapResponse()
            .WithView("_KanbanBoard", kanban)
            .AlsoUpdate("filter-results-count", "_FilterResultsCount", tasks.Count)
            .WithState(state)
            .WithTrigger(DashboardEvents.Filter.Changed)
            .Build();
    }

    /// <summary>
    /// Search tasks.
    /// </summary>
    [HttpGet]
    public IActionResult Search(string q, [FromSwapState] DashboardState state)
    {
        state.SearchTerm = q;
        var tasks = _tasks.GetFiltered(
            state.SelectedProjectId,
            state.StatusFilter,
            state.PriorityFilter,
            state.SelectedMemberId,
            q);

        var kanban = new KanbanViewModel
        {
            TodoTasks = tasks.Where(t => t.Status == TaskStatus.Todo).ToList(),
            InProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            ReviewTasks = tasks.Where(t => t.Status == TaskStatus.Review).ToList(),
            DoneTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList()
        };

        return SwapResponse()
            .WithView("_KanbanBoard", kanban)
            .WithState(state)
            .AlsoUpdate("filter-results-count", "_FilterResultsCount", tasks.Count)
            .Build();
    }

    /// <summary>
    /// Load more activities (infinite scroll).
    /// </summary>
    [HttpGet]
    public IActionResult LoadMoreActivities(int skip = 0)
    {
        var activities = _activities.GetRecent(20).Skip(skip).Take(5).ToList();
        return SwapResponse()
            .WithView("_ActivityItems", activities)
            .Build();
    }

    /// <summary>
    /// Get notifications.
    /// </summary>
    [HttpGet]
    public IActionResult Notifications()
    {
        var notifications = _notifications.GetAll();
        return SwapResponse()
            .WithView("_NotificationList", notifications)
            .Build();
    }

    /// <summary>
    /// Mark all notifications as read.
    /// </summary>
    [HttpPost]
    public IActionResult MarkAllRead()
    {
        _notifications.MarkAllAsRead();
        return SwapResponse()
            .AlsoUpdate("notification-badge", "_NotificationBadge", 0)
            .AlsoUpdate("notification-list", "_NotificationList", _notifications.GetAll())
            .WithSuccessToast("Marked all as read")
            .Build();
    }

    /// <summary>
    /// Add comment to task.
    /// </summary>
    [HttpPost]
    public IActionResult AddComment(int taskId, string content, [FromSwapState] DashboardState? state = null)
    {
        state ??= new DashboardState();
        
        var task = _tasks.GetById(taskId);
        if (task == null) return NotFound();

        _activities.Add("comment", "commented on", null, "You", taskId, task.Title, task.ProjectId);

        return SwapEvent(DashboardEvents.Comment.Added, new CommentAddedEvent(taskId, content))
            .WithSuccessToast("Comment added")
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Refresh stats panel only.
    /// </summary>
    [HttpGet]
    public IActionResult RefreshStats(int? projectId)
    {
        var stats = _tasks.GetStats(projectId);
        return SwapResponse()
            .WithView("_StatsPanel", stats)
            .Build();
    }

    /// <summary>
    /// Switch view mode (board/list/timeline).
    /// </summary>
    [HttpGet]
    public IActionResult SwitchView(string mode, [FromSwapState] DashboardState state)
    {
        state.ViewMode = mode;
        var tasks = _tasks.GetFiltered(
            state.SelectedProjectId,
            state.StatusFilter,
            state.PriorityFilter,
            state.SelectedMemberId,
            state.SearchTerm);

        var viewName = mode switch
        {
            "list" => "_ListView",
            "timeline" => "_TimelineView",
            _ => "_BoardView"
        };

        return SwapResponse()
            .WithView(viewName, tasks)
            .WithState(state)
            .WithTrigger(DashboardEvents.View.Mode.Changed, new { Mode = mode })
            .Build();
    }

    private DashboardViewModel BuildViewModel(DashboardState state)
    {
        var projects = _projects.GetAll();
        var selectedProject = state.SelectedProjectId.HasValue 
            ? _projects.GetById(state.SelectedProjectId.Value) 
            : projects.FirstOrDefault();
        
        state.SelectedProjectId ??= selectedProject?.Id;

        var tasks = state.SelectedProjectId.HasValue 
            ? _tasks.GetByProject(state.SelectedProjectId.Value) 
            : _tasks.GetAll();

        return new DashboardViewModel
        {
            State = state,
            Projects = projects,
            SelectedProject = selectedProject,
            Tasks = tasks,
            SelectedTask = state.SelectedTaskId.HasValue ? _tasks.GetById(state.SelectedTaskId.Value) : null,
            TeamMembers = _team.GetAll(),
            SelectedMember = state.SelectedMemberId.HasValue ? _team.GetById(state.SelectedMemberId.Value) : null,
            Activities = _activities.GetRecent(10),
            Notifications = _notifications.GetAll(),
            Stats = _tasks.GetStats(state.SelectedProjectId),
            UnreadNotificationCount = _notifications.GetUnreadCount()
        };
    }
}

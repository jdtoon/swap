using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapDashboard.Events;
using SwapDashboard.Models;
using SwapDashboard.Services;

namespace SwapDashboard.Handlers;

/// <summary>
/// HANDLER 1: Updates the stats panel when tasks change.
/// This handler runs for ANY task event (created, completed, deleted).
/// </summary>
[SwapHandler]
public class StatsHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITaskService _tasks;

    public StatsHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var stats = _tasks.GetStats(e.ProjectId);
        builder.AlsoUpdate("stats-panel", "_StatsPanel", stats);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 2: Updates the project sidebar when tasks change.
/// Projects show task counts, so any task change affects them.
/// </summary>
[SwapHandler]
public class ProjectListHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly IProjectService _projects;

    public ProjectListHandler(IProjectService projects) => _projects = projects;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var projects = _projects.GetAll();
        builder.AlsoUpdate("project-list", "_ProjectList", projects);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 3: Updates the kanban board columns when tasks change.
/// </summary>
[SwapHandler]
public class KanbanHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITaskService _tasks;

    public KanbanHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var kanban = _tasks.GetKanban(e.ProjectId);
        builder.AlsoUpdate("kanban-todo", "_KanbanColumn", 
            new KanbanColumnModel("Todo", kanban.TodoTasks, "bg-gray-100"));
        builder.AlsoUpdate("kanban-inprogress", "_KanbanColumn", 
            new KanbanColumnModel("In Progress", kanban.InProgressTasks, "bg-blue-100"));
        builder.AlsoUpdate("kanban-review", "_KanbanColumn", 
            new KanbanColumnModel("Review", kanban.ReviewTasks, "bg-yellow-100"));
        builder.AlsoUpdate("kanban-done", "_KanbanColumn", 
            new KanbanColumnModel("Done", kanban.DoneTasks, "bg-green-100"));
        return Task.CompletedTask;
    }
}

public record KanbanColumnModel(string Title, List<Models.TaskItem> Tasks, string ColorClass);

/// <summary>
/// HANDLER 4: Updates the activity feed when tasks change.
/// </summary>
[SwapHandler]
public class ActivityFeedHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly IActivityService _activities;

    public ActivityFeedHandler(IActivityService activities) => _activities = activities;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var activities = _activities.GetRecent(10);
        builder.AlsoUpdate("activity-feed", "_ActivityFeed", activities);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 5: Updates the task detail panel when a specific task changes.
/// Only updates if the task detail panel is showing the changed task.
/// </summary>
[SwapHandler]
public class TaskDetailHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITaskService _tasks;

    public TaskDetailHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var task = _tasks.GetById(e.TaskId);
        if (task != null)
        {
            builder.AlsoUpdateIfExists($"task-detail-{e.TaskId}", "_TaskDetailPanel", task);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 6: Updates team member workload when tasks are assigned.
/// </summary>
[SwapHandler]
public class TeamWorkloadHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITeamService _team;

    public TeamWorkloadHandler(ITeamService team) => _team = team;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var members = _team.GetAll();
        builder.AlsoUpdate("team-panel", "_TeamPanel", members);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 7: Updates the progress bar when tasks complete.
/// </summary>
[SwapHandler]
public class ProgressBarHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITaskService _tasks;

    public ProgressBarHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var stats = _tasks.GetStats(e.ProjectId);
        builder.AlsoUpdate("progress-bar", "_ProgressBar", stats);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 8: Updates notification badge when things happen.
/// </summary>
[SwapHandler]
public class NotificationBadgeHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly INotificationService _notifications;

    public NotificationBadgeHandler(INotificationService notifications) => _notifications = notifications;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var count = _notifications.GetUnreadCount();
        builder.AlsoUpdate("notification-badge", "_NotificationBadge", count);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 9: Updates the overdue tasks widget.
/// </summary>
[SwapHandler]
public class OverdueHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITaskService _tasks;

    public OverdueHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var stats = _tasks.GetStats(e.ProjectId);
        builder.AlsoUpdate("overdue-widget", "_OverdueWidget", stats.OverdueTasks);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 10: Updates the task counter in the header.
/// </summary>
[SwapHandler]
public class TaskCounterHandler : 
    ISwapEventHandler<TaskEvent>
{
    private readonly ITaskService _tasks;

    public TaskCounterHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var stats = _tasks.GetStats(e.ProjectId);
        builder.AlsoUpdate("task-counter", "_TaskCounter", stats);
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 11: Handles comment added events - updates comment list.
/// </summary>
[SwapHandler]
public class CommentListHandler : 
    ISwapEventHandler<CommentAddedEvent>
{
    private readonly ITaskService _tasks;

    public CommentListHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(CommentAddedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var task = _tasks.GetById(e.TaskId);
        if (task != null)
        {
            builder.AlsoUpdateIfExists($"task-comments-{e.TaskId}", "_CommentList", task);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// HANDLER 12: Updates the filter results count.
/// </summary>
[SwapHandler]
public class FilterResultsHandler : 
    ISwapEventHandler<FilterChangedEvent>
{
    private readonly ITaskService _tasks;

    public FilterResultsHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(FilterChangedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var tasks = _tasks.GetFiltered(null, e.Status, e.Priority, e.AssigneeId, null);
        builder.AlsoUpdate("filter-results-count", "_FilterResultsCount", tasks.Count);
        return Task.CompletedTask;
    }
}

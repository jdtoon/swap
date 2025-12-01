using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapDashboard.Events;
using SwapDashboard.Models;
using SwapDashboard.Services;

namespace SwapDashboard.Handlers;

// ============================================================================
// KANBAN COLUMN MODEL
// ============================================================================

/// <summary>
/// Model for rendering a single kanban column.
/// </summary>
public record KanbanColumnModel(string Title, string Status, List<TaskItem> Tasks, string ColorClass);

// ============================================================================
// HANDLER 1: StatsHandler
// Updates the stats panel showing task counts.
// Responds to: Task Created, Completed, Deleted, Moved
// ============================================================================

[SwapHandler]
public class StatsHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>,
    ISwapEventHandler<TaskMovedEvent>
{
    private readonly ITaskService _tasks;

    public StatsHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateStats(e.ProjectId, builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateStats(e.ProjectId, builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateStats(e.ProjectId, builder);

    public Task HandleAsync(TaskMovedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateStats(e.ProjectId, builder);

    private Task UpdateStats(int? projectId, SwapResponseBuilder builder)
    {
        var stats = _tasks.GetStats(projectId);
        builder.AlsoUpdate("stats-panel", "_StatsPanel", stats);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 2: ProjectListHandler
// Updates the project sidebar with task counts.
// Responds to: Task Created, Completed, Deleted
// ============================================================================

[SwapHandler]
public class ProjectListHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>
{
    private readonly IProjectService _projects;
    private readonly ITaskService _tasks;

    public ProjectListHandler(IProjectService projects, ITaskService tasks)
    {
        _projects = projects;
        _tasks = tasks;
    }

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateProjectList(builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateProjectList(builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateProjectList(builder);

    private Task UpdateProjectList(SwapResponseBuilder builder)
    {
        var projects = _projects.GetAll();
        // Update task counts for each project
        foreach (var project in projects)
        {
            var tasks = _tasks.GetByProject(project.Id);
            project.TaskCount = tasks.Count;
            project.CompletedCount = tasks.Count(t => t.Status == Models.TaskStatus.Done);
        }
        builder.AlsoUpdate("project-list", "_ProjectList", projects);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 3: KanbanHandler
// Updates all kanban columns when tasks change.
// Responds to: Task Created, Completed, Deleted, Moved
// ============================================================================

[SwapHandler]
public class KanbanHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>,
    ISwapEventHandler<TaskMovedEvent>,
    ISwapEventHandler<TaskAssignedEvent>
{
    private readonly ITaskService _tasks;

    public KanbanHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateKanban(e.ProjectId, builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateKanban(e.ProjectId, builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateKanban(e.ProjectId, builder);

    public Task HandleAsync(TaskMovedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateKanban(e.ProjectId, builder);

    public Task HandleAsync(TaskAssignedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateKanban(e.ProjectId, builder);

    private Task UpdateKanban(int? projectId, SwapResponseBuilder builder)
    {
        var kanban = _tasks.GetKanban(projectId);
        builder.AlsoUpdate("kanban-todo", "_KanbanColumn", 
            new KanbanColumnModel("To Do", "todo", kanban.TodoTasks, "bg-gray-100"));
        builder.AlsoUpdate("kanban-inprogress", "_KanbanColumn", 
            new KanbanColumnModel("In Progress", "inprogress", kanban.InProgressTasks, "bg-blue-100"));
        builder.AlsoUpdate("kanban-review", "_KanbanColumn", 
            new KanbanColumnModel("Review", "review", kanban.ReviewTasks, "bg-yellow-100"));
        builder.AlsoUpdate("kanban-done", "_KanbanColumn", 
            new KanbanColumnModel("Done", "done", kanban.DoneTasks, "bg-green-100"));
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 4: ActivityFeedHandler
// Updates the activity feed in the right sidebar.
// Responds to: All task events
// ============================================================================

[SwapHandler]
public class ActivityFeedHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>,
    ISwapEventHandler<TaskMovedEvent>,
    ISwapEventHandler<TaskAssignedEvent>,
    ISwapEventHandler<CommentAddedEvent>
{
    private readonly IActivityService _activities;

    public ActivityFeedHandler(IActivityService activities) => _activities = activities;

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateActivityFeed(builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateActivityFeed(builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateActivityFeed(builder);

    public Task HandleAsync(TaskMovedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateActivityFeed(builder);

    public Task HandleAsync(TaskAssignedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateActivityFeed(builder);

    public Task HandleAsync(CommentAddedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateActivityFeed(builder);

    private Task UpdateActivityFeed(SwapResponseBuilder builder)
    {
        var activities = _activities.GetRecent(10);
        builder.AlsoUpdate("activity-feed", "_ActivityFeed", activities);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 5: TaskDetailHandler
// Updates the task detail panel if it's showing the changed task.
// Responds to: Task Completed, Moved, Assigned
// ============================================================================

[SwapHandler]
public class TaskDetailHandler : 
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskMovedEvent>,
    ISwapEventHandler<TaskAssignedEvent>
{
    private readonly ITaskService _tasks;

    public TaskDetailHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTaskDetail(e.TaskId, builder);

    public Task HandleAsync(TaskMovedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTaskDetail(e.TaskId, builder);

    public Task HandleAsync(TaskAssignedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTaskDetail(e.TaskId, builder);

    private Task UpdateTaskDetail(int taskId, SwapResponseBuilder builder)
    {
        var task = _tasks.GetById(taskId);
        if (task != null)
        {
            // Only update if the task detail panel is showing this specific task
            builder.AlsoUpdateIfExists($"task-detail-{taskId}", "_TaskDetail", task);
        }
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 6: TeamWorkloadHandler
// Updates team member workload display.
// Responds to: Task Completed, Assigned
// ============================================================================

[SwapHandler]
public class TeamWorkloadHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>,
    ISwapEventHandler<TaskAssignedEvent>
{
    private readonly ITeamService _team;
    private readonly ITaskService _tasks;

    public TeamWorkloadHandler(ITeamService team, ITaskService tasks)
    {
        _team = team;
        _tasks = tasks;
    }

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTeamPanel(builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTeamPanel(builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTeamPanel(builder);

    public Task HandleAsync(TaskAssignedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateTeamPanel(builder);

    private Task UpdateTeamPanel(SwapResponseBuilder builder)
    {
        var members = _team.GetAll();
        var allTasks = _tasks.GetAll();
        
        // Update active task counts for each member
        foreach (var member in members)
        {
            member.ActiveTaskCount = allTasks.Count(t => 
                t.AssigneeId == member.Id && 
                t.Status != Models.TaskStatus.Done);
        }
        
        builder.AlsoUpdate("team-panel", "_TeamPanel", members);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 7: ProgressBarHandler
// Updates the progress bar showing project completion.
// Responds to: Task Created, Completed, Deleted
// ============================================================================

[SwapHandler]
public class ProgressBarHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>
{
    private readonly ITaskService _tasks;

    public ProgressBarHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateProgressBar(e.ProjectId, builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateProgressBar(e.ProjectId, builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateProgressBar(e.ProjectId, builder);

    private Task UpdateProgressBar(int? projectId, SwapResponseBuilder builder)
    {
        var stats = _tasks.GetStats(projectId);
        builder.AlsoUpdate("progress-bar", "_ProgressBar", stats);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 8: NotificationBadgeHandler
// Updates the notification badge count in the header.
// Responds to: Task Created, Completed, Assigned
// ============================================================================

[SwapHandler]
public class NotificationBadgeHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskAssignedEvent>
{
    private readonly INotificationService _notifications;

    public NotificationBadgeHandler(INotificationService notifications) => _notifications = notifications;

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateBadge(builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateBadge(builder);

    public Task HandleAsync(TaskAssignedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateBadge(builder);

    private Task UpdateBadge(SwapResponseBuilder builder)
    {
        var count = _notifications.GetUnreadCount();
        builder.AlsoUpdate("notification-badge", "_NotificationBadge", count);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 9: OverdueHandler
// Updates the overdue tasks widget.
// Responds to: Task Completed, Deleted (these can change overdue count)
// ============================================================================

[SwapHandler]
public class OverdueHandler : 
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>
{
    private readonly ITaskService _tasks;

    public OverdueHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateOverdue(e.ProjectId, builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateOverdue(e.ProjectId, builder);

    private Task UpdateOverdue(int? projectId, SwapResponseBuilder builder)
    {
        var stats = _tasks.GetStats(projectId);
        builder.AlsoUpdate("overdue-widget", "_OverdueWidget", stats.OverdueTasks);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 10: TaskCounterHandler
// Updates the task counter in the header.
// Responds to: Task Created, Completed, Deleted
// ============================================================================

[SwapHandler]
public class TaskCounterHandler : 
    ISwapEventHandler<TaskCreatedEvent>,
    ISwapEventHandler<TaskCompletedEvent>,
    ISwapEventHandler<TaskDeletedEvent>
{
    private readonly ITaskService _tasks;

    public TaskCounterHandler(ITaskService tasks) => _tasks = tasks;

    public Task HandleAsync(TaskCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateCounter(e.ProjectId, builder);

    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateCounter(e.ProjectId, builder);

    public Task HandleAsync(TaskDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
        => UpdateCounter(e.ProjectId, builder);

    private Task UpdateCounter(int? projectId, SwapResponseBuilder builder)
    {
        var stats = _tasks.GetStats(projectId);
        builder.AlsoUpdate("task-counter", "_TaskCounter", stats);
        return Task.CompletedTask;
    }
}

// ============================================================================
// HANDLER 11: CommentListHandler
// Updates the comment list when comments are added.
// Responds to: Comment Added
// ============================================================================

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
            // Update comment count on the task
            task.CommentCount++;
            builder.AlsoUpdateIfExists($"task-comments-{e.TaskId}", "_CommentList", task);
        }
        return Task.CompletedTask;
    }
}

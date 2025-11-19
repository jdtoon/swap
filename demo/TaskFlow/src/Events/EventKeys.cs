using Swap.Htmx.Events;

namespace TaskFlow.Events;

/// <summary>
/// Task lifecycle events
/// </summary>
public static class TaskEvents
{
    public static readonly EventKey Created = new("task.created");
    public static readonly EventKey Updated = new("task.updated");
    public static readonly EventKey Deleted = new("task.deleted");
    public static readonly EventKey StatusChanged = new("task.statusChanged");
    public static readonly EventKey PriorityChanged = new("task.priorityChanged");
    public static readonly EventKey Assigned = new("task.assigned");
    public static readonly EventKey Unassigned = new("task.unassigned");
    public static readonly EventKey DueDateSet = new("task.dueDateSet");
    public static readonly EventKey Overdue = new("task.overdue");
    public static readonly EventKey Completed = new("task.completed");
    
    // Failure events
    public static readonly EventKey CreateFailed = new("task.createFailed");
    public static readonly EventKey AssignmentFailed = new("task.assignmentFailed");
    public static readonly EventKey ConflictDetected = new("task.conflictDetected");
}

/// <summary>
/// Project events
/// </summary>
public static class ProjectEvents
{
    public static readonly EventKey Created = new("project.created");
    public static readonly EventKey Updated = new("project.updated");
    public static readonly EventKey Deleted = new("project.deleted");
    public static readonly EventKey ProgressChanged = new("project.progressChanged");
}

/// <summary>
/// Comment events
/// </summary>
public static class CommentEvents
{
    public static readonly EventKey Added = new("comment.added");
    public static readonly EventKey Edited = new("comment.edited");
    public static readonly EventKey Deleted = new("comment.deleted");
}

/// <summary>
/// Notification events
/// </summary>
public static class NotificationEvents
{
    public static readonly EventKey Created = new("notification.created");
    public static readonly EventKey Read = new("notification.read");
    public static readonly EventKey Cleared = new("notification.cleared");
    public static readonly EventKey TaskAssigned = new("notification.taskAssigned");
    public static readonly EventKey DeadlineApproaching = new("notification.deadlineApproaching");
    public static readonly EventKey Mentioned = new("notification.mentioned");
}

/// <summary>
/// Activity events
/// </summary>
public static class ActivityEvents
{
    public static readonly EventKey Logged = new("activity.logged");
}

/// <summary>
/// Team presence events (for SSE)
/// </summary>
public static class PresenceEvents
{
    public static readonly EventKey UserOnline = new("presence.userOnline");
    public static readonly EventKey UserOffline = new("presence.userOffline");
    public static readonly EventKey ViewingTask = new("presence.viewingTask");
}

/// <summary>
/// SSE event names for dashboard updates
/// </summary>
public static class DashboardSseEvents
{
    public const string StatsUpdate = "stats-update";
    public const string ActivityUpdate = "activity-update";
    public const string TeamUpdate = "team-update";
}

/// <summary>
/// SSE event names for project updates
/// </summary>
public static class ProjectSseEvents
{
    public const string ListUpdate = "project-list-update";
    public const string ProgressUpdate = "project-progress-update";
}

/// <summary>
/// SSE event names for task board updates
/// </summary>
public static class TaskSseEvents
{
    public const string BoardUpdate = "task-board-update";
    public const string ColumnUpdate = "task-column-update";
    public const string TodoColumnUpdate = "task-column-todo-update";
    public const string InProgressColumnUpdate = "task-column-inprogress-update";
    public const string ReviewColumnUpdate = "task-column-review-update";
    public const string DoneColumnUpdate = "task-column-done-update";
}

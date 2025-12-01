using Swap.Htmx.Attributes;

namespace SwapDashboard.Events;

/// <summary>
/// Event keys for the dashboard demo.
/// Demonstrates how a complex UI with 20+ partials can coordinate through events.
/// 
/// The [SwapEventSource] attribute generates EventKey properties from the const values.
/// "task.created" becomes DashboardEvents.Task.Created
/// </summary>
[SwapEventSource]
public partial class DashboardEvents
{
    // Task events
    public const string TaskCreated = "task.created";
    public const string TaskUpdated = "task.updated";
    public const string TaskCompleted = "task.completed";
    public const string TaskDeleted = "task.deleted";
    public const string TaskAssigned = "task.assigned";
    public const string TaskPriorityChanged = "task.priority.changed";
    public const string TaskMoved = "task.moved";
    
    // Project events
    public const string ProjectSelected = "project.selected";
    public const string ProjectUpdated = "project.updated";
    
    // Filter/UI events
    public const string FilterChanged = "filter.changed";
    public const string ViewModeChanged = "view.mode.changed";
    
    // Team events
    public const string TeamMemberSelected = "team.member.selected";
    
    // Comment events
    public const string CommentAdded = "comment.added";
}

// ============================================================================
// SPECIFIC EVENT TYPES
// ============================================================================
// Using specific event types allows handlers to subscribe to exactly what they need.
// This is more efficient than one generic TaskEvent where every handler fires.
// ============================================================================

/// <summary>
/// Base record for all task events with common properties.
/// </summary>
public abstract record TaskEventBase(int TaskId, int? ProjectId, string? Title);

/// <summary>
/// Fired when a new task is created.
/// Handlers: Stats, ProjectList, Kanban, Activity, TaskCounter, ProgressBar
/// </summary>
public record TaskCreatedEvent(int TaskId, int? ProjectId, string? Title) 
    : TaskEventBase(TaskId, ProjectId, Title);

/// <summary>
/// Fired when a task is marked as completed.
/// Handlers: Stats, ProjectList, Kanban, Activity, TaskDetail, ProgressBar, 
///           Overdue, TaskCounter, NotificationBadge, TeamWorkload
/// </summary>
public record TaskCompletedEvent(int TaskId, int? ProjectId, string? Title) 
    : TaskEventBase(TaskId, ProjectId, Title);

/// <summary>
/// Fired when a task is deleted.
/// Handlers: Stats, ProjectList, Kanban, Activity, TaskCounter, ProgressBar, Overdue
/// </summary>
public record TaskDeletedEvent(int TaskId, int? ProjectId, string? Title) 
    : TaskEventBase(TaskId, ProjectId, Title);

/// <summary>
/// Fired when a task is moved to a different status (kanban drag-drop).
/// Handlers: Stats, Kanban, Activity, TaskDetail, ProgressBar
/// </summary>
public record TaskMovedEvent(int TaskId, int? ProjectId, string? Title) 
    : TaskEventBase(TaskId, ProjectId, Title);

/// <summary>
/// Fired when a task is assigned to a team member.
/// Handlers: Kanban, Activity, TaskDetail, TeamWorkload, NotificationBadge
/// </summary>
public record TaskAssignedEvent(int TaskId, int? ProjectId, string? Title, int? AssigneeId) 
    : TaskEventBase(TaskId, ProjectId, Title);

/// <summary>
/// Event payload for project selection.
/// </summary>
public record ProjectSelectedEvent(int ProjectId);

/// <summary>
/// Event payload for filter changes.
/// </summary>
public record FilterChangedEvent(string? Status, string? Priority, int? AssigneeId);

/// <summary>
/// Event payload for team member selection.
/// </summary>
public record TeamMemberSelectedEvent(int MemberId);

/// <summary>
/// Event payload for comments.
/// </summary>
public record CommentAddedEvent(int TaskId, string Content);

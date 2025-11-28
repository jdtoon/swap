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
public static partial class DashboardEvents
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

/// <summary>
/// Event payload for task-related events.
/// </summary>
public record TaskEvent(int TaskId, int? ProjectId = null, string? Title = null);

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

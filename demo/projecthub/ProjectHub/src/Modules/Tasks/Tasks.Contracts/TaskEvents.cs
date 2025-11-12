using Swap.Htmx.Events;

namespace ProjectHub.Modules.Tasks.Contracts;

/// <summary>
/// Task module events for server-to-client communication.
/// </summary>
public static class TaskEvents
{
    /// <summary>
    /// Task was created
    /// </summary>
    public static readonly EventKey Created = new("task.created");
    
    /// <summary>
    /// Task was updated
    /// </summary>
    public static readonly EventKey Updated = new("task.updated");
    
    /// <summary>
    /// Task was deleted
    /// </summary>
    public static readonly EventKey Deleted = new("task.deleted");
    
    /// <summary>
    /// Task was moved on Kanban board
    /// </summary>
    public static readonly EventKey Moved = new("task.moved");
    
    /// <summary>
    /// Task was archived
    /// </summary>
    public static readonly EventKey Archived = new("task.archived");
    
    /// <summary>
    /// Kanban board needs refresh
    /// </summary>
    public static readonly EventKey KanbanRefresh = new("task.kanbanRefresh");
}

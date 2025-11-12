using Swap.Htmx.Events;

namespace ProjectHub.Modules.Tasks.Contracts;

/// <summary>
/// UI-level events for task operations
/// </summary>
public static class TaskUiEvents
{
    public static readonly EventKey RefreshList = new("ui.tasks.refresh");
    public static readonly EventKey RefreshProjectTasks = new("ui.project.tasks.refresh");
    public static readonly EventKey RefreshStats = new("ui.dashboard.stats.refresh");
}

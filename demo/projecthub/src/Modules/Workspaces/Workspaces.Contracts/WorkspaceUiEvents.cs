using Swap.Htmx.Events;

namespace ProjectHub.Modules.Workspaces.Contracts;

/// <summary>
/// UI-level events for workspace operations
/// </summary>
public static class WorkspaceUiEvents
{
    public static readonly EventKey RefreshList = new("ui.workspaces.refresh");
    public static readonly EventKey RefreshStats = new("ui.dashboard.stats.refresh");
}

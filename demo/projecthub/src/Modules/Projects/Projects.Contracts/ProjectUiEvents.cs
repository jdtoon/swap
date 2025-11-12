using Swap.Htmx.Events;

namespace ProjectHub.Modules.Projects.Contracts;

/// <summary>
/// UI-level events for project operations
/// </summary>
public static class ProjectUiEvents
{
    public static readonly EventKey RefreshList = new("ui.projects.refresh");
    public static readonly EventKey RefreshWorkspaceProjects = new("ui.workspace.projects.refresh");
    public static readonly EventKey RefreshStats = new("ui.dashboard.stats.refresh");
}

using Swap.Htmx.Events;

namespace ProjectHub.Modules.Workspaces.Contracts;

public static class WorkspaceEvents
{
    public static readonly EventKey Created = new("workspace.created");
    public static readonly EventKey Updated = new("workspace.updated");
    public static readonly EventKey Archived = new("workspace.archived");
    public static readonly EventKey Unarchived = new("workspace.unarchived");
}

public static class WorkspaceEventPayloads
{
    public record Created(int WorkspaceId, string Name);
    public record Updated(int WorkspaceId, string Name);
    public record Archived(int WorkspaceId, string Name);
    public record Unarchived(int WorkspaceId, string Name);
}

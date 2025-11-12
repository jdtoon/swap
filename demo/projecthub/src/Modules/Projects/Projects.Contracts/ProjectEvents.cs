using Swap.Htmx.Events;

namespace ProjectHub.Modules.Projects.Contracts;

public static class ProjectEvents
{
    public static readonly EventKey Created = new("project.created");
    public static readonly EventKey Updated = new("project.updated");
    public static readonly EventKey Archived = new("project.archived");
    public static readonly EventKey Deleted = new("project.deleted");
    public static readonly EventKey StatusChanged = new("project.statusChanged");
}

public static class ProjectEventPayloads
{
    public record Created(int ProjectId, int WorkspaceId, string Name);
    public record Updated(int ProjectId, string Name);
    public record Archived(int ProjectId, string Name);
    public record Deleted(int ProjectId, int WorkspaceId);
    public record StatusChanged(int ProjectId, string OldStatus, string NewStatus);
}

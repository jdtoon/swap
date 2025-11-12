namespace ProjectHub.Modules.Workspaces.Contracts;

public static class WorkspaceEvents
{
    public static class Domain
    {
        public const string Created = "workspace.created";
        public const string Updated = "workspace.updated";
        public const string Archived = "workspace.archived";
        public const string Unarchived = "workspace.unarchived";
    }

    public static class Ui
    {
        public const string RefreshList = "ui.workspace.refreshList";
        public const string ToastSuccess = "ui.toast.success";
        public const string StatsRefresh = "ui.stats.refresh";
    }
}

public static class WorkspaceEventPayloads
{
    public record Created(int WorkspaceId, string Name);
    public record Updated(int WorkspaceId, string Name);
    public record Archived(int WorkspaceId, string Name);
    public record Unarchived(int WorkspaceId, string Name);
}

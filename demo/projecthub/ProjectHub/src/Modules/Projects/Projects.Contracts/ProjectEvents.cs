namespace ProjectHub.Modules.Projects.Contracts;

public static class ProjectEvents
{
    public static class Domain
    {
        public const string Created = "project.created";
        public const string Updated = "project.updated";
        public const string Archived = "project.archived";
        public const string Deleted = "project.deleted";
        public const string StatusChanged = "project.statusChanged";
    }

    public static class Ui
    {
        public const string RefreshList = "ui.project.refreshList";
        public const string RefreshWorkspaceProjects = "ui.project.refreshWorkspaceProjects";
        public const string ToastSuccess = "ui.toast.success";
        public const string StatsRefresh = "ui.stats.refresh";
    }
}

public static class ProjectEventPayloads
{
    public record Created(int ProjectId, int WorkspaceId, string Name);
    public record Updated(int ProjectId, string Name);
    public record Archived(int ProjectId, string Name);
    public record Deleted(int ProjectId, int WorkspaceId);
    public record StatusChanged(int ProjectId, string OldStatus, string NewStatus);
}

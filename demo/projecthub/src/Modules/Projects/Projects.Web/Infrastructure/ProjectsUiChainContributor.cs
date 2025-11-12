using Swap.Htmx.Events;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Workspaces.Contracts;

namespace ProjectHub.Modules.Projects.Web.Infrastructure;

public class ProjectsUiChainContributor : ISwapUiChainContributor
{
    public void Configure(SwapEventBusOptions options)
    {
        // When project is created, trigger UI refresh events
        options.Chain(ProjectEvents.Created,
            ProjectUiEvents.RefreshList,
            ProjectUiEvents.RefreshWorkspaceProjects,
            ProjectUiEvents.RefreshStats);

        // When project is updated
        options.Chain(ProjectEvents.Updated,
            ProjectUiEvents.RefreshList);

        // When project is archived
        options.Chain(ProjectEvents.Archived,
            ProjectUiEvents.RefreshList,
            ProjectUiEvents.RefreshStats);

        // Listen to workspace archived event - cascade UI updates
        options.Chain(WorkspaceEvents.Archived,
            ProjectUiEvents.RefreshList);
    }
}

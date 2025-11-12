using Swap.Htmx.Events;
using ProjectHub.Modules.Workspaces.Contracts;

namespace ProjectHub.Modules.Workspaces.Web.Infrastructure;

public class WorkspacesUiChainContributor : ISwapUiChainContributor
{
    public void Configure(SwapEventBusOptions options)
    {
        // When workspace is created, trigger UI refresh events
        options.Chain(WorkspaceEvents.Created, 
            WorkspaceUiEvents.RefreshList,
            WorkspaceUiEvents.RefreshStats);

        // When workspace is updated, trigger UI refresh
        options.Chain(WorkspaceEvents.Updated, 
            WorkspaceUiEvents.RefreshList);

        // When workspace is archived, trigger UI refresh
        options.Chain(WorkspaceEvents.Archived, 
            WorkspaceUiEvents.RefreshList,
            WorkspaceUiEvents.RefreshStats);
    }
}

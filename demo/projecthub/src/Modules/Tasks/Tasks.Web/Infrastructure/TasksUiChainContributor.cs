using Swap.Htmx.Events;
using ProjectHub.Modules.Tasks.Contracts;
using ProjectHub.Modules.Projects.Contracts;

namespace ProjectHub.Modules.Tasks.Web.Infrastructure;

public class TasksUiChainContributor : ISwapUiChainContributor
{
    public void Configure(SwapEventBusOptions options)
    {
        // When task is created, trigger UI refresh events
        options.Chain(TaskEvents.Created,
            TaskUiEvents.RefreshList,
            TaskEvents.KanbanRefresh,
            TaskUiEvents.RefreshProjectTasks,
            TaskUiEvents.RefreshStats);

        // When task is updated
        options.Chain(TaskEvents.Updated,
            TaskUiEvents.RefreshList,
            TaskEvents.KanbanRefresh,
            TaskUiEvents.RefreshProjectTasks);

        // When task is moved on kanban board
        options.Chain(TaskEvents.Moved,
            TaskEvents.KanbanRefresh,
            TaskUiEvents.RefreshProjectTasks);

        // When task is deleted
        options.Chain(TaskEvents.Deleted,
            TaskUiEvents.RefreshList,
            TaskEvents.KanbanRefresh,
            TaskUiEvents.RefreshProjectTasks,
            TaskUiEvents.RefreshStats);

        // When task is archived
        options.Chain(TaskEvents.Archived,
            TaskUiEvents.RefreshList,
            TaskUiEvents.RefreshProjectTasks);

        // Listen to project archived event - cascade UI updates
        options.Chain(ProjectEvents.Archived,
            TaskUiEvents.RefreshList,
            TaskEvents.KanbanRefresh);
    }
}

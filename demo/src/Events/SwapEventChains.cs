namespace TaskFlow.Events;

using Swap.Htmx.Events;

public static class SwapEventChains
{
    public static void Configure(SwapEventBusOptions events)
    {
        // Todo domain chains (homepage demo uses Todo items)
        events
            .Chain(EventNames.Domain.TodoCreated, EventNames.Ui.TodoRefreshList, EventNames.Ui.ToastSuccess, EventNames.Ui.StatsRefresh, EventNames.Ui.ActivityAppend)
            .Chain(EventNames.Domain.TodoDeleted, EventNames.Ui.ToastSuccess, EventNames.Ui.StatsRefresh, EventNames.Ui.ActivityAppend)
            .Chain(EventNames.Domain.TodoToggled, EventNames.Ui.StatsRefresh)
            .Chain(EventNames.Domain.ProjectNoteAdded, EventNames.Ui.DetailsRefresh, EventNames.Ui.SummaryRefresh, EventNames.Ui.ToastSuccess)
            .Chain(EventNames.Domain.BulkCompleted, EventNames.Ui.TodoRefreshList, EventNames.Ui.StatsRefresh, EventNames.Ui.ToastSuccess)
            
            // Task Management chains - comprehensive event handling
            .Chain(EventNames.Domain.TaskCreated, 
                EventNames.Ui.TaskBoardRefresh, 
                EventNames.Ui.TaskStatsRefresh, 
                EventNames.Ui.TaskActivityRefresh, 
                EventNames.Ui.ToastSuccess)
            
            .Chain(EventNames.Domain.TaskUpdated, 
                EventNames.Ui.TaskBoardRefresh, 
                EventNames.Ui.TaskActivityRefresh, 
                EventNames.Ui.ToastSuccess)
            
            .Chain(EventNames.Domain.TaskDeleted, 
                EventNames.Ui.TaskBoardRefresh, 
                EventNames.Ui.TaskStatsRefresh, 
                EventNames.Ui.TaskActivityRefresh, 
                EventNames.Ui.ToastSuccess)
            
            .Chain(EventNames.Domain.TaskStatusChanged, 
                EventNames.Ui.TaskTodoRefresh,
                EventNames.Ui.TaskInProgressRefresh,
                EventNames.Ui.TaskDoneRefresh,
                EventNames.Ui.TaskStatsRefresh, 
                EventNames.Ui.TaskActivityRefresh, 
                EventNames.Ui.ToastSuccess)
            
            .Chain(EventNames.Domain.TaskPriorityChanged, 
                EventNames.Ui.TaskBoardRefresh, 
                EventNames.Ui.TaskActivityRefresh, 
                EventNames.Ui.ToastSuccess)
            
            .Chain(EventNames.Domain.TaskAssigned, 
                EventNames.Ui.TaskBoardRefresh, 
                EventNames.Ui.TaskActivityRefresh, 
                EventNames.Ui.ToastSuccess)
            
            // Component demo chains
            .Chain(EventNames.Domain.ComponentAUpdated, EventNames.Ui.ComponentARefresh, EventNames.Ui.ToastSuccess)
            .Chain(EventNames.Domain.ComponentBUpdated, EventNames.Ui.ComponentBRefresh, EventNames.Ui.ToastSuccess);
    }
}

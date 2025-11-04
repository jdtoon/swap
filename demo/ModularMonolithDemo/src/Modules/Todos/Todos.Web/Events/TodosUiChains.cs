using ModularMonolithDemo.Modules.Todos.Contracts;
using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Todos.Web.Events;

public static class TodosUiChains
{
    public static void Configure(SwapEventBusOptions events)
    {
        events
            .Chain(TodoEvents.Domain.Created, TodoEvents.Ui.RefreshList, TodoEvents.Ui.ToastSuccess, TodoEvents.Ui.StatsRefresh)
            .Chain(TodoEvents.Domain.Deleted, TodoEvents.Ui.RefreshList, TodoEvents.Ui.ToastSuccess, TodoEvents.Ui.StatsRefresh)
            // Toggle returns the updated row directly; we still refresh stats via UI event
            .Chain(TodoEvents.Domain.Toggled, TodoEvents.Ui.StatsRefresh);
    }
}

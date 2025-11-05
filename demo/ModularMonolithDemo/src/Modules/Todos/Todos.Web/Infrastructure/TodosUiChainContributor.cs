using Swap.Htmx.Events;
using ModularMonolithDemo.Modules.Todos.Web.Events;

namespace ModularMonolithDemo.Modules.Todos.Web.Infrastructure;

public sealed class TodosUiChainContributor : ISwapUiChainContributor
{
    public void Configure(SwapEventBusOptions options)
    {
        TodosUiChains.Configure(options);
    }
}

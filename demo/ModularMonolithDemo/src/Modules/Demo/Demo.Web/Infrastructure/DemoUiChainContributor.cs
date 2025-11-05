using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Demo.Web.Infrastructure;

/// <summary>
/// Placeholder contributor to showcase the pattern. Add UI chains here when Demo needs them.
/// </summary>
public sealed class DemoUiChainContributor : ISwapUiChainContributor
{
    public void Configure(SwapEventBusOptions options)
    {
        // No UI chains for Demo in this demo app yet.
        // Example:
        // options.Chain("demo.something", Swap.Htmx.Events.SwapEvents.UI.RefreshList);
    }
}

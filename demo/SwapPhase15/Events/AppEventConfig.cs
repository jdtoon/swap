using Swap.Htmx;
using Swap.Htmx.Events;
using SwapPhase15.Events;

namespace SwapPhase15.Events;

public class AppEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // When counter is updated, also update stats
        events.Chain(AppEvents.Counter.Updated, AppEvents.Stats.Updated);
    }
}
using Swap.Htmx;
using Swap.Htmx.Events;

namespace SwapSmallPartials.Modules.Analytics.Events;

public class AnalyticsEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // When a purchase is completed, update all relevant partials via distributed handlers
        // The handlers themselves will determine what to update based on the event data
        
        // Cart abandonment event
        events.When(AnalyticsEvents.Cart.Abandoned);
        
        // Stock replenishment
        events.When(AnalyticsEvents.Stock.Replenished);
        
        // Hour advancement
        events.When(AnalyticsEvents.Hour.Advanced);
    }
}

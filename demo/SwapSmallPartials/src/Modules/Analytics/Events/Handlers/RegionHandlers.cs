using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// REGION HANDLERS (8 partials)
// ============================================================================

[SwapHandler]
public class RegionMapHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public RegionMapHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        if (_state.Regions.ContainsKey(evt.Region))
        {
            var region = _state.Regions[evt.Region];
            builder.AlsoUpdate($"partial-region-{evt.Region.ToLower()}", "_RegionCard", region);
        }
        return Task.CompletedTask;
    }
}

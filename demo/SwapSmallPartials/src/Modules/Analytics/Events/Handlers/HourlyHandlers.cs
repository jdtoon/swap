using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// HOURLY SALES HANDLERS (24 partials - one for each hour)
// ============================================================================

[SwapHandler]
public class HourlySalesHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public HourlySalesHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Update only the current hour's bar
        var hour = _state.CurrentHour;
        builder.AlsoUpdate($"partial-hour-{hour}", "_HourBar", new { Hour = hour, Revenue = _state.HourlySales[hour] });
        return Task.CompletedTask;
    }
}

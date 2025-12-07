using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// CUSTOMER METRICS HANDLERS (4 partials)
// ============================================================================

[SwapHandler]
public class NewCustomersHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public NewCustomersHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        if (evt.IsNewCustomer)
        {
            builder.AlsoUpdate("partial-new-customers", "_NewCustomers", _state);
        }
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class ReturningCustomersHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public ReturningCustomersHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        if (!evt.IsNewCustomer)
        {
            builder.AlsoUpdate("partial-returning-customers", "_ReturningCustomers", _state);
        }
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class VipCustomersHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public VipCustomersHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        if (evt.IsVip)
        {
            builder.AlsoUpdate("partial-vip-customers", "_VipCustomers", _state);
        }
        return Task.CompletedTask;
    }
}

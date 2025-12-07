using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// KPI HANDLERS (6 partials)
// ============================================================================

[SwapHandler]
public class RevenueTodayHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public RevenueTodayHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-revenue-today", "_RevenueToday", _state);
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class OrdersCountHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public OrdersCountHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-orders-count", "_OrdersCount", _state);
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class ConversionRateHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public ConversionRateHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-conversion-rate", "_ConversionRate", _state);
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class AvgOrderValueHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public AvgOrderValueHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-avg-order-value", "_AvgOrderValue", _state);
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class CartAbandonmentHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public CartAbandonmentHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("partial-cart-abandonment", "_CartAbandonment", _state);
        return Task.CompletedTask;
    }
}

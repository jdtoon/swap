using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// KPI HANDLERS - Distributed Event Pattern Showcase
// ============================================================================
//
// PATTERN: Each handler updates ONE partial in response to an event.
//
// WHY THIS IS POWERFUL:
// 1. Controllers don't know about these handlers—loose coupling
// 2. Adding a new KPI? Just add a handler. No refactoring.
// 3. Handlers don't know about each other—true independence
// 4. Testing is trivial (handler is just a function: event in → partial out)
//
// COMPARE TO REACT:
// React: One purchase → dispatch action → reducer updates state → 
//        60 components re-render (useEffect chains, memo checks) → 
//        need React DevTools to debug why component didn't update
//
// Swap.Htmx: One purchase → event fired → 6 handlers run → 
//            6 partials swap → done. Linear flow. Easy to trace.
//
// ============================================================================

/// <summary>
/// Updates the "Revenue Today" KPI when a purchase completes.
/// 
/// HTMX MAGIC: builder.AlsoUpdate() generates an "out-of-band" swap.
/// The server returns HTML with: <div id="partial-revenue-today" hx-swap-oob="true">
/// HTMX finds this in the response and swaps it into the DOM automatically.
/// </summary>
[SwapHandler]
public class RevenueTodayHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public RevenueTodayHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Render the _RevenueToday partial with current state, swap into #partial-revenue-today
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

/// <summary>
/// Demonstrates computed properties in state.
/// AnalyticsState.AvgOrderValue is a calculated property (RevenueToday / OrdersCount).
/// No need for React useMemo or manual caching—just a C# property.
/// </summary>
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

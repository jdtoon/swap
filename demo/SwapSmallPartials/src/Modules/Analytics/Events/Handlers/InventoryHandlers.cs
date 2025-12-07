using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// INVENTORY ALERT HANDLERS
// ============================================================================

[SwapHandler]
public class InventoryAlertsHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public InventoryAlertsHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        var product = _state.Products.FirstOrDefault(p => p.Id == evt.ProductId);
        
        // Only update if the product is now low stock or out of stock
        if (product != null && product.Stock <= 10)
        {
            builder.AlsoUpdate("partial-inventory-alerts", "_InventoryAlerts", _state);
        }
        
        return Task.CompletedTask;
    }
}

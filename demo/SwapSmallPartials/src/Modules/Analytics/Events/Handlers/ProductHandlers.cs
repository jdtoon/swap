using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// PRODUCT HANDLERS (12 partials - one for each product)
// ============================================================================

[SwapHandler]
public class ProductCardsHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public ProductCardsHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Update only the specific product that was purchased
        var product = _state.Products.FirstOrDefault(p => p.Id == evt.ProductId);
        if (product != null)
        {
            builder.AlsoUpdate($"partial-product-{product.Id}", "_ProductCard", product);
        }
        return Task.CompletedTask;
    }
}

using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Events.Handlers;

// ============================================================================
// CATEGORY HANDLERS (6 partials - one for each category)
// ============================================================================

[SwapHandler]
public class CategoryBreakdownHandler : ISwapEventHandler<PurchaseCompletedEvent>
{
    private readonly AnalyticsState _state;
    
    public CategoryBreakdownHandler(AnalyticsState state) => _state = state;
    
    public Task HandleAsync(PurchaseCompletedEvent evt, SwapResponseBuilder builder, CancellationToken ct)
    {
        var product = _state.Products.FirstOrDefault(p => p.Id == evt.ProductId);
        if (product != null && _state.Categories.ContainsKey(product.Category))
        {
            var category = _state.Categories[product.Category];
            builder.AlsoUpdate($"partial-category-{product.Category.ToLower()}", "_CategoryCard", category);
        }
        return Task.CompletedTask;
    }
}

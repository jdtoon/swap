using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Services;

namespace SwapShop.Events;

public class ProductEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(ProductEvents.StockChecked)
            .RefreshPartialAsync("stock-badge-container", "_StockBadge", async ctx =>
            {
                // Simulate async database operation
                await Task.Delay(500);
                
                // Get product ID from route
                if (int.TryParse(ctx.Request.RouteValues["id"]?.ToString(), out int id))
                {
                    var service = ctx.RequestServices.GetRequiredService<IProductService>();
                    return service.GetById(id);
                }
                return null;
            })
            .Toast("Stock checked asynchronously!", ToastType.Info);
    }
}

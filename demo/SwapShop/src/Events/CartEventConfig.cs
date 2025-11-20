using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Services;
using SwapShop.Views;

namespace SwapShop.Events;

public class CartEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(CartEvents.ItemAdded)
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetItemCount(ctx.Session.Id);
            })
            .Toast("Item added to cart", ToastType.Success);

        config.When(CartEvents.QuantityUpdated)
            .RefreshPartial(CartElements.Items, CartViews.Items, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetCart(ctx.Session.Id);
            })
            .RefreshPartial(CartElements.Total, CartViews.Total, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetCart(ctx.Session.Id);
            })
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetItemCount(ctx.Session.Id);
            });

        config.When(CartEvents.ItemRemoved)
            .RefreshPartial(CartElements.Items, CartViews.Items, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetCart(ctx.Session.Id);
            })
            .RefreshPartial(CartElements.Total, CartViews.Total, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetCart(ctx.Session.Id);
            })
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx =>
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetItemCount(ctx.Session.Id);
            })
            .Toast("Item removed", ToastType.Info);

        config.When(CartEvents.Cleared)
            .RefreshPartial(CartElements.Items, CartViews.Empty, ctx => null)
            .RefreshPartial(CartElements.Total, CartViews.Total, ctx => 
            {
                var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
                return cartService.GetCart(ctx.Session.Id);
            })
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
            .Toast("Cart cleared", ToastType.Info);

        config.When(CartEvents.AddFailed)
            .Toast("Could not add item - insufficient stock", ToastType.Error);

        config.When(CartEvents.UpdateFailed)
            .Toast("Could not update quantity - exceeds available stock", ToastType.Error);
    }
}

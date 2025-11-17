using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Services;
using SwapShop.Views;

namespace SwapShop.Events;

/// <summary>
/// Centralized HTTP event chain configuration for SwapShop
/// All event-driven UI updates are defined here instead of in Program.cs
/// </summary>
public static class EventChainConfiguration
{
    public static void ConfigureEventChains(SwapEventBusOptions config)
    {
        // ================================================================================
        // PRODUCT EVENTS
        // ================================================================================
        
        config.When(ProductEvents.Viewed)
            .RefreshPartial(ProductElements.Card, ProductViews.Card, ctx => null);

        config.When(ProductEvents.LowStock)
            .RefreshPartial(ProductElements.Card, ProductViews.StockBadge, ctx => null)
            .Toast("Low stock alert!", ToastType.Warning);

        config.When(ProductEvents.StockChecked)
            .RefreshPartial(ProductElements.Card, ProductViews.StockBadge, ctx => null);

        // ================================================================================
        // CART EVENTS - Demonstrates centralized coordination of multiple UI updates
        // ================================================================================
        
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
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
            .Toast("Cart cleared", ToastType.Info);

        config.When(CartEvents.AddFailed)
            .Toast("Could not add item - insufficient stock", ToastType.Error);

        config.When(CartEvents.UpdateFailed)
            .Toast("Could not update quantity - exceeds available stock", ToastType.Error);

        // ================================================================================
        // ORDER EVENTS - Demonstrates complex event chains with cascading triggers
        // ================================================================================
        
        config.When(OrderEvents.Created)
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
            .Toast("Order placed successfully!", ToastType.Success)
            .AlsoTrigger(NotificationEvents.OrderConfirmation);

        config.When(OrderEvents.Processing)
            .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx => null)
            .Toast("Order is being processed", ToastType.Info);

        config.When(OrderEvents.Shipped)
            .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx => null)
            .Toast("Order has been shipped!", ToastType.Success);

        config.When(OrderEvents.Delivered)
            .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx => null)
            .Toast("Order delivered!", ToastType.Success);

        config.When(OrderEvents.Failed)
            .Toast("Order failed - please try again", ToastType.Error);
    }
}

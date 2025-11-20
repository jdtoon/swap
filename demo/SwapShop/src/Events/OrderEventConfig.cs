using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Views;

namespace SwapShop.Events;

public class OrderEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(OrderEvents.Created)
            .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
            .Toast("Order placed successfully!", ToastType.Success)
            .Redirect("/Orders")
            .AlsoTrigger(NotificationEvents.OrderConfirmation);

        config.When(OrderEvents.Processing)
            .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx =>
            {
                // Get order from service - event payload not accessible here
                // In a real app, you'd pass order ID or get from route data
                return null; // Status partial will handle null gracefully
            })
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

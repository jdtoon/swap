using Swap.Htmx.Events;

namespace SwapShop.Events;

/// <summary>
/// Product-related events
/// </summary>
public static class ProductEvents
{
    public static readonly EventKey Viewed = new("product.viewed");
    public static readonly EventKey LowStock = new("product.lowStock");
    public static readonly EventKey StockChecked = new("product.stockChecked");
}

/// <summary>
/// Shopping cart events
/// </summary>
public static class CartEvents
{
    public static readonly EventKey ItemAdded = new("cart.itemAdded");
    public static readonly EventKey ItemRemoved = new("cart.itemRemoved");
    public static readonly EventKey QuantityUpdated = new("cart.quantityUpdated");
    public static readonly EventKey Cleared = new("cart.cleared");
    public static readonly EventKey AddFailed = new("cart.addFailed");
    public static readonly EventKey UpdateFailed = new("cart.updateFailed");
}

/// <summary>
/// Order events
/// </summary>
public static class OrderEvents
{
    public static readonly EventKey Created = new("order.created");
    public static readonly EventKey Processing = new("order.processing");
    public static readonly EventKey Shipped = new("order.shipped");
    public static readonly EventKey Delivered = new("order.delivered");
    public static readonly EventKey Failed = new("order.failed");
    public static readonly EventKey StatusChanged = new("order.statusChanged");
}

/// <summary>
/// Review events
/// </summary>
public static class ReviewEvents
{
    public static readonly EventKey Added = new("review.added");
}

/// <summary>
/// Notification events
/// </summary>
public static class NotificationEvents
{
    public static readonly EventKey OrderConfirmation = new("notification.orderConfirmation");
}

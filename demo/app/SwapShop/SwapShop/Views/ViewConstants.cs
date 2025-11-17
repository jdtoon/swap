namespace SwapShop.Views;

/// <summary>
/// Constants for partial view names (no magic strings!)
/// </summary>
public static class ProductViews
{
    public const string Grid = "_ProductGrid";
    public const string Card = "_ProductCard";
    public const string Count = "_ProductCount";
    public const string Details = "_ProductDetails";
    public const string StockBadge = "_StockBadge";
}

public static class CartViews
{
    public const string Badge = "_CartBadge";
    public const string Total = "_CartTotal";
    public const string Items = "_CartItems";
    public const string Item = "_CartItem";
    public const string Summary = "_CartSummary";
    public const string MiniCart = "_MiniCart";
    public const string Empty = "_EmptyCart";
}

public static class OrderViews
{
    public const string List = "_OrderList";
    public const string Card = "_OrderCard";
    public const string Status = "_OrderStatus";
    public const string Summary = "_OrderSummary";
}

public static class NotificationViews
{
    public const string Toast = "_Toast";
    public const string Alert = "_Alert";
}

/// <summary>
/// Constants for HTML element IDs (no magic strings!)
/// </summary>
public static class ProductElements
{
    public const string Grid = "product-grid";
    public const string Count = "product-count";
    public const string Card = "product-card";
}

public static class CartElements
{
    public const string Badge = "cart-badge";
    public const string Total = "cart-total";
    public const string Items = "cart-items";
    public const string Summary = "cart-summary";
    public const string MiniCart = "mini-cart";
}

public static class OrderElements
{
    public const string List = "order-list";
    public const string Status = "order-status";
}

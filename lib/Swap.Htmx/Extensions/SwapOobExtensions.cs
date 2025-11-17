using Swap.Htmx.Models;

namespace Swap.Htmx.Extensions;

/// <summary>
/// Extension methods for working with out-of-band swap IDs in list scenarios.
/// Simplifies handling dynamic IDs when dealing with collections of items.
/// </summary>
public static class SwapOobExtensions
{
    /// <summary>
    /// Generates an ID for an OOB swap target by combining a base ID with an instance identifier.
    /// Useful when updating individual items in a list.
    /// </summary>
    /// <param name="baseId">The base element ID (e.g., "order-status").</param>
    /// <param name="instanceId">The instance identifier (e.g., order ID, index).</param>
    /// <returns>A combined ID in the format "{baseId}-{instanceId}".</returns>
    /// <example>
    /// <code>
    /// // Instead of manual concatenation:
    /// var targetId = $"order-status-{order.Id}";
    /// 
    /// // Use WithId:
    /// var targetId = "order-status".WithId(order.Id);
    /// // Result: "order-status-42"
    /// 
    /// // Works with any type that converts to string:
    /// "product-card".WithId(productId);      // "product-card-123"
    /// "cart-item".WithId(index);             // "cart-item-0"
    /// "notification".WithId(notificationId); // "notification-abc-def"
    /// </code>
    /// </example>
    public static string WithId(this string baseId, object instanceId)
    {
        return $"{baseId}-{instanceId}";
    }

    /// <summary>
    /// Adds an out-of-band swap for a specific instance in a list.
    /// Shorthand for <c>builder.AlsoUpdate(baseId.WithId(instanceId), viewName, model, swapMode)</c>.
    /// </summary>
    /// <param name="builder">The SwapResponseBuilder to extend.</param>
    /// <param name="baseId">The base element ID.</param>
    /// <param name="instanceId">The instance identifier.</param>
    /// <param name="viewName">The partial view to render.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="swapMode">How to swap the content (defaults to OuterHTML).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Instead of:
    /// builder.AlsoUpdate($"order-status-{order.Id}", "_OrderStatus", order);
    /// 
    /// // Use AlsoUpdateById:
    /// builder.AlsoUpdateById("order-status", order.Id, "_OrderStatus", order);
    /// 
    /// // Combine with other methods for multiple updates:
    /// return SwapView("_OrderList", orders)
    ///     .AlsoUpdateById("order-status", updatedOrder.Id, "_OrderStatus", updatedOrder)
    ///     .AlsoUpdateById("order-total", updatedOrder.Id, "_OrderTotal", updatedOrder)
    ///     .WithSuccessToast("Order updated!");
    /// </code>
    /// </example>
    public static SwapResponseBuilder AlsoUpdateById(
        this SwapResponseBuilder builder,
        string baseId,
        object instanceId,
        string viewName,
        object? model = null,
        SwapMode swapMode = SwapMode.OuterHTML)
    {
        return builder.AlsoUpdate(baseId.WithId(instanceId), viewName, model, swapMode);
    }
}

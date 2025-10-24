namespace NetMX.Events;

/// <summary>
/// Order entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Order-specific event constants. This allows type-safe access like:
/// <code>Events.Order.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Order-related events.
    /// </summary>
    public static class Order
    {
        /// <summary>
        /// Event: "order.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Order is created.
        /// Payload: { orderId: Guid }
        /// </remarks>
        public const string Created = "order.created";
        
        /// <summary>
        /// Event: "order.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Order is updated.
        /// Payload: { orderId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "order.updated";
        
        /// <summary>
        /// Event: "order.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Order is deleted.
        /// Payload: { orderId: Guid }
        /// </remarks>
        public const string Deleted = "order.deleted";
    }
}

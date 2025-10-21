namespace NetMX.Events;

/// <summary>
/// Event constants for Order entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with Order-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// Events for Order entity
    /// </summary>
    public static class Order
    {
        /// <summary>
        /// Triggered when a new Order is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "order-created";

        /// <summary>
        /// Triggered when an existing Order is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "order-updated";

        /// <summary>
        /// Triggered when a Order is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "order-deleted";
    }
}
namespace NetMX.Events;

/// <summary>
/// Event constants for Product entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with Product-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// Events for Product entity
    /// </summary>
    public static class Product
    {
        /// <summary>
        /// Triggered when a new Product is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "product-created";

        /// <summary>
        /// Triggered when an existing Product is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "product-updated";

        /// <summary>
        /// Triggered when a Product is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "product-deleted";
    }
}
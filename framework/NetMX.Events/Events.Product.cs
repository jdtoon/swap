namespace NetMX.Events;

/// <summary>
/// Product entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Product-specific event constants. This allows type-safe access like:
/// <code>Events.Product.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Product-related events.
    /// </summary>
    public static class Product
    {
        /// <summary>
        /// Event: "product.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Product is created.
        /// Payload: { productId: Guid }
        /// </remarks>
        public const string Created = "product.created";
        
        /// <summary>
        /// Event: "product.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Product is updated.
        /// Payload: { productId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "product.updated";
        
        /// <summary>
        /// Event: "product.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Product is deleted.
        /// Payload: { productId: Guid }
        /// </remarks>
        public const string Deleted = "product.deleted";
    }
}

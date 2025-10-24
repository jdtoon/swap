namespace NetMX.Events;

/// <summary>
/// Supplier entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Supplier-specific event constants. This allows type-safe access like:
/// <code>Events.Supplier.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Supplier-related events.
    /// </summary>
    public static class Supplier
    {
        /// <summary>
        /// Event: "supplier.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Supplier is created.
        /// Payload: { supplierId: Guid }
        /// </remarks>
        public const string Created = "supplier.created";
        
        /// <summary>
        /// Event: "supplier.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Supplier is updated.
        /// Payload: { supplierId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "supplier.updated";
        
        /// <summary>
        /// Event: "supplier.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Supplier is deleted.
        /// Payload: { supplierId: Guid }
        /// </remarks>
        public const string Deleted = "supplier.deleted";
    }
}

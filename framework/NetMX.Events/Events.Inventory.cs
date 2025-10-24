namespace NetMX.Events;

/// <summary>
/// Inventory entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Inventory-specific event constants. This allows type-safe access like:
/// <code>Events.Inventory.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Inventory-related events.
    /// </summary>
    public static class Inventory
    {
        /// <summary>
        /// Event: "inventory.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Inventory is created.
        /// Payload: { inventoryId: Guid }
        /// </remarks>
        public const string Created = "inventory.created";
        
        /// <summary>
        /// Event: "inventory.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Inventory is updated.
        /// Payload: { inventoryId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "inventory.updated";
        
        /// <summary>
        /// Event: "inventory.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Inventory is deleted.
        /// Payload: { inventoryId: Guid }
        /// </remarks>
        public const string Deleted = "inventory.deleted";
    }
}

namespace NetMX.Events;

/// <summary>
/// Customer entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Customer-specific event constants. This allows type-safe access like:
/// <code>Events.Customer.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Customer-related events.
    /// </summary>
    public static class Customer
    {
        /// <summary>
        /// Event: "customer.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new Customer is created.
        /// Payload: { customerId: Guid }
        /// </remarks>
        public const string Created = "customer.created";
        
        /// <summary>
        /// Event: "customer.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a Customer is updated.
        /// Payload: { customerId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "customer.updated";
        
        /// <summary>
        /// Event: "customer.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a Customer is deleted.
        /// Payload: { customerId: Guid }
        /// </remarks>
        public const string Deleted = "customer.deleted";
    }
}

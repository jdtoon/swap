namespace NetMX.Events;

/// <summary>
/// Event constants for AuditEntry entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with AuditEntry-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// AuditEntry-related events.
    /// </summary>
    public static class AuditEntry
    {
        /// <summary>
        /// Triggered when a new auditentry is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "auditentry:created";
        
        /// <summary>
        /// Triggered when a auditentry is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "auditentry:updated";
        
        /// <summary>
        /// Triggered when a auditentry is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "auditentry:deleted";
    }
}
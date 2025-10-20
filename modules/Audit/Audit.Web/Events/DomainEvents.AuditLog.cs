namespace NetMX.Events;

/// <summary>
/// Event constants for AuditLog entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with AuditLog-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// AuditLog-related events.
    /// </summary>
    public static class AuditLog
    {
        /// <summary>
        /// Triggered when a new auditlog is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "auditlog:created";
        
        /// <summary>
        /// Triggered when a auditlog is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "auditlog:updated";
        
        /// <summary>
        /// Triggered when a auditlog is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "auditlog:deleted";
    }
}
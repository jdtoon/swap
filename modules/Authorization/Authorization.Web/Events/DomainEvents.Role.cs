namespace NetMX.Events;

/// <summary>
/// Event constants for Role entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with Role-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// Role-related events.
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// Triggered when a new role is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "role:created";
        
        /// <summary>
        /// Triggered when a role is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "role:updated";
        
        /// <summary>
        /// Triggered when a role is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "role:deleted";
    }
}
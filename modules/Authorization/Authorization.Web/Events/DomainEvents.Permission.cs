namespace NetMX.Events;

/// <summary>
/// Event constants for Permission entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with Permission-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// Permission-related events.
    /// </summary>
    public static class Permission
    {
        /// <summary>
        /// Triggered when a new permission is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "permission:created";
        
        /// <summary>
        /// Triggered when a permission is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "permission:updated";
        
        /// <summary>
        /// Triggered when a permission is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "permission:deleted";
    }
}
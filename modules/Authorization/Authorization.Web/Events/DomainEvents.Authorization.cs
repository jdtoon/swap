namespace NetMX.Events;

/// <summary>
/// Authorization module domain events.
/// Extends the base DomainEvents class using partial classes.
/// </summary>
public static partial class DomainEvents
{
    /// <summary>
    /// Permission-related events.
    /// </summary>
    public static class Permission
    {
        /// <summary>
        /// Triggered when a permission is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "permission.created";

        /// <summary>
        /// Triggered when a permission is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "permission.updated";

        /// <summary>
        /// Triggered when a permission is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "permission.deleted";
    }

    /// <summary>
    /// Role-related events.
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// Triggered when a role is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "role.created";

        /// <summary>
        /// Triggered when a role is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "role.updated";

        /// <summary>
        /// Triggered when a role is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "role.deleted";
    }
}

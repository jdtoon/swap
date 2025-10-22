namespace NetMX.Events;

/// <summary>
/// Authorization module events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref="Events"/> class with
/// Authorization-specific event constants. This allows type-safe access like:
/// <code>Events.Permission.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{
    /// <summary>
    /// Permission-related events from Authorization module.
    /// </summary>
    public static class Permission
    {
        /// <summary>
        /// Event: "permission.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new permission is created.
        /// Payload: { permissionId: Guid, name: string, displayName: string }
        /// </remarks>
        public const string Created = "permission.created";
        
        /// <summary>
        /// Event: "permission.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a permission is updated.
        /// Payload: { permissionId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "permission.updated";
        
        /// <summary>
        /// Event: "permission.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a permission is deleted.
        /// Payload: { permissionId: Guid, name: string }
        /// </remarks>
        public const string Deleted = "permission.deleted";
    }
    
    /// <summary>
    /// Role-related events from Authorization module.
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// Event: "role.created"
        /// </summary>
        /// <remarks>
        /// Triggered when a new role is created.
        /// Payload: { roleId: Guid, name: string, isSystemRole: bool }
        /// </remarks>
        public const string Created = "role.created";
        
        /// <summary>
        /// Event: "role.updated"
        /// </summary>
        /// <remarks>
        /// Triggered when a role is updated.
        /// Payload: { roleId: Guid, changes: string[] }
        /// </remarks>
        public const string Updated = "role.updated";
        
        /// <summary>
        /// Event: "role.deleted"
        /// </summary>
        /// <remarks>
        /// Triggered when a role is deleted.
        /// Payload: { roleId: Guid, name: string }
        /// </remarks>
        public const string Deleted = "role.deleted";
        
        /// <summary>
        /// Event: "role.permission.granted"
        /// </summary>
        /// <remarks>
        /// Triggered when a permission is granted to a role.
        /// Payload: { roleId: Guid, permissionId: Guid, grantedBy: Guid }
        /// </remarks>
        public const string PermissionGranted = "role.permission.granted";
        
        /// <summary>
        /// Event: "role.permission.revoked"
        /// </summary>
        /// <remarks>
        /// Triggered when a permission is revoked from a role.
        /// Payload: { roleId: Guid, permissionId: Guid, revokedBy: Guid }
        /// </remarks>
        public const string PermissionRevoked = "role.permission.revoked";
    }
}

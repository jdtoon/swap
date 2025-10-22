using NetMX.Events;

namespace NetMX.Authorization.Web.Events;

/// <summary>
/// Defines and registers all Authorization module events.
/// </summary>
public static class AuthorizationEventDefinitions
{
    /// <summary>
    /// Registers all Authorization events with the event registry.
    /// </summary>
    /// <param name="registry">The event registry to register events with.</param>
    public static void Register(IEventRegistry registry)
    {
        // Permission events
        registry.RegisterEvent(AuthorizationEvents.Permission.Created, new EventMetadata
        {
            Name = AuthorizationEvents.Permission.Created,
            Module = "Authorization",
            Category = "Permission",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a new permission is created. Payload: { permissionId: Guid, name: string, displayName: string }"
        });
        
        registry.RegisterEvent(AuthorizationEvents.Permission.Updated, new EventMetadata
        {
            Name = AuthorizationEvents.Permission.Updated,
            Module = "Authorization",
            Category = "Permission",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a permission is updated. Payload: { permissionId: Guid, changes: string[] }"
        });
        
        registry.RegisterEvent(AuthorizationEvents.Permission.Deleted, new EventMetadata
        {
            Name = AuthorizationEvents.Permission.Deleted,
            Module = "Authorization",
            Category = "Permission",
            Direction = EventDirection.Terminal,
            Description = "Triggered when a permission is deleted. Payload: { permissionId: Guid, name: string }"
        });
        
        // Role events
        registry.RegisterEvent(AuthorizationEvents.Role.Created, new EventMetadata
        {
            Name = AuthorizationEvents.Role.Created,
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a new role is created. Payload: { roleId: Guid, name: string, isSystemRole: bool }"
        });
        
        registry.RegisterEvent(AuthorizationEvents.Role.Updated, new EventMetadata
        {
            Name = AuthorizationEvents.Role.Updated,
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a role is updated. Payload: { roleId: Guid, changes: string[] }"
        });
        
        registry.RegisterEvent(AuthorizationEvents.Role.Deleted, new EventMetadata
        {
            Name = AuthorizationEvents.Role.Deleted,
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Terminal,
            Description = "Triggered when a role is deleted. Payload: { roleId: Guid, name: string }"
        });
        
        registry.RegisterEvent(AuthorizationEvents.Role.PermissionGranted, new EventMetadata
        {
            Name = AuthorizationEvents.Role.PermissionGranted,
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a permission is granted to a role. Payload: { roleId: Guid, permissionId: Guid, grantedBy: Guid }"
        });
        
        registry.RegisterEvent(AuthorizationEvents.Role.PermissionRevoked, new EventMetadata
        {
            Name = AuthorizationEvents.Role.PermissionRevoked,
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a permission is revoked from a role. Payload: { roleId: Guid, permissionId: Guid, revokedBy: Guid }"
        });
    }
}

/// <summary>
/// Type-safe event name constants for Authorization module.
/// Use these instead of magic strings for IntelliSense support.
/// </summary>
public static class AuthorizationEvents
{
    /// <summary>
    /// Permission-related events.
    /// </summary>
    public static class Permission
    {
        /// <summary>
        /// Event name: "permission.created"
        /// </summary>
        public const string Created = "permission.created";
        
        /// <summary>
        /// Event name: "permission.updated"
        /// </summary>
        public const string Updated = "permission.updated";
        
        /// <summary>
        /// Event name: "permission.deleted"
        /// </summary>
        public const string Deleted = "permission.deleted";
    }
    
    /// <summary>
    /// Role-related events.
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// Event name: "role.created"
        /// </summary>
        public const string Created = "role.created";
        
        /// <summary>
        /// Event name: "role.updated"
        /// </summary>
        public const string Updated = "role.updated";
        
        /// <summary>
        /// Event name: "role.deleted"
        /// </summary>
        public const string Deleted = "role.deleted";
        
        /// <summary>
        /// Event name: "role.permission.granted"
        /// </summary>
        public const string PermissionGranted = "role.permission.granted";
        
        /// <summary>
        /// Event name: "role.permission.revoked"
        /// </summary>
        public const string PermissionRevoked = "role.permission.revoked";
    }
}

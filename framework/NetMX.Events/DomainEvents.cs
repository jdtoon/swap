namespace NetMX.Events;

/// <summary>
/// Provides type-safe event names for HTMX event-driven communication.
/// </summary>
/// <remarks>
/// Use these constants instead of magic strings for HTMX events to get:
/// - IntelliSense support
/// - Compile-time checking
/// - Refactoring safety
/// - Self-documenting code
/// 
/// This class is marked as partial to allow modules to extend it with their own events.
/// Each module should create a DomainEvents.{ModuleName}.cs file that extends this class.
/// 
/// Example usage in controller:
/// <code>
/// this.HxTrigger(DomainEvents.User.Created, new { userId = user.Id });
/// </code>
/// 
/// Example usage in view:
/// <code>
/// &lt;div hx-get="/api/stats" 
///      hx-trigger="@DomainEvents.User.Created from:body"&gt;
/// &lt;/div&gt;
/// </code>
/// 
/// Example module extension:
/// <code>
/// // In Authorization.Web/Events/DomainEvents.Authorization.cs
/// namespace NetMX.Events;
/// 
/// public static partial class DomainEvents
/// {
///     public static class Permission
///     {
///         public const string Created = "permission.created";
///     }
/// }
/// </code>
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// User-related events.
    /// </summary>
    public static class User
    {
        /// <summary>
        /// Triggered when a new user is created.
        /// Payload: { userId: Guid }
        /// </summary>
        public const string Created = "user:created";
        
        /// <summary>
        /// Triggered when a user is updated.
        /// Payload: { userId: Guid }
        /// </summary>
        public const string Updated = "user:updated";
        
        /// <summary>
        /// Triggered when a user is deleted.
        /// Payload: { userId: Guid }
        /// </summary>
        public const string Deleted = "user:deleted";
        
        /// <summary>
        /// Triggered when a user's role changes.
        /// Payload: { userId: Guid, roleId: Guid }
        /// </summary>
        public const string RoleChanged = "user:role-changed";
        
        /// <summary>
        /// Triggered when a user logs in.
        /// Payload: { userId: Guid, timestamp: DateTime }
        /// </summary>
        public const string LoggedIn = "user:logged-in";
        
        /// <summary>
        /// Triggered when a user logs out.
        /// Payload: { userId: Guid }
        /// </summary>
        public const string LoggedOut = "user:logged-out";
    }
    
    /// <summary>
    /// Role-related events.
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// Triggered when a new role is created.
        /// Payload: { roleId: Guid }
        /// </summary>
        public const string Created = "role:created";
        
        /// <summary>
        /// Triggered when a role is updated.
        /// Payload: { roleId: Guid }
        /// </summary>
        public const string Updated = "role:updated";
        
        /// <summary>
        /// Triggered when a role is deleted.
        /// Payload: { roleId: Guid }
        /// </summary>
        public const string Deleted = "role:deleted";
        
        /// <summary>
        /// Triggered when permissions for a role change.
        /// Payload: { roleId: Guid, permissions: string[] }
        /// </summary>
        public const string PermissionsChanged = "role:permissions-changed";
    }
    
    /// <summary>
    /// Audit-related events.
    /// </summary>
    public static class Audit
    {
        /// <summary>
        /// Triggered when a new audit log entry is created.
        /// Payload: { auditLogId: Guid, entityType: string, action: string }
        /// </summary>
        public const string LogCreated = "audit:log-created";
        
        /// <summary>
        /// Triggered when audit settings are changed.
        /// Payload: { setting: string, value: object }
        /// </summary>
        public const string SettingsChanged = "audit:settings-changed";
    }
    
    /// <summary>
    /// Generic entity events (use when specific events don't exist).
    /// </summary>
    public static class Entity
    {
        /// <summary>
        /// Triggered when any entity is created.
        /// Payload: { entityType: string, entityId: object }
        /// </summary>
        public const string Created = "entity:created";
        
        /// <summary>
        /// Triggered when any entity is updated.
        /// Payload: { entityType: string, entityId: object }
        /// </summary>
        public const string Updated = "entity:updated";
        
        /// <summary>
        /// Triggered when any entity is deleted.
        /// Payload: { entityType: string, entityId: object }
        /// </summary>
        public const string Deleted = "entity:deleted";
    }
    
    /// <summary>
    /// UI-related events.
    /// </summary>
    public static class UI
    {
        /// <summary>
        /// Triggered to show a toast notification.
        /// Payload: { message: string, type: "success"|"error"|"warning"|"info" }
        /// </summary>
        public const string ShowToast = "ui:show-toast";
        
        /// <summary>
        /// Triggered to close a modal dialog.
        /// Payload: { modalId: string }
        /// </summary>
        public const string CloseModal = "ui:close-modal";
        
        /// <summary>
        /// Triggered to refresh a specific section.
        /// Payload: { sectionId: string }
        /// </summary>
        public const string RefreshSection = "ui:refresh-section";
        
        /// <summary>
        /// Triggered to show a loading indicator.
        /// Payload: { targetId: string }
        /// </summary>
        public const string ShowLoading = "ui:show-loading";
        
        /// <summary>
        /// Triggered to hide a loading indicator.
        /// Payload: { targetId: string }
        /// </summary>
        public const string HideLoading = "ui:hide-loading";
    }
    
    /// <summary>
    /// Form-related events.
    /// </summary>
    public static class Form
    {
        /// <summary>
        /// Triggered when a form is submitted successfully.
        /// Payload: { formId: string, data: object }
        /// </summary>
        public const string Submitted = "form:submitted";
        
        /// <summary>
        /// Triggered when form validation fails.
        /// Payload: { formId: string, errors: object }
        /// </summary>
        public const string ValidationFailed = "form:validation-failed";
        
        /// <summary>
        /// Triggered when a form is reset.
        /// Payload: { formId: string }
        /// </summary>
        public const string Reset = "form:reset";
    }
}

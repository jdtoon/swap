using Microsoft.AspNetCore.Authorization;

namespace Authorization.Web.Attributes;

/// <summary>
/// Authorization attribute that requires the current user to have the specified permission.
/// Uses ASP.NET Core's policy-based authorization system.
/// </summary>
/// <example>
/// <code>
/// [RequirePermission("Users.View")]
/// public async Task&lt;IActionResult&gt; Index()
/// {
///     // Only users with "Users.View" permission can access
/// }
/// 
/// [RequirePermission("Users.Delete")]
/// public async Task&lt;IActionResult&gt; Delete(Guid id)
/// {
///     // Only users with "Users.Delete" permission can access
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets the permission name required to access the resource
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Creates a new RequirePermission attribute
    /// </summary>
    /// <param name="permission">The permission name required (e.g., "Users.View")</param>
    public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission name cannot be empty", nameof(permission));

        Permission = permission;
        Policy = $"Permission:{permission}";
    }
}

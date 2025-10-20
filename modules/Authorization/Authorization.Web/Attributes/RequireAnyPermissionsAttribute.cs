using Microsoft.AspNetCore.Authorization;

namespace Authorization.Web.Attributes;

/// <summary>
/// Authorization attribute that requires the current user to have ANY of the specified permissions.
/// Uses ASP.NET Core's policy-based authorization system.
/// </summary>
/// <example>
/// <code>
/// [RequireAnyPermissions("Users.View", "Users.Edit", "Users.Delete")]
/// public async Task&lt;IActionResult&gt; Index()
/// {
///     // Users with ANY of these permissions can access
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireAnyPermissionsAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets the permissions where at least one is required
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Creates a new RequireAnyPermissions attribute
    /// </summary>
    /// <param name="permissions">The permission names where at least one is required</param>
    public RequireAnyPermissionsAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        if (permissions.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Permission names cannot be empty", nameof(permissions));

        Permissions = permissions;
        Policy = $"PermissionsAny:{string.Join(",", permissions)}";
    }
}

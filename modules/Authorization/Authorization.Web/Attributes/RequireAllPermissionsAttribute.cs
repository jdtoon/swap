using Microsoft.AspNetCore.Authorization;

namespace Authorization.Web.Attributes;

/// <summary>
/// Authorization attribute that requires the current user to have ALL of the specified permissions.
/// Uses ASP.NET Core's policy-based authorization system.
/// </summary>
/// <example>
/// <code>
/// [RequireAllPermissions("Users.View", "Users.Edit")]
/// public async Task&lt;IActionResult&gt; Edit(Guid id)
/// {
///     // Only users with BOTH "Users.View" AND "Users.Edit" can access
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireAllPermissionsAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Gets the permissions that are all required
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Creates a new RequireAllPermissions attribute
    /// </summary>
    /// <param name="permissions">The permission names that are all required</param>
    public RequireAllPermissionsAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        if (permissions.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Permission names cannot be empty", nameof(permissions));

        Permissions = permissions;
        Policy = $"PermissionsAll:{string.Join(",", permissions)}";
    }
}

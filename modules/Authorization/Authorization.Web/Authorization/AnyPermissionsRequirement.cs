using Microsoft.AspNetCore.Authorization;

namespace Authorization.Web.Authorization;

/// <summary>
/// Represents a requirement for any of the specified permissions (OR logic).
/// Used in ASP.NET Core's policy-based authorization.
/// </summary>
public class AnyPermissionsRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the permissions where at least one is required
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Creates a new any-permissions requirement
    /// </summary>
    /// <param name="permissions">The permission names where at least one is required</param>
    public AnyPermissionsRequirement(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        if (permissions.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Permission names cannot be empty", nameof(permissions));

        Permissions = permissions;
    }
}

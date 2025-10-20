using Microsoft.AspNetCore.Authorization;

namespace Authorization.Web.Authorization;

/// <summary>
/// Represents a requirement for all of the specified permissions (AND logic).
/// Used in ASP.NET Core's policy-based authorization.
/// </summary>
public class AllPermissionsRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the permissions that are all required
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Creates a new all-permissions requirement
    /// </summary>
    /// <param name="permissions">The permission names that are all required</param>
    public AllPermissionsRequirement(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        if (permissions.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Permission names cannot be empty", nameof(permissions));

        Permissions = permissions;
    }
}

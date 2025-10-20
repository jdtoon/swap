using Microsoft.AspNetCore.Authorization;

namespace Authorization.Web.Authorization;

/// <summary>
/// Represents a requirement for a single permission.
/// Used in ASP.NET Core's policy-based authorization.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the permission name that is required
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Creates a new permission requirement
    /// </summary>
    /// <param name="permission">The permission name required (e.g., "Users.View")</param>
    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission name cannot be empty", nameof(permission));

        Permission = permission;
    }
}

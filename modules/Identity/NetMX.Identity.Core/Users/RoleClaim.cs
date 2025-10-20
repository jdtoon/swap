using Microsoft.AspNetCore.Identity;
using NetMX.Ddd.Domain;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents a claim associated with a role.
/// Extends ASP.NET Core Identity with custom navigation properties.
/// </summary>
public class RoleClaim : IdentityRoleClaim<Guid>
{
    // Note: Id, RoleId, ClaimType, ClaimValue are inherited from IdentityRoleClaim<Guid>

    // Navigation property
    public AppRole Role { get; private set; } = null!;

    // EF Core constructor
    public RoleClaim()
    {
    }

    /// <summary>
    /// Creates a new role claim.
    /// </summary>
    public RoleClaim(Guid roleId, string claimType, string claimValue)
    {
        RoleId = roleId;
        ClaimType = Guard.NotNullOrEmpty(claimType, nameof(claimType));
        ClaimValue = Guard.NotNullOrEmpty(claimValue, nameof(claimValue));
    }
}

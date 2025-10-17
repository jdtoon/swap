using NetMX.Ddd.Domain.Entities;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents a claim associated with a role.
/// </summary>
public class RoleClaim : Entity<Guid>
{
    /// <summary>
    /// The role ID.
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// The claim type.
    /// </summary>
    public string ClaimType { get; private set; }

    /// <summary>
    /// The claim value.
    /// </summary>
    public string ClaimValue { get; private set; }

    // Navigation property
    public AppRole Role { get; private set; } = null!;

    // EF Core constructor
    private RoleClaim()
    {
        ClaimType = string.Empty;
        ClaimValue = string.Empty;
    }

    /// <summary>
    /// Creates a new role claim.
    /// </summary>
    public RoleClaim(Guid roleId, string claimType, string claimValue)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        ClaimType = Guard.NotNullOrEmpty(claimType, nameof(claimType));
        ClaimValue = Guard.NotNullOrEmpty(claimValue, nameof(claimValue));
    }
}

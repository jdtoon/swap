using NetMX.Ddd.Domain.Entities;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents a claim associated with a user.
/// </summary>
public class UserClaim : Entity<Guid>
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The claim type.
    /// </summary>
    public string ClaimType { get; private set; }

    /// <summary>
    /// The claim value.
    /// </summary>
    public string ClaimValue { get; private set; }

    // Navigation property
    public AppUser User { get; private set; } = null!;

    // EF Core constructor
    private UserClaim()
    {
        ClaimType = string.Empty;
        ClaimValue = string.Empty;
    }

    /// <summary>
    /// Creates a new user claim.
    /// </summary>
    public UserClaim(Guid userId, string claimType, string claimValue)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ClaimType = Guard.NotNullOrEmpty(claimType, nameof(claimType));
        ClaimValue = Guard.NotNullOrEmpty(claimValue, nameof(claimValue));
    }
}

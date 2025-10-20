using Microsoft.AspNetCore.Identity;
using NetMX.Ddd.Domain;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents a claim associated with a user.
/// Extends ASP.NET Core Identity with custom navigation properties.
/// </summary>
public class UserClaim : IdentityUserClaim<Guid>
{
    // Note: Id, UserId, ClaimType, ClaimValue are inherited from IdentityUserClaim<Guid>

    // Navigation property
    public AppUser User { get; private set; } = null!;

    // EF Core constructor
    public UserClaim()
    {
    }

    /// <summary>
    /// Creates a new user claim.
    /// </summary>
    public UserClaim(Guid userId, string claimType, string claimValue)
    {
        UserId = userId;
        ClaimType = Guard.NotNullOrEmpty(claimType, nameof(claimType));
        ClaimValue = Guard.NotNullOrEmpty(claimValue, nameof(claimValue));
    }
}

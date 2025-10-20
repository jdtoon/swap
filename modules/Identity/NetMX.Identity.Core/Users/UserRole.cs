using Microsoft.AspNetCore.Identity;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// Extends ASP.NET Core Identity with custom navigation properties.
/// </summary>
public class UserRole : IdentityUserRole<Guid>
{
    // Note: UserId and RoleId are inherited from IdentityUserRole<Guid>

    // Navigation properties
    public AppUser User { get; private set; } = null!;
    public AppRole Role { get; private set; } = null!;

    // EF Core constructor
    public UserRole() { }

    /// <summary>
    /// Creates a new user-role association.
    /// </summary>
    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}

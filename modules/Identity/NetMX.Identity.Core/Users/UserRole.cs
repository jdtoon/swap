using NetMX.Ddd.Domain.Entities;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// </summary>
public class UserRole : Entity<Guid>
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The role ID.
    /// </summary>
    public Guid RoleId { get; private set; }

    // Navigation properties
    public AppUser User { get; private set; } = null!;
    public AppRole Role { get; private set; } = null!;

    // EF Core constructor
    private UserRole() { }

    /// <summary>
    /// Creates a new user-role association.
    /// </summary>
    public UserRole(Guid userId, Guid roleId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoleId = roleId;
    }
}

using NetMX.Ddd.Domain.Entities;

namespace Identity.Core.Entities;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// </summary>
public class AppUserRole : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Private constructor for EF Core
    private AppUserRole() { }

    // Factory method
    public static AppUserRole Create(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID is required", nameof(roleId));

        return new AppUserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId
        };
    }
}

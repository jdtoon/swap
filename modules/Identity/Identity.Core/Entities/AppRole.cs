using NetMX.Ddd.Domain.Entities;

namespace Identity.Core.Entities;

/// <summary>
/// Represents a role in the application (e.g., Admin, User, Manager).
/// Roles are used to group permissions and assign them to users.
/// </summary>
public class AppRole : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    // Private constructor for EF Core
    private AppRole() { }

    // Factory method
    public static AppRole Create(string name, string? description = null, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required", nameof(name));

        return new AppRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole
        };
    }

    // Business logic methods
    public void Update(string name, string? description)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("System roles cannot be modified");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required", nameof(name));

        Name = name;
        Description = description;
    }
}

using NetMX.Authorization.Entities;
using NetMX.Ddd.Domain.Repositories;

namespace NetMX.Authorization.Seeding;

/// <summary>
/// Seeds system roles for the Authorization module.
/// Run this seeder after PermissionSeeder.
/// </summary>
public class RoleSeeder
{
    private readonly IRepository<Role, Guid> _roleRepository;
    private readonly IRepository<Permission, Guid> _permissionRepository;

    public RoleSeeder(
        IRepository<Role, Guid> roleRepository,
        IRepository<Permission, Guid> permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// Seeds system roles with default permissions.
    /// Idempotent - safe to run multiple times.
    /// </summary>
    public async Task SeedAsync(Guid? adminUserId = null)
    {
        // Check if already seeded
        if (await _roleRepository.GetCountAsync() > 0)
        {
            return; // Already seeded
        }

        // Get all permissions for assignment
        var allPermissions = await _permissionRepository.GetListAsync();
        var permissionDict = allPermissions.ToDictionary(p => p.Name, p => p);

        // Create roles
        await SeedAdminRole(permissionDict, adminUserId);
        await SeedUserRole(permissionDict, adminUserId);
        await SeedModeratorRole(permissionDict, adminUserId);
    }

    /// <summary>
    /// Seeds the Admin role with all permissions.
    /// </summary>
    private async Task SeedAdminRole(Dictionary<string, Permission> permissions, Guid? grantedBy)
    {
        var adminRole = new Role(
            id: Guid.NewGuid(),
            name: "Admin",
            description: "System administrator with full access",
            isSystemRole: true,
            isDefault: false
        );

        // Grant ALL permissions to Admin
        foreach (var permission in permissions.Values)
        {
            adminRole.GrantPermission(permission.Id, grantedBy ?? Guid.Empty, DateTime.UtcNow);
        }

        await _roleRepository.InsertAsync(adminRole);
    }

    /// <summary>
    /// Seeds the User role with basic permissions.
    /// </summary>
    private async Task SeedUserRole(Dictionary<string, Permission> permissions, Guid? grantedBy)
    {
        var userRole = new Role(
            id: Guid.NewGuid(),
            name: "User",
            description: "Standard user with basic access",
            isSystemRole: true,
            isDefault: true // New users get this role by default
        );

        // Grant basic view permissions only
        var basicPermissions = new[]
        {
            "Users.View",           // Can view user list
            "Roles.View",           // Can view roles
            "Permissions.View",     // Can view permissions
            "System.ViewHealth"     // Can view system health
        };

        foreach (var permissionName in basicPermissions)
        {
            if (permissions.TryGetValue(permissionName, out var permission))
            {
                userRole.GrantPermission(permission.Id, grantedBy ?? Guid.Empty, DateTime.UtcNow);
            }
        }

        await _roleRepository.InsertAsync(userRole);
    }

    /// <summary>
    /// Seeds the Moderator role with moderate permissions.
    /// </summary>
    private async Task SeedModeratorRole(Dictionary<string, Permission> permissions, Guid? grantedBy)
    {
        var moderatorRole = new Role(
            id: Guid.NewGuid(),
            name: "Moderator",
            description: "Moderator with user management permissions",
            isSystemRole: true,
            isDefault: false
        );

        // Grant user management permissions
        var moderatorPermissions = new[]
        {
            "Users.View",
            "Users.Create",
            "Users.Edit",
            "Users.ManageRoles",    // Can assign roles to users
            "Roles.View",
            "Permissions.View",
            "Authorization.ViewAudit",
            "System.ViewHealth",
            "System.ViewLogs"
        };

        foreach (var permissionName in moderatorPermissions)
        {
            if (permissions.TryGetValue(permissionName, out var permission))
            {
                moderatorRole.GrantPermission(permission.Id, grantedBy ?? Guid.Empty, DateTime.UtcNow);
            }
        }

        await _roleRepository.InsertAsync(moderatorRole);
    }
}

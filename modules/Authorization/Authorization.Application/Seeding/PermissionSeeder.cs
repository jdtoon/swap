using Authorization.Core.Entities;
using NetMX.Ddd.Domain.Repositories;

namespace Authorization.Application.Seeding;

/// <summary>
/// Seeds system permissions for the Authorization module.
/// Run this seeder during application startup or via migration.
/// </summary>
public class PermissionSeeder
{
    private readonly IRepository<Permission, Guid> _permissionRepository;

    public PermissionSeeder(IRepository<Permission, Guid> permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// Seeds all system permissions.
    /// Idempotent - safe to run multiple times.
    /// </summary>
    public async Task SeedAsync()
    {
        // Check if already seeded
        var existingPermissions = await _permissionRepository.GetListAsync();
        if (existingPermissions.Count > 0)
        {
            return; // Already seeded
        }

        var permissions = GetSystemPermissions();

        foreach (var permission in permissions)
        {
            await _permissionRepository.InsertAsync(permission);
        }
    }

    /// <summary>
    /// Defines all system permissions.
    /// Organized by group for better management.
    /// </summary>
    private List<Permission> GetSystemPermissions()
    {
        return new List<Permission>
        {
            // ===== Users Group =====
            CreatePermission("Users.View", "View Users", "Users", "View user list and details"),
            CreatePermission("Users.Create", "Create User", "Users", "Create new users"),
            CreatePermission("Users.Edit", "Edit User", "Users", "Edit user information"),
            CreatePermission("Users.Delete", "Delete User", "Users", "Delete users from the system"),
            CreatePermission("Users.ManageRoles", "Manage User Roles", "Users", "Assign/remove roles from users"),
            CreatePermission("Users.ManagePermissions", "Manage User Permissions", "Users", "Assign/remove direct permissions from users"),
            
            // ===== Roles Group =====
            CreatePermission("Roles.View", "View Roles", "Roles", "View role list and details"),
            CreatePermission("Roles.Create", "Create Role", "Roles", "Create new roles"),
            CreatePermission("Roles.Edit", "Edit Role", "Roles", "Edit role information"),
            CreatePermission("Roles.Delete", "Delete Role", "Roles", "Delete roles from the system"),
            CreatePermission("Roles.ManagePermissions", "Manage Role Permissions", "Roles", "Assign/remove permissions from roles"),
            
            // ===== Permissions Group =====
            CreatePermission("Permissions.View", "View Permissions", "Permissions", "View permission list and details"),
            CreatePermission("Permissions.Manage", "Manage Permissions", "Permissions", "Create, edit, and delete permissions"),
            
            // ===== Authorization Group =====
            CreatePermission("Authorization.ViewAudit", "View Authorization Audit", "Authorization", "View authorization audit logs"),
            CreatePermission("Authorization.ManageSettings", "Manage Authorization Settings", "Authorization", "Configure authorization settings"),
            
            // ===== System Group (Admin Only) =====
            CreatePermission("System.ViewLogs", "View System Logs", "System", "View application logs"),
            CreatePermission("System.ManageSettings", "Manage System Settings", "System", "Configure system settings"),
            CreatePermission("System.ViewHealth", "View Health Status", "System", "View system health checks"),
            CreatePermission("System.ManageBackgroundJobs", "Manage Background Jobs", "System", "Start, stop, and monitor background jobs"),
        };
    }

    /// <summary>
    /// Helper method to create a system permission.
    /// All seeded permissions are marked as system permissions.
    /// </summary>
    private Permission CreatePermission(string name, string displayName, string group, string description)
    {
        return new Permission(
            id: Guid.NewGuid(),
            name: name,
            displayName: displayName,
            group: group,
            description: description,
            isSystemPermission: true // All seeded permissions are system permissions
        );
    }
}

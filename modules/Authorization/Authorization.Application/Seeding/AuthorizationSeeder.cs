namespace Authorization.Application.Seeding;

/// <summary>
/// Master seeder for the Authorization module.
/// Orchestrates all authorization data seeding in the correct order.
/// </summary>
public class AuthorizationSeeder
{
    private readonly PermissionSeeder _permissionSeeder;
    private readonly RoleSeeder _roleSeeder;

    public AuthorizationSeeder(
        PermissionSeeder permissionSeeder,
        RoleSeeder roleSeeder)
    {
        _permissionSeeder = permissionSeeder;
        _roleSeeder = roleSeeder;
    }

    /// <summary>
    /// Seeds all authorization data in the correct order.
    /// 1. Permissions (must exist first)
    /// 2. Roles (with permission assignments)
    /// </summary>
    /// <param name="adminUserId">Optional: User ID to record as granting permissions</param>
    public async Task SeedAsync(Guid? adminUserId = null)
    {
        // 1. Seed permissions first
        await _permissionSeeder.SeedAsync();

        // 2. Seed roles with permission assignments
        await _roleSeeder.SeedAsync(adminUserId);
    }
}

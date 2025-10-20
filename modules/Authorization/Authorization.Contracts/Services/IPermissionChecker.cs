namespace Authorization.Contracts.Services;

/// <summary>
/// Service for checking permissions in the authorization system.
/// This is the primary interface for permission checks throughout the application.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// Checks if the current user has the specified permission
    /// </summary>
    /// <param name="permissionName">The permission name to check (e.g., "Users.View")</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> IsGrantedAsync(string permissionName);
    
    /// <summary>
    /// Checks if a specific user has the specified permission
    /// </summary>
    /// <param name="userId">The ID of the user to check</param>
    /// <param name="permissionName">The permission name to check</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> IsGrantedForUserAsync(Guid userId, string permissionName);
    
    /// <summary>
    /// Gets all permissions granted to the current user
    /// </summary>
    /// <returns>List of permission names</returns>
    Task<List<string>> GetGrantedPermissionsAsync();
    
    /// <summary>
    /// Gets all permissions granted to a specific user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>List of permission names</returns>
    Task<List<string>> GetGrantedPermissionsForUserAsync(Guid userId);
    
    /// <summary>
    /// Checks if the current user has all of the specified permissions
    /// </summary>
    /// <param name="permissionNames">The permission names to check</param>
    /// <returns>True if the user has all permissions, false otherwise</returns>
    Task<bool> IsGrantedAllAsync(params string[] permissionNames);
    
    /// <summary>
    /// Checks if the current user has any of the specified permissions
    /// </summary>
    /// <param name="permissionNames">The permission names to check</param>
    /// <returns>True if the user has at least one permission, false otherwise</returns>
    Task<bool> IsGrantedAnyAsync(params string[] permissionNames);
}

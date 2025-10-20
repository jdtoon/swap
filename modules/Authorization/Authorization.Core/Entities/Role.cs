using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace Authorization.Core.Entities;

/// <summary>
/// Represents a role in the authorization system.
/// Roles are groups of permissions that can be assigned to users.
/// </summary>
public class Role : Entity<Guid>
{
    /// <summary>
    /// The unique name of the role (e.g., "Admin", "Manager", "User")
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// Optional description explaining the purpose of this role
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; private set; }
    
    /// <summary>
    /// Whether this role is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;
    
    /// <summary>
    /// Whether this is a system role that cannot be deleted (e.g., Admin, User)
    /// </summary>
    public bool IsSystemRole { get; private set; }
    
    /// <summary>
    /// Whether this role should be automatically assigned to new users
    /// </summary>
    public bool IsDefault { get; private set; }
    
    private readonly List<RolePermission> _rolePermissions = new();
    
    /// <summary>
    /// Navigation property for the many-to-many relationship with permissions
    /// </summary>
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Role()
    {
    }

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="name">Role name</param>
    /// <param name="description">Optional description</param>
    /// <param name="isSystemRole">Whether this is a system role</param>
    /// <param name="isDefault">Whether this role is assigned to new users</param>
    public Role(
        Guid id, 
        string name, 
        string? description = null, 
        bool isSystemRole = false,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        Id = id;
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates role details
    /// </summary>
    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name cannot be empty", nameof(name));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Grants a permission to this role
    /// </summary>
    public void GrantPermission(Guid permissionId, Guid grantedBy)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return; // Permission already granted

        var rolePermission = new RolePermission(
            Guid.NewGuid(),
            Id,
            permissionId,
            grantedBy);
        
        _rolePermissions.Add(rolePermission);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes a permission from this role
    /// </summary>
    public void RevokePermission(Guid permissionId)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets this role as the default role for new users
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the default flag from this role
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the role
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the role
    /// </summary>
    public void Deactivate()
    {
        if (IsSystemRole)
            throw new InvalidOperationException("Cannot deactivate system roles");
        
        if (IsDefault)
            throw new InvalidOperationException("Cannot deactivate the default role");
        
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
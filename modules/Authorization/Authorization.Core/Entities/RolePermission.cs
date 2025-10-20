using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace Authorization.Core.Entities;

/// <summary>
/// Represents the many-to-many relationship between roles and permissions.
/// Tracks which permissions are granted to which roles, along with audit information.
/// </summary>
public class RolePermission : Entity<Guid>
{
    /// <summary>
    /// The ID of the role
    /// </summary>
    [Required]
    public Guid RoleId { get; private set; }
    
    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public Role Role { get; private set; } = null!;
    
    /// <summary>
    /// The ID of the permission
    /// </summary>
    [Required]
    public Guid PermissionId { get; private set; }
    
    /// <summary>
    /// Navigation property to the permission
    /// </summary>
    public Permission Permission { get; private set; } = null!;
    
    /// <summary>
    /// When this permission was granted to the role
    /// </summary>
    public DateTime GrantedAt { get; private set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The ID of the user who granted this permission
    /// </summary>
    [Required]
    public Guid GrantedBy { get; private set; }

    // EF Core requires a parameterless constructor
    private RolePermission()
    {
    }

    /// <summary>
    /// Creates a new role-permission relationship
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="roleId">ID of the role</param>
    /// <param name="permissionId">ID of the permission</param>
    /// <param name="grantedBy">ID of the user granting the permission</param>
    public RolePermission(Guid id, Guid roleId, Guid permissionId, Guid grantedBy)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
        
        if (permissionId == Guid.Empty)
            throw new ArgumentException("Permission ID cannot be empty", nameof(permissionId));
        
        if (grantedBy == Guid.Empty)
            throw new ArgumentException("GrantedBy cannot be empty", nameof(grantedBy));

        Id = id;
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedBy = grantedBy;
        GrantedAt = DateTime.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace Authorization.Core.Entities;

/// <summary>
/// Represents a permission in the authorization system.
/// Permissions are the atomic units of access control (e.g., "Users.View", "Products.Delete").
/// </summary>
public class Permission : Entity<Guid>
{
    /// <summary>
    /// The unique name of the permission (e.g., "Users.View", "Products.Edit").
    /// Convention: {Resource}.{Action}
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// Human-readable display name (e.g., "View Users", "Edit Products")
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string DisplayName { get; private set; } = string.Empty;
    
    /// <summary>
    /// Optional description explaining what this permission grants access to
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; private set; }
    
    /// <summary>
    /// The group this permission belongs to (e.g., "Users", "Products", "Orders")
    /// Used for organizing permissions in the UI
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Group { get; private set; } = string.Empty;
    
    /// <summary>
    /// Whether this permission is currently active and can be granted
    /// </summary>
    public bool IsActive { get; private set; } = true;
    
    /// <summary>
    /// Whether this is a system permission that cannot be deleted
    /// </summary>
    public bool IsSystemPermission { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private Permission()
    {
    }

    /// <summary>
    /// Creates a new permission
    /// </summary>
    /// <param name="id">Unique identifier</param>
    /// <param name="name">Permission name (e.g., "Users.View")</param>
    /// <param name="displayName">Human-readable name</param>
    /// <param name="group">Permission group (e.g., "Users")</param>
    /// <param name="description">Optional description</param>
    /// <param name="isSystemPermission">Whether this is a system permission</param>
    public Permission(
        Guid id, 
        string name, 
        string displayName,
        string group,
        string? description = null, 
        bool isSystemPermission = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));
        
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Group cannot be empty", nameof(group));
        
        // Validate name format (should be {Resource}.{Action})
        if (!name.Contains('.'))
            throw new ArgumentException("Permission name must follow format: Resource.Action", nameof(name));

        Id = id;
        Name = name;
        DisplayName = displayName;
        Group = group;
        Description = description;
        IsSystemPermission = isSystemPermission;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates permission details
    /// </summary>
    public void UpdateDetails(string displayName, string? description)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        DisplayName = displayName;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the permission
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the permission
    /// </summary>
    public void Deactivate()
    {
        if (IsSystemPermission)
            throw new InvalidOperationException("Cannot deactivate system permissions");
        
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
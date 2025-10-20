using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace Audit.Core.Entities;

public class AuditLog : Entity<Guid>
{
    [Required]
    [MaxLength(256)]
    public string Name { get; private set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; private set; }
    
    public bool IsActive { get; private set; } = true;
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private AuditLog()
    {
    }

    public AuditLog(Guid id, string name, string? description = null, bool isActive = true)
    {
        Id = id;
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
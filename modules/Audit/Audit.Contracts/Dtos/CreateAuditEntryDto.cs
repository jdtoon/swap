using System.ComponentModel.DataAnnotations;

namespace Audit.Contracts.Dtos;

public class CreateAuditEntryDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
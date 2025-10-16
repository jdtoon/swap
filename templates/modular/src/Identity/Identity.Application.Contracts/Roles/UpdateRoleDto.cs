using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Roles;

public class UpdateRoleDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}

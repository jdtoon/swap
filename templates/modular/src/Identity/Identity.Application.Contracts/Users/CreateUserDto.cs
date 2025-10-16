using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? FullName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<Guid> RoleIds { get; set; } = new();
}

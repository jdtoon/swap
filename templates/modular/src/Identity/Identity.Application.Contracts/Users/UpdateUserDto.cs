using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Contracts.Users;

public class UpdateUserDto
{
    [MaxLength(256)]
    public string? FullName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool? EmailConfirmed { get; set; }
    public bool? PhoneNumberConfirmed { get; set; }
    
    public List<Guid> RoleIds { get; set; } = new();
}

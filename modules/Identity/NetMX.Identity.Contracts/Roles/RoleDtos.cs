using NetMX.Ddd.Application.Dtos;

namespace NetMX.Identity.Contracts.Roles;

/// <summary>
/// DTO for role information.
/// </summary>
public class RoleDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// DTO for creating a new role.
/// </summary>
public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// DTO for updating role information.
/// </summary>
public class UpdateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

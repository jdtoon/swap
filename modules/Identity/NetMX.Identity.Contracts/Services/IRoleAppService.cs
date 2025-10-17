using NetMX.Ddd.Application.Services;
using NetMX.Identity.Contracts.Roles;

namespace NetMX.Identity.Contracts.Services;

/// <summary>
/// Application service for role management.
/// </summary>
public interface IRoleAppService : IApplicationService
{
    Task<RoleDto?> GetAsync(Guid id);
    Task<RoleDto?> GetByNameAsync(string name);
    Task<List<RoleDto>> GetListAsync();
    Task<RoleDto> CreateAsync(CreateRoleDto input);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto input);
    Task DeleteAsync(Guid id);
}

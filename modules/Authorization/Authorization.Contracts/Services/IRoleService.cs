using Authorization.Contracts.Dtos;

namespace Authorization.Contracts.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync();
    Task<RoleDto?> GetByIdAsync(Guid id);
    Task<RoleDto> CreateAsync(CreateRoleDto dto);
    Task<RoleDto> UpdateAsync(UpdateRoleDto dto);
    Task DeleteAsync(Guid id);
}
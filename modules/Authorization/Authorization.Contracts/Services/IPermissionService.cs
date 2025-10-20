using Authorization.Contracts.Dtos;

namespace Authorization.Contracts.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllAsync();
    Task<PermissionDto?> GetByIdAsync(Guid id);
    Task<PermissionDto> CreateAsync(CreatePermissionDto dto);
    Task<PermissionDto> UpdateAsync(UpdatePermissionDto dto);
    Task DeleteAsync(Guid id);
}
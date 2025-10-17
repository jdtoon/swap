using NetMX.Ddd.Application.Services;
using NetMX.Identity.Contracts.Roles;
using NetMX.Identity.Contracts.Users;

namespace NetMX.Identity.Contracts.Services;

/// <summary>
/// Application service for user management.
/// </summary>
public interface IUserAppService : IApplicationService
{
    Task<UserDto?> GetAsync(Guid id);
    Task<UserDto?> GetByUserNameAsync(string userName);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<List<UserDto>> GetListAsync();
    Task<UserDto> CreateAsync(CreateUserDto input);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input);
    Task DeleteAsync(Guid id);
    Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto input);
    Task<LoginResultDto> LoginAsync(LoginDto input);
    Task<bool> ConfirmEmailAsync(Guid id);
    Task<bool> AddToRoleAsync(Guid userId, Guid roleId);
    Task<bool> RemoveFromRoleAsync(Guid userId, Guid roleId);
    Task<List<RoleDto>> GetRolesAsync(Guid userId);
}

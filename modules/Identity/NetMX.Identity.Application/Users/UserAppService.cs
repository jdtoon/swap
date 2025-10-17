using Microsoft.EntityFrameworkCore;
using NetMX.DependencyInjection;
using NetMX.Ddd.Domain.Repositories;
using NetMX.Identity.Contracts.Roles;
using NetMX.Identity.Contracts.Services;
using NetMX.Identity.Contracts.Users;
using NetMX.Identity.Core.Security;
using NetMX.Identity.Core.Users;

namespace NetMX.Identity.Application.Users;

/// <summary>
/// Application service for user management.
/// </summary>
public class UserAppService : IUserAppService, IScopedDependency
{
    private readonly IQueryableRepository<AppUser, Guid> _userRepository;
    private readonly IQueryableRepository<AppRole, Guid> _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserAppService(
        IQueryableRepository<AppUser, Guid> userRepository,
        IQueryableRepository<AppRole, Guid> roleRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto?> GetAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByUserNameAsync(string userName)
    {
        var queryable = await _userRepository.GetQueryableAsync();
        var user = await queryable
            .FirstOrDefaultAsync(u => u.UserName == userName);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var queryable = await _userRepository.GetQueryableAsync();
        var user = await queryable
            .FirstOrDefaultAsync(u => u.Email == email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetListAsync()
    {
        var users = await _userRepository.GetListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto input)
    {
        // Check if username already exists
        var existingUser = await GetByUserNameAsync(input.UserName);
        if (existingUser != null)
            throw new InvalidOperationException($"User with username '{input.UserName}' already exists.");

        // Check if email already exists
        existingUser = await GetByEmailAsync(input.Email);
        if (existingUser != null)
            throw new InvalidOperationException($"User with email '{input.Email}' already exists.");

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(input.Password);

        // Create user
        var user = new AppUser(
            Guid.NewGuid(),
            input.UserName,
            input.Email,
            passwordHash,
            input.TenantId);

        // Update profile if provided
        if (!string.IsNullOrEmpty(input.FirstName) || !string.IsNullOrEmpty(input.LastName) || !string.IsNullOrEmpty(input.PhoneNumber))
        {
            user.UpdateProfile(input.FirstName, input.LastName, input.PhoneNumber);
        }

        await _userRepository.InsertAsync(user);
        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
            throw new InvalidOperationException($"User with id '{id}' not found.");

        user.UpdateProfile(input.FirstName, input.LastName, input.PhoneNumber);
        await _userRepository.UpdateAsync(user);
        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _userRepository.DeleteAsync(id);
    }

    public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto input)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
            return false;

        // Verify current password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, input.CurrentPassword))
            return false;

        // Hash new password
        var newPasswordHash = _passwordHasher.HashPassword(input.NewPassword);
        user.UpdatePasswordHash(newPasswordHash);
        
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto input)
    {
        var user = await GetByUserNameAsync(input.UserName);
        if (user == null)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Invalid username or password."
            };
        }

        // Get the actual user entity to check password
        var userEntity = await _userRepository.GetAsync(user.Id);
        if (userEntity == null)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Invalid username or password."
            };
        }

        // Check if user is locked out
        if (userEntity.IsLockedOut())
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Account is locked out.",
                IsLockedOut = true
            };
        }

        // Check if user is active
        if (!userEntity.IsActive)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Account is not active."
            };
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(userEntity.PasswordHash, input.Password))
        {
            // Record failed login
            userEntity.RecordFailedLogin();
            
            // Lock out after 5 failed attempts
            if (userEntity.AccessFailedCount >= 5)
            {
                userEntity.LockOut(DateTime.UtcNow.AddMinutes(15));
            }
            
            await _userRepository.UpdateAsync(userEntity);
            
            return new LoginResultDto
            {
                Success = false,
                Message = "Invalid username or password."
            };
        }

        // Reset failed login count on successful login
        userEntity.ResetAccessFailedCount();
        await _userRepository.UpdateAsync(userEntity);

        // Check if 2FA is required
        if (userEntity.TwoFactorEnabled)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Two-factor authentication required.",
                RequiresTwoFactor = true,
                User = MapToDto(userEntity)
            };
        }

        return new LoginResultDto
        {
            Success = true,
            Message = "Login successful.",
            User = MapToDto(userEntity)
        };
    }

    public async Task<bool> ConfirmEmailAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
            return false;

        user.ConfirmEmail();
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> AddToRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _userRepository.GetAsync(userId);
        if (user == null)
            return false;

        var role = await _roleRepository.GetAsync(roleId);
        if (role == null)
            return false;

        user.AddRole(roleId);
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> RemoveFromRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _userRepository.GetAsync(userId);
        if (user == null)
            return false;

        user.RemoveRole(roleId);
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<List<RoleDto>> GetRolesAsync(Guid userId)
    {
        var queryable = await _userRepository.GetQueryableAsync();
        var user = await queryable
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return new List<RoleDto>();

        return user.UserRoles
            .Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                IsSystemRole = ur.Role.IsSystemRole,
                TenantId = ur.Role.TenantId
            })
            .ToList();
    }

    private static UserDto MapToDto(AppUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            TenantId = user.TenantId,
            CreatedAt = DateTime.UtcNow // TODO: Add CreatedAt to domain entity
        };
    }
}

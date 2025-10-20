using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetMX.DependencyInjection;
using NetMX.Identity.Contracts.Roles;
using NetMX.Identity.Contracts.Services;
using NetMX.Identity.Contracts.Users;
using NetMX.Identity.Core.Users;

namespace NetMX.Identity.Application.Users;

/// <summary>
/// Application service for user management.
/// Wraps ASP.NET Core Identity's UserManager with NetMX abstractions.
/// </summary>
public class UserAppService : IUserAppService, IScopedDependency
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public UserAppService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<UserDto?> GetAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByUserNameAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetListAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto input)
    {
        // Create user with Identity's UserManager (handles password hashing)
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = input.UserName,
            Email = input.Email,
            TenantId = input.TenantId,
            IsActive = true,
            EmailConfirmed = false
        };

        // Update profile if provided
        if (!string.IsNullOrEmpty(input.FirstName) || !string.IsNullOrEmpty(input.LastName) || !string.IsNullOrEmpty(input.PhoneNumber))
        {
            user.UpdateProfile(input.FirstName, input.LastName, input.PhoneNumber);
        }

        // UserManager will validate uniqueness and hash password
        var result = await _userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            throw new InvalidOperationException($"User with id '{id}' not found.");

        user.UpdateProfile(input.FirstName, input.LastName, input.PhoneNumber);
        
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to delete user: {errors}");
            }
        }
    }

    public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto input)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return false;

        // UserManager handles password verification and hashing
        var result = await _userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        return result.Succeeded;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto input)
    {
        var user = await _userManager.FindByNameAsync(input.UserName);
        if (user == null)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Invalid username or password."
            };
        }

        // Check if user is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Account is locked out.",
                IsLockedOut = true
            };
        }

        // Check if user is active (custom property)
        if (!user.IsActive)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Account is not active."
            };
        }

        // Verify password using UserManager
        if (!await _userManager.CheckPasswordAsync(user, input.Password))
        {
            // Record failed login using UserManager
            await _userManager.AccessFailedAsync(user);
            
            return new LoginResultDto
            {
                Success = false,
                Message = "Invalid username or password."
            };
        }

        // Reset failed login count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Check if 2FA is required
        if (user.TwoFactorEnabled)
        {
            return new LoginResultDto
            {
                Success = false,
                Message = "Two-factor authentication required.",
                RequiresTwoFactor = true,
                User = MapToDto(user)
            };
        }

        return new LoginResultDto
        {
            Success = true,
            Message = "Login successful.",
            User = MapToDto(user)
        };
    }

    public async Task<bool> ConfirmEmailAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return false;

        // Generate a token and confirm (simplified - normally you'd validate a token from email)
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task<bool> AddToRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return false;

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        return result.Succeeded;
    }

    public async Task<bool> RemoveFromRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
            return false;

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        return result.Succeeded;
    }

    public async Task<List<RoleDto>> GetRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return new List<RoleDto>();

        var roleNames = await _userManager.GetRolesAsync(user);
        var roles = new List<RoleDto>();

        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                roles.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    IsSystemRole = role.IsSystemRole,
                    TenantId = role.TenantId
                });
            }
        }

        return roles;
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

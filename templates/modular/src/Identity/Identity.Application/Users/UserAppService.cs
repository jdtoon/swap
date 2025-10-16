using Identity.Application.Contracts.Users;
using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Application.Services;
using NetMX.Ddd.Domain.Repositories;

namespace Identity.Application.Users;

public class UserAppService : ApplicationService
{
    private readonly IQueryableRepository<AppUser, Guid> _userRepository;
    private readonly IQueryableRepository<AppRole, Guid> _roleRepository;
    private readonly IQueryableRepository<AppUserRole, Guid> _userRoleRepository;

    public UserAppService(
        IQueryableRepository<AppUser, Guid> userRepository,
        IQueryableRepository<AppRole, Guid> roleRepository,
        IQueryableRepository<AppUserRole, Guid> userRoleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<List<UserDto>> GetListAsync()
    {
        var users = await _userRepository.GetQueryableAsync();
        var userRoles = await _userRoleRepository.GetQueryableAsync();
        var roles = await _roleRepository.GetQueryableAsync();

        var result = await (from user in users
                           join ur in userRoles on user.Id equals ur.UserId into userRoleGroup
                           from ur in userRoleGroup.DefaultIfEmpty()
                           join role in roles on ur.RoleId equals role.Id into roleGroup
                           from role in roleGroup.DefaultIfEmpty()
                           group role by user into g
                           select new UserDto
                           {
                               Id = g.Key.Id,
                               Email = g.Key.Email,
                               FullName = g.Key.FullName,
                               PhoneNumber = g.Key.PhoneNumber,
                               EmailConfirmed = g.Key.EmailConfirmed,
                               PhoneNumberConfirmed = g.Key.PhoneNumberConfirmed,
                               IsActive = g.Key.IsActive,
                               LastLoginDate = g.Key.LastLoginDate,
                               RoleNames = g.Where(r => r != null).Select(r => r.Name).ToList()
                           }).ToListAsync();

        return result;
    }

    public async Task<UserDto?> GetAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null) return null;

        var userRoles = await _userRoleRepository.GetQueryableAsync();
        var roles = await _roleRepository.GetQueryableAsync();

        var roleNames = await (from ur in userRoles
                              where ur.UserId == id
                              join role in roles on ur.RoleId equals role.Id
                              select role.Name).ToListAsync();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            IsActive = user.IsActive,
            LastLoginDate = user.LastLoginDate,
            RoleNames = roleNames
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserDto input)
    {
        // Check if email already exists
        var users = await _userRepository.GetQueryableAsync();
        if (await users.AnyAsync(u => u.Email == input.Email))
        {
            throw new InvalidOperationException($"User with email '{input.Email}' already exists.");
        }

        // Create user - NOTE: In production, hash the password properly (e.g., using BCrypt)
        var user = AppUser.Create(
            input.Email,
            input.Password, // In production, this should be hashed!
            input.FullName,
            input.PhoneNumber,
            input.EmailConfirmed,
            input.PhoneNumberConfirmed);

        if (!input.IsActive)
        {
            user.Deactivate();
        }

        await _userRepository.InsertAsync(user);

        // Assign roles
        if (input.RoleIds.Any())
        {
            var roles = await _roleRepository.GetQueryableAsync();
            var validRoleIds = await roles.Where(r => input.RoleIds.Contains(r.Id)).Select(r => r.Id).ToListAsync();

            foreach (var roleId in validRoleIds)
            {
                var userRole = AppUserRole.Create(user.Id, roleId);
                await _userRoleRepository.InsertAsync(userRole);
            }
        }

        var roleNames = input.RoleIds.Any()
            ? await (from r in await _roleRepository.GetQueryableAsync()
                    where input.RoleIds.Contains(r.Id)
                    select r.Name).ToListAsync()
            : new List<string>();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            IsActive = user.IsActive,
            LastLoginDate = user.LastLoginDate,
            RoleNames = roleNames
        };
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with id '{id}' not found.");
        }

        user.UpdateProfile(input.FullName, input.PhoneNumber);

        if (input.EmailConfirmed.HasValue)
        {
            user.EmailConfirmed = input.EmailConfirmed.Value;
        }

        if (input.PhoneNumberConfirmed.HasValue)
        {
            user.PhoneNumberConfirmed = input.PhoneNumberConfirmed.Value;
        }

        await _userRepository.UpdateAsync(user);

        // Update roles
        var existingUserRoles = await _userRoleRepository.GetQueryableAsync();
        var currentUserRoles = await existingUserRoles.Where(ur => ur.UserId == id).ToListAsync();

        // Remove old roles
        foreach (var userRole in currentUserRoles)
        {
            await _userRoleRepository.DeleteAsync(userRole.Id);
        }

        // Add new roles
        if (input.RoleIds.Any())
        {
            var roles = await _roleRepository.GetQueryableAsync();
            var validRoleIds = await roles.Where(r => input.RoleIds.Contains(r.Id)).Select(r => r.Id).ToListAsync();

            foreach (var roleId in validRoleIds)
            {
                var userRole = AppUserRole.Create(user.Id, roleId);
                await _userRoleRepository.InsertAsync(userRole);
            }
        }

        var roleNames = input.RoleIds.Any()
            ? await (from r in await _roleRepository.GetQueryableAsync()
                    where input.RoleIds.Contains(r.Id)
                    select r.Name).ToListAsync()
            : new List<string>();

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            IsActive = user.IsActive,
            LastLoginDate = user.LastLoginDate,
            RoleNames = roleNames
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with id '{id}' not found.");
        }

        // Delete user roles first
        var userRoles = await _userRoleRepository.GetQueryableAsync();
        var userRolesToDelete = await userRoles.Where(ur => ur.UserId == id).ToListAsync();
        
        foreach (var userRole in userRolesToDelete)
        {
            await _userRoleRepository.DeleteAsync(userRole.Id);
        }

        await _userRepository.DeleteAsync(user.Id);
    }

    public async Task ActivateAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with id '{id}' not found.");
        }

        user.Activate();
        await _userRepository.UpdateAsync(user);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with id '{id}' not found.");
        }

        user.Deactivate();
        await _userRepository.UpdateAsync(user);
    }
}

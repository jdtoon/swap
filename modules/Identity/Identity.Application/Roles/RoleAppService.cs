using Identity.Application.Contracts.Roles;
using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Application.Services;
using NetMX.Ddd.Domain.Repositories;

namespace Identity.Application.Roles;

public class RoleAppService : ApplicationService
{
    private readonly IQueryableRepository<AppRole, Guid> _roleRepository;
    private readonly IQueryableRepository<AppUserRole, Guid> _userRoleRepository;

    public RoleAppService(
        IQueryableRepository<AppRole, Guid> roleRepository,
        IQueryableRepository<AppUserRole, Guid> userRoleRepository)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<List<RoleDto>> GetListAsync()
    {
        var roles = await _roleRepository.GetQueryableAsync();
        return await roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole
        }).ToListAsync();
    }

    public async Task<RoleDto?> GetAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null) return null;

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole
        };
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto input)
    {
        // Check if role name already exists
        var roles = await _roleRepository.GetQueryableAsync();
        if (await roles.AnyAsync(r => r.Name == input.Name))
        {
            throw new InvalidOperationException($"Role with name '{input.Name}' already exists.");
        }

        var role = AppRole.Create(input.Name, input.Description);
        await _roleRepository.InsertAsync(role);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole
        };
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto input)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with id '{id}' not found.");
        }

        // Check if role name already exists (excluding current role)
        var roles = await _roleRepository.GetQueryableAsync();
        if (await roles.AnyAsync(r => r.Name == input.Name && r.Id != id))
        {
            throw new InvalidOperationException($"Role with name '{input.Name}' already exists.");
        }

        role.Update(input.Name, input.Description);
        await _roleRepository.UpdateAsync(role);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with id '{id}' not found.");
        }

        // Check if there are users assigned to this role
        var userRoles = await _userRoleRepository.GetQueryableAsync();
        if (await userRoles.AnyAsync(ur => ur.RoleId == id))
        {
            throw new InvalidOperationException($"Cannot delete role '{role.Name}' because it has assigned users.");
        }

        await _roleRepository.DeleteAsync(role.Id);
    }
}

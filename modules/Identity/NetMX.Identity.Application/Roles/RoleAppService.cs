using Microsoft.EntityFrameworkCore;
using NetMX.DependencyInjection;
using NetMX.Ddd.Domain.Repositories;
using NetMX.Identity.Contracts.Roles;
using NetMX.Identity.Contracts.Services;
using NetMX.Identity.Core.Users;

namespace NetMX.Identity.Application.Roles;

/// <summary>
/// Application service for role management.
/// </summary>
public class RoleAppService : IRoleAppService, IScopedDependency
{
    private readonly IQueryableRepository<AppRole, Guid> _roleRepository;

    public RoleAppService(IQueryableRepository<AppRole, Guid> roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleDto?> GetAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        return role == null ? null : MapToDto(role);
    }

    public async Task<RoleDto?> GetByNameAsync(string name)
    {
        var queryable = await _roleRepository.GetQueryableAsync();
        var role = await queryable
            .FirstOrDefaultAsync(r => r.Name == name);
        return role == null ? null : MapToDto(role);
    }

    public async Task<List<RoleDto>> GetListAsync()
    {
        var roles = await _roleRepository.GetListAsync();
        return roles.Select(MapToDto).ToList();
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto input)
    {
        // Check if role already exists
        var existingRole = await GetByNameAsync(input.Name);
        if (existingRole != null)
            throw new InvalidOperationException($"Role with name '{input.Name}' already exists.");

        // Create role
        var role = new AppRole(
            Guid.NewGuid(),
            input.Name,
            input.Description,
            false, // Not a system role
            input.TenantId);

        await _roleRepository.InsertAsync(role);
        return MapToDto(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto input)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
            throw new InvalidOperationException($"Role with id '{id}' not found.");

        // Prevent updating system roles
        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot update system roles.");

        role.UpdateName(input.Name);
        role.UpdateDescription(input.Description);
        
        await _roleRepository.UpdateAsync(role);
        return MapToDto(role);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
            throw new InvalidOperationException($"Role with id '{id}' not found.");

        // Prevent deleting system roles
        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot delete system roles.");

        await _roleRepository.DeleteAsync(id);
    }

    private static RoleDto MapToDto(AppRole role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            TenantId = role.TenantId
        };
    }
}

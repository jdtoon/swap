using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetMX.DependencyInjection;
using NetMX.Identity.Contracts.Roles;
using NetMX.Identity.Contracts.Services;
using NetMX.Identity.Core.Users;

namespace NetMX.Identity.Application.Roles;

/// <summary>
/// Application service for role management.
/// Wraps ASP.NET Core Identity's RoleManager with NetMX abstractions.
/// </summary>
public class RoleAppService : IRoleAppService, IScopedDependency
{
    private readonly RoleManager<AppRole> _roleManager;

    public RoleAppService(RoleManager<AppRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<RoleDto?> GetAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        return role == null ? null : MapToDto(role);
    }

    public async Task<RoleDto?> GetByNameAsync(string name)
    {
        var role = await _roleManager.FindByNameAsync(name);
        return role == null ? null : MapToDto(role);
    }

    public async Task<List<RoleDto>> GetListAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return roles.Select(MapToDto).ToList();
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto input)
    {
        var role = new AppRole(
            Guid.NewGuid(),
            input.Name,
            input.Description,
            false, // Not a system role by default
            input.TenantId);

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role: {errors}");
        }

        return MapToDto(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto input)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            throw new InvalidOperationException($"Role with id '{id}' not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot update system roles.");

        role.UpdateName(input.Name);
        role.UpdateDescription(input.Description);

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update role: {errors}");
        }

        return MapToDto(role);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            throw new InvalidOperationException($"Role with id '{id}' not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot delete system roles.");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete role: {errors}");
        }
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

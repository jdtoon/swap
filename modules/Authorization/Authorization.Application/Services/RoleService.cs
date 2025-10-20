using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Repositories;
using Authorization.Core.Entities;
using Authorization.Contracts.Dtos;
using Authorization.Contracts.Services;

namespace Authorization.Application.Services;

public class RoleService : IRoleService
{
    private readonly IQueryableRepository<Role, Guid> _repository;

    public RoleService(IQueryableRepository<Role, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        var queryable = await _repository.GetQueryableAsync();
        return await queryable
            .Select(e => new RoleDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        if (entity == null) return null;

        return new RoleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var entity = new Role(
            Guid.NewGuid(),
            dto.Name,
            dto.Description,
            dto.IsActive
        );

        await _repository.InsertAsync(entity);

        return new RoleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<RoleDto> UpdateAsync(UpdateRoleDto dto)
    {
        var entity = await _repository.GetAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"Role with ID {dto.Id} not found");

        entity.UpdateDetails(dto.Name, dto.Description, dto.IsActive);
        await _repository.UpdateAsync(entity);

        return new RoleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
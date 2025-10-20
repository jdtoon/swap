using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Repositories;
using Authorization.Core.Entities;
using Authorization.Contracts.Dtos;
using Authorization.Contracts.Services;

namespace Authorization.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IQueryableRepository<Permission, Guid> _repository;

    public PermissionService(IQueryableRepository<Permission, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<List<PermissionDto>> GetAllAsync()
    {
        var queryable = await _repository.GetQueryableAsync();
        return await queryable
            .Select(e => new PermissionDto
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

    public async Task<PermissionDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        if (entity == null) return null;

        return new PermissionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionDto dto)
    {
        var entity = new Permission(
            Guid.NewGuid(),
            dto.Name,
            dto.Description,
            dto.IsActive
        );

        await _repository.InsertAsync(entity);

        return new PermissionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<PermissionDto> UpdateAsync(UpdatePermissionDto dto)
    {
        var entity = await _repository.GetAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"Permission with ID {dto.Id} not found");

        entity.UpdateDetails(dto.Name, dto.Description, dto.IsActive);
        await _repository.UpdateAsync(entity);

        return new PermissionDto
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
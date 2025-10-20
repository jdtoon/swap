using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Repositories;
using Audit.Core.Entities;
using Audit.Contracts.Dtos;
using Audit.Contracts.Services;

namespace Audit.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IQueryableRepository<AuditLog, Guid> _repository;

    public AuditLogService(IQueryableRepository<AuditLog, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<List<AuditLogDto>> GetAllAsync()
    {
        var queryable = await _repository.GetQueryableAsync();
        return await queryable
            .Select(e => new AuditLogDto
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

    public async Task<AuditLogDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        if (entity == null) return null;

        return new AuditLogDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<AuditLogDto> CreateAsync(CreateAuditLogDto dto)
    {
        var entity = new AuditLog(
            Guid.NewGuid(),
            dto.Name,
            dto.Description,
            dto.IsActive
        );

        await _repository.InsertAsync(entity);

        return new AuditLogDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<AuditLogDto> UpdateAsync(UpdateAuditLogDto dto)
    {
        var entity = await _repository.GetAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"AuditLog with ID {dto.Id} not found");

        entity.UpdateDetails(dto.Name, dto.Description, dto.IsActive);
        await _repository.UpdateAsync(entity);

        return new AuditLogDto
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
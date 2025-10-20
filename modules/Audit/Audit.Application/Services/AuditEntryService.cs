using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Repositories;
using Audit.Core.Entities;
using Audit.Contracts.Dtos;
using Audit.Contracts.Services;

namespace Audit.Application.Services;

public class AuditEntryService : IAuditEntryService
{
    private readonly IQueryableRepository<AuditEntry, Guid> _repository;

    public AuditEntryService(IQueryableRepository<AuditEntry, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<List<AuditEntryDto>> GetAllAsync()
    {
        var queryable = await _repository.GetQueryableAsync();
        return await queryable
            .Select(e => new AuditEntryDto
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

    public async Task<AuditEntryDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        if (entity == null) return null;

        return new AuditEntryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<AuditEntryDto> CreateAsync(CreateAuditEntryDto dto)
    {
        var entity = new AuditEntry(
            Guid.NewGuid(),
            dto.Name,
            dto.Description,
            dto.IsActive
        );

        await _repository.InsertAsync(entity);

        return new AuditEntryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<AuditEntryDto> UpdateAsync(UpdateAuditEntryDto dto)
    {
        var entity = await _repository.GetAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"AuditEntry with ID {dto.Id} not found");

        entity.UpdateDetails(dto.Name, dto.Description, dto.IsActive);
        await _repository.UpdateAsync(entity);

        return new AuditEntryDto
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
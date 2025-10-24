namespace Settings.Application.Services;

using Microsoft.EntityFrameworkCore;
using Settings.Contracts.Dtos;
using Settings.Contracts.Services;
using Settings.Core.Entities;
using Settings.Infrastructure.Data;

/// <summary>
/// Service implementation for Setting operations
/// </summary>
public class SettingService : ISettingService
{
    private readonly SettingsDbContext _context;

    public SettingService(SettingsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<SettingDto>> GetAllAsync()
    {
        return await _context.Set<Setting>()
            .Select(x => new SettingDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SettingDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<Setting>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        return new SettingDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<SettingDto> CreateAsync(CreateSettingDto dto)
    {
        var entity = new Setting(Guid.NewGuid());

        _context.Set<Setting>().Add(entity);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve created entity");
    }

    /// <inheritdoc />
    public async Task<SettingDto> UpdateAsync(UpdateSettingDto dto)
    {
        var entity = await _context.Set<Setting>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        if (entity == null)
            throw new InvalidOperationException("Setting not found");


        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated entity");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<Setting>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new InvalidOperationException("Setting not found");

        // Hard delete
        _context.Set<Setting>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}

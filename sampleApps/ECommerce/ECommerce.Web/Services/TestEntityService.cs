namespace ECommerce.Web.Services;

using Microsoft.EntityFrameworkCore;
using ECommerce.Web.Dtos;
using ECommerce.Web.Services;
using ECommerce.Web.Models;

/// <summary>
/// Service implementation for TestEntity operations
/// </summary>
public class TestEntityService : ITestEntityService
{
    private readonly DbContext _context;

    public TestEntityService(DbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<TestEntityDto>> GetAllAsync()
    {
        return await _context.Set<TestEntity>()
            .Select(x => new TestEntityDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TestEntityDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<TestEntity>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        return new TestEntityDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<TestEntityDto> CreateAsync(CreateTestEntityDto dto)
    {
        var entity = new TestEntity(Guid.NewGuid());

        _context.Set<TestEntity>().Add(entity);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve created entity");
    }

    /// <inheritdoc />
    public async Task<TestEntityDto> UpdateAsync(UpdateTestEntityDto dto)
    {
        var entity = await _context.Set<TestEntity>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        if (entity == null)
            throw new InvalidOperationException("TestEntity not found");


        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated entity");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<TestEntity>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new InvalidOperationException("TestEntity not found");

        // Hard delete
        _context.Set<TestEntity>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}

namespace ECommerceDogfood.Web.Services;

using Microsoft.EntityFrameworkCore;
using ECommerceDogfood.Web.Dtos;
using ECommerceDogfood.Web.Services;
using ECommerceDogfood.Web.Models;
using ECommerceDogfood.Web.Data;

/// <summary>
/// Service implementation for Category operations
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _context.Set<Category>()
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<Category>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        return new CategoryDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var entity = new Category(Guid.NewGuid());

        _context.Set<Category>().Add(entity);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve created entity");
    }

    /// <inheritdoc />
    public async Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto)
    {
        var entity = await _context.Set<Category>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        if (entity == null)
            throw new InvalidOperationException("Category not found");


        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated entity");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<Category>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new InvalidOperationException("Category not found");

        // Hard delete
        _context.Set<Category>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}

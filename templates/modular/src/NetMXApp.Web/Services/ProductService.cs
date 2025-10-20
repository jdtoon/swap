using Microsoft.EntityFrameworkCore;
using NetMXApp.Data;
using NetMXApp.Dtos;
using NetMXApp.Models;

namespace NetMXApp.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _context.Set<Product>()
            .Select(e => new ProductDto
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

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<Product>().FindAsync(id);
        if (entity == null) return null;

        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Product>().Add(entity);
        await _context.SaveChangesAsync();

        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<ProductDto> UpdateAsync(UpdateProductDto dto)
    {
        var entity = await _context.Set<Product>().FindAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"Product with ID {dto.Id} not found");

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ProductDto
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
        var entity = await _context.Set<Product>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<Product>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
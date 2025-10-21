namespace ECommerce.Web.Services;

using Microsoft.EntityFrameworkCore;
using ECommerce.Web.Dtos;
using ECommerce.Web.Services;
using ECommerce.Web.Models;

/// <summary>
/// Service implementation for Product operations
/// </summary>
public class ProductService : IProductService
{
    private readonly DbContext _context;

    public ProductService(DbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _context.Set<Product>()
            .Select(x => new ProductDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<Product>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        return new ProductDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var entity = new Product(Guid.NewGuid());

        _context.Set<Product>().Add(entity);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve created entity");
    }

    /// <inheritdoc />
    public async Task<ProductDto> UpdateAsync(UpdateProductDto dto)
    {
        var entity = await _context.Set<Product>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        if (entity == null)
            throw new InvalidOperationException("Product not found");


        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated entity");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<Product>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new InvalidOperationException("Product not found");

        // Hard delete
        _context.Set<Product>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}

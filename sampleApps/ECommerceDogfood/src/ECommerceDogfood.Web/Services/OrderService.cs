namespace ECommerceDogfood.Web.Services;

using Microsoft.EntityFrameworkCore;
using ECommerceDogfood.Web.Dtos;
using ECommerceDogfood.Web.Services;
using ECommerceDogfood.Web.Models;
using ECommerceDogfood.Web.Data;

/// <summary>
/// Service implementation for Order operations
/// </summary>
public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<OrderDto>> GetAllAsync()
    {
        return await _context.Set<Order>()
            .Select(x => new OrderDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        return new OrderDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        var entity = new Order(Guid.NewGuid());

        _context.Set<Order>().Add(entity);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve created entity");
    }

    /// <inheritdoc />
    public async Task<OrderDto> UpdateAsync(UpdateOrderDto dto)
    {
        var entity = await _context.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        if (entity == null)
            throw new InvalidOperationException("Order not found");


        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated entity");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new InvalidOperationException("Order not found");

        // Hard delete
        _context.Set<Order>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}

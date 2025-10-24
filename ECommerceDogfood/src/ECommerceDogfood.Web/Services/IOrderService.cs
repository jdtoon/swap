namespace ECommerceDogfood.Web.Services;

using ECommerceDogfood.Web.Dtos;

/// <summary>
/// Service interface for Order operations
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Gets all Orders
    /// </summary>
    Task<List<OrderDto>> GetAllAsync();

    /// <summary>
    /// Gets Order by ID
    /// </summary>
    Task<OrderDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new Order
    /// </summary>
    Task<OrderDto> CreateAsync(CreateOrderDto dto);

    /// <summary>
    /// Updates an existing Order
    /// </summary>
    Task<OrderDto> UpdateAsync(UpdateOrderDto dto);

    /// <summary>
    /// Deletes Order by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}

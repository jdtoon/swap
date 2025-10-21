namespace ECommerce.Web.Services;

using ECommerce.Web.Dtos;

/// <summary>
/// Service interface for Product operations
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets all Products
    /// </summary>
    Task<List<ProductDto>> GetAllAsync();

    /// <summary>
    /// Gets Product by ID
    /// </summary>
    Task<ProductDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new Product
    /// </summary>
    Task<ProductDto> CreateAsync(CreateProductDto dto);

    /// <summary>
    /// Updates an existing Product
    /// </summary>
    Task<ProductDto> UpdateAsync(UpdateProductDto dto);

    /// <summary>
    /// Deletes Product by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}

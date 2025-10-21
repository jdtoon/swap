namespace ECommerce.Web.Services;

using ECommerce.Web.Dtos;

/// <summary>
/// Service interface for Category operations
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets all Categorys
    /// </summary>
    Task<List<CategoryDto>> GetAllAsync();

    /// <summary>
    /// Gets Category by ID
    /// </summary>
    Task<CategoryDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new Category
    /// </summary>
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);

    /// <summary>
    /// Updates an existing Category
    /// </summary>
    Task<CategoryDto> UpdateAsync(UpdateCategoryDto dto);

    /// <summary>
    /// Deletes Category by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}

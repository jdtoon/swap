namespace ECommerce.Web.Services;

using ECommerce.Web.Dtos;

/// <summary>
/// Service interface for TestEntity operations
/// </summary>
public interface ITestEntityService
{
    /// <summary>
    /// Gets all TestEntitys
    /// </summary>
    Task<List<TestEntityDto>> GetAllAsync();

    /// <summary>
    /// Gets TestEntity by ID
    /// </summary>
    Task<TestEntityDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new TestEntity
    /// </summary>
    Task<TestEntityDto> CreateAsync(CreateTestEntityDto dto);

    /// <summary>
    /// Updates an existing TestEntity
    /// </summary>
    Task<TestEntityDto> UpdateAsync(UpdateTestEntityDto dto);

    /// <summary>
    /// Deletes TestEntity by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}

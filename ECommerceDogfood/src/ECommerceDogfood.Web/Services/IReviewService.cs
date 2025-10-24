namespace ECommerceDogfood.Web.Services;

using ECommerceDogfood.Web.Dtos;

/// <summary>
/// Service interface for Review operations
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Gets all Reviews
    /// </summary>
    Task<List<ReviewDto>> GetAllAsync();

    /// <summary>
    /// Gets Review by ID
    /// </summary>
    Task<ReviewDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new Review
    /// </summary>
    Task<ReviewDto> CreateAsync(CreateReviewDto dto);

    /// <summary>
    /// Updates an existing Review
    /// </summary>
    Task<ReviewDto> UpdateAsync(UpdateReviewDto dto);

    /// <summary>
    /// Deletes Review by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}

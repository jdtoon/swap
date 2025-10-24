namespace ECommerceDogfood.Web.Services;

using Microsoft.EntityFrameworkCore;
using ECommerceDogfood.Web.Dtos;
using ECommerceDogfood.Web.Services;
using ECommerceDogfood.Web.Models;
using ECommerceDogfood.Web.Data;

/// <summary>
/// Service implementation for Review operations
/// </summary>
public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<ReviewDto>> GetAllAsync()
    {
        return await _context.Set<Review>()
            .Select(x => new ReviewDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ReviewDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<Review>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return null;

        return new ReviewDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<ReviewDto> CreateAsync(CreateReviewDto dto)
    {
        var entity = new Review(Guid.NewGuid());

        _context.Set<Review>().Add(entity);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve created entity");
    }

    /// <inheritdoc />
    public async Task<ReviewDto> UpdateAsync(UpdateReviewDto dto)
    {
        var entity = await _context.Set<Review>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        if (entity == null)
            throw new InvalidOperationException("Review not found");


        await _context.SaveChangesAsync();

        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated entity");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<Review>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new InvalidOperationException("Review not found");

        // Hard delete
        _context.Set<Review>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}

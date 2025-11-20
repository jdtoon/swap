using SwapShop.Models;

namespace SwapShop.Services;

public interface IReviewService
{
    void AddReview(Review review);
    IReadOnlyList<Review> GetByProductId(int productId);
}

public class ReviewService : IReviewService
{
    private readonly List<Review> _reviews = new();

    public ReviewService()
    {
        // Seed some data
        _reviews.Add(new Review { Id = 1, ProductId = 1, UserName = "Alice", Rating = 5, Comment = "Great quality!", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        _reviews.Add(new Review { Id = 2, ProductId = 1, UserName = "Bob", Rating = 4, Comment = "Good fit.", CreatedAt = DateTime.UtcNow.AddDays(-1) });
    }

    public void AddReview(Review review)
    {
        review.Id = _reviews.Count + 1;
        review.CreatedAt = DateTime.UtcNow;
        _reviews.Add(review);
    }

    public IReadOnlyList<Review> GetByProductId(int productId)
    {
        return _reviews
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }
}

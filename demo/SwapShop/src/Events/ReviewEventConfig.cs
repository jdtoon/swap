using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Services;

namespace SwapShop.Events;

public class ReviewEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(ReviewEvents.Added)
            .RefreshPartial("reviews-container", "_ReviewList", (ctx, payload) =>
            {
                var review = payload as SwapShop.Models.Review;
                var service = ctx.RequestServices.GetRequiredService<IReviewService>();
                
                // Pass ProductId to view so the form works after refresh
                if (review != null)
                {
                    ctx.Items["ViewData_ProductId"] = review.ProductId; // Helper to pass to view
                    return service.GetByProductId(review.ProductId);
                }
                return new List<SwapShop.Models.Review>();
            })
            .SuccessToast("Review submitted successfully!");
    }
}

using Microsoft.AspNetCore.Mvc;
using Swap.Htmx; // Import extension methods
using Swap.Htmx.Events;
using SwapShop.Events;
using SwapShop.Models;
using SwapShop.Services;

namespace SwapShop.Controllers;

/// <summary>
/// Demonstrates "Composition Over Inheritance" (Phase 1.2).
/// This controller inherits from standard MVC Controller, NOT SwapController.
/// It uses extension methods (this.SwapResponse(), this.SwapEvent()) to access Swap features.
/// </summary>
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public IActionResult List(int productId)
    {
        var reviews = _reviewService.GetByProductId(productId);
        
        // Using extension method .SwapView() on standard Controller
        // This automatically handles partial vs full view based on HX-Request header
        return this.SwapView("_ReviewList", reviews);
    }

    [HttpPost]
    public IActionResult Add(Review review)
    {
        if (!ModelState.IsValid)
        {
            // Simple validation handling for demo
            return BadRequest("Invalid review");
        }

        _reviewService.AddReview(review);

        // DEMO: Using extension method .SwapEvent() on standard Controller
        // This triggers the event chain defined in EventChainConfiguration
        return this.SwapEvent(ReviewEvents.Added, review).Build();
    }
}

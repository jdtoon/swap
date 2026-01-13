using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Stories;
using SwapStateDemo.Models;

namespace SwapStateDemo.Controllers;

/// <summary>
/// dedicated controller for component stories.
/// In a real app, you might put [SwapStory] on existing actions, or have a dedicated controller like this.
/// </summary>
public class StoriesController : SwapController
{
    // ==========================================
    // Components -> Cards
    // ==========================================

    [SwapStory("Product Card (Standard)", "Cards", Description = "Standard product card display", Width = 300, Height = 400)]
    public IActionResult ProductCard()
    {
        // Render just the card partial
        // Note: We're simulating how the main page renders it
        var product = new Product(1, "Premium Headphones", "Electronics", 299.99m, true);
        
        // We need a view for this. In the main app, it's inline in the loop. 
        // Best practice: Extract to _ProductCard.cshtml
        return PartialView("_StoryProductCard", product);
    }

    [SwapStory("Product Card (Out of Stock)", "Cards", Width = 300, Height = 400)]
    public IActionResult ProductCardOutOfStock()
    {
        var product = new Product(2, "Sold Out Item", "Electronics", 49.99m, false);
        return PartialView("_StoryProductCard", product);
    }

    // ==========================================
    // Components -> Dashboard
    // ==========================================

    [SwapStory("Dashboard Stat Card", "Dashboard", Width = 300, Height = 150)]
    public IActionResult StatCard()
    {
        var card = new DashboardCard 
        { 
            Id = 1, 
            Title = "Total Revenue", 
            Summary = "$1,234,567", 
            Icon = "💰" 
        };
        return PartialView("_StoryStatCard", card);
    }

    // ==========================================
    // Components -> Interactive
    // ==========================================

    [SwapStory("Self-Contained Counter", "Interactive", Description = "A counter that updates itself via HTMX")]
    public IActionResult Counter()
    {
        // For stories, we can return self-contained HTMX snippets
        return Content(@"
            <div id='counter-demo' style='padding: 20px; border: 1px solid #ddd; text-align: center;'>
                <h3>Counter: <span id='count'>0</span></h3>
                <button class='btn' onclick='document.getElementById(""count"").innerText++'>
                    Increment (Client-side simulation)
                </button>
            </div>
        ", "text/html");
    }
}

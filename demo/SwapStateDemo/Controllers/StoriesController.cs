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
        var product = new Product(1, "Premium Headphones", "Electronics", 299.99m, true);
        // Use View with _StoryLayout to ensure HTMX is loaded
        ViewData["Title"] = "Product Card";
        return View("_StoryProductCard", product);
    }

    [SwapStory("Product Card (Out of Stock)", "Cards", Width = 300, Height = 400)]
    public IActionResult ProductCardOutOfStock()
    {
        var product = new Product(2, "Sold Out Item", "Electronics", 49.99m, false);
        return View("_StoryProductCard", product);
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
        return View("_StoryStatCard", card);
    }

    // ==========================================
    // Components -> Interactive
    // ==========================================

    [SwapStory("Self-Contained Counter", "Interactive", Description = "A counter that updates itself via HTMX")]
    public IActionResult Counter()
    {
        return Content(@"
            <!DOCTYPE html>
            <html>
            <head>
                <script src='https://unpkg.com/htmx.org@1.9.10'></script>
            </head>
            <body>
                <div id='counter-demo' style='padding: 20px; border: 1px solid #ddd; text-align: center;'>
                    <h3>Counter: <span id='count'>0</span></h3>
                    <button class='btn' onclick='document.getElementById(""count"").innerText++'>
                        Increment (Client-side simulation)
                    </button>
                </div>
            </body>
            </html>
        ", "text/html");
    }

    [SwapStory("Error Handling (Crash)", "Interactive", Description = "Demonstrates how exceptions are caught result in a toast")]
    public IActionResult ErrorDemo()
    {
        // Must return a full page with HTMX script for hx-post to work
        return Content(@"
            <!DOCTYPE html>
            <html>
            <head>
                <script src='https://unpkg.com/htmx.org@1.9.10'></script>
                <style>body { font-family: sans-serif; padding: 2rem; }</style>
            </head>
            <body>
                <div style='padding: 20px; text-align: center; border: 1px dashed #ccc;'>
                    <h3>Crash Test</h3>
                    <p>Click below to cause a server-side exception.</p>
                    <button hx-post='/Stories/Crash' hx-swap='none' style='padding: 10px 20px; background: #dc3545; color: white; border: none; border-radius: 4px; cursor: pointer;'>
                        💥 Cause Crash
                    </button>
                    <div id='swap-error-toast'></div>
                </div>
            </body>
            </html>
        ", "text/html");
    }

    [HttpPost]
    public IActionResult Crash()
    {
        throw new Exception("This is a simulated crash! It should appear in a toast.");
    }}

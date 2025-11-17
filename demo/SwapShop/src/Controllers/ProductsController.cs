using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Events;
using SwapShop.Services;
using SwapShop.Views;

namespace SwapShop.Controllers;

/// <summary>
/// Product browsing controller - demonstrates all three Swap.Htmx tiers
/// </summary>
public class ProductsController : SwapController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Full page view - standard MVC
    /// </summary>
    public IActionResult Index(string? search = null)
    {
        var products = _productService.Search(search);
        ViewData["SearchQuery"] = search;
        return SwapView(products);
    }

    /// <summary>
    /// Tier 1: SwapView - Simple partial refresh
    /// </summary>
    public IActionResult Grid(string? search = null)
    {
        var products = _productService.Search(search);
        return SwapView(ProductViews.Grid, products);
    }

    /// <summary>
    /// Tier 1: SwapView - Simple partial for product count
    /// </summary>
    public IActionResult Count()
    {
        var count = _productService.GetProductCount();
        return SwapView(ProductViews.Count, count);
    }

    /// <summary>
    /// Tier 2: SwapResponse - Coordinated multi-partial response
    /// When searching, we want to update both the grid AND the count
    /// Returns the grid as main content with count as OOB update
    /// </summary>
    public IActionResult Search(string? query = null)
    {
        var products = _productService.Search(query);
        var count = products.Count;

        return SwapResponse()
            .WithView(ProductViews.Grid, products)
            .AlsoUpdate(ProductElements.Count, ProductViews.Count, count)
            .Build();
    }

    /// <summary>
    /// Tier 3: SwapEvent - Event-driven response with event chains
    /// The view is configured in Program.cs event chains
    /// </summary>
    public IActionResult Details(int id)
    {
        var product = _productService.GetById(id);
        if (product == null)
        {
            return NotFound();
        }

        // Event chain configured in Program.cs will handle the view rendering
        return SwapEvent(ProductEvents.Viewed, product).Build();
    }

    /// <summary>
    /// Tier 3: SwapEvent - Stock check that could trigger low stock alerts
    /// </summary>
    public IActionResult StockBadge(int id)
    {
        var product = _productService.GetById(id);
        if (product == null)
        {
            return NotFound();
        }

        var eventKey = product.Stock <= 5
            ? ProductEvents.LowStock
            : ProductEvents.StockChecked;

        return SwapEvent(eventKey, product).Build();
    }

    /// <summary>
    /// OLD-STYLE DEMO: Event-driven update
    /// This demonstrates the event-driven approach where the view updates
    /// are handled by event chains configured in EventChainConfiguration.cs
    /// Compare this to the manual SwapResponse approach above
    /// </summary>
    public IActionResult QuickView(int id)
    {
        var product = _productService.GetById(id);
        if (product == null)
        {
            return NotFound();
        }

        // Trigger ProductViewed event - the event chain will handle the UI updates
        return SwapEvent(ProductEvents.Viewed, product).Build();
    }
}


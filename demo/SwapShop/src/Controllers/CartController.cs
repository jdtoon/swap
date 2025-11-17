using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapShop.Events;
using SwapShop.Services;
using SwapShop.Views;

namespace SwapShop.Controllers;

/// <summary>
/// Shopping cart controller - demonstrates event-driven cart updates
/// </summary>
public class CartController : SwapController
{
    private readonly ICartService _cartService;
    private readonly IProductService _productService;

    public CartController(ICartService cartService, IProductService productService)
    {
        _cartService = cartService;
        _productService = productService;
    }

    private string SessionId => HttpContext.Session.Id;

    /// <summary>
    /// Full cart page
    /// </summary>
    public IActionResult Index()
    {
        var cart = _cartService.GetCart(SessionId);
        return SwapView(cart);
    }

    /// <summary>
    /// Tier 1: SwapView - Simple cart badge refresh
    /// </summary>
    public IActionResult Badge()
    {
        var count = _cartService.GetItemCount(SessionId);
        return SwapView(CartViews.Badge, count);
    }

    /// <summary>
    /// Tier 3: SwapEvent - Add item to cart with coordinated updates
    /// This demonstrates the power of event chains - single action triggers multiple UI updates
    /// Event chain configured in Program.cs handles all the UI updates
    /// </summary>
    [HttpPost]
    public IActionResult AddItem(int productId, int quantity = 1)
    {
        var product = _productService.GetById(productId);
        if (product == null || product.Stock < quantity)
        {
            // Trigger failure event via response header
            Response.HxTrigger(CartEvents.AddFailed.Name);
            return Content("", "text/html");
        }

        _cartService.AddItem(SessionId, productId, quantity);
        
        // Trigger success event - event chain will handle UI updates
        Response.HxTrigger(CartEvents.ItemAdded.Name);
        return Content("", "text/html");
    }

    /// <summary>
    /// Tier 3: SwapEvent - Update item quantity with validation
    /// </summary>
    [HttpPut]
    public IActionResult UpdateQuantity(int productId, int quantity)
    {
        var product = _productService.GetById(productId);
        if (product == null || quantity > product.Stock)
        {
            Response.HxTrigger(CartEvents.UpdateFailed.Name);
            return Content("", "text/html");
        }

        _cartService.UpdateQuantity(SessionId, productId, quantity);
        
        Response.HxTrigger(CartEvents.QuantityUpdated.Name);
        return Content("", "text/html");
    }

    /// <summary>
    /// Tier 3: SwapEvent - Remove item from cart
    /// </summary>
    [HttpDelete]
    public IActionResult RemoveItem(int productId)
    {
        _cartService.RemoveItem(SessionId, productId);
        
        Response.HxTrigger(CartEvents.ItemRemoved.Name);
        return Content("", "text/html");
    }

    /// <summary>
    /// Tier 3: SwapEvent - Clear entire cart
    /// </summary>
    [HttpPost]
    public IActionResult Clear()
    {
        _cartService.ClearCart(SessionId);
        
        Response.HxTrigger(CartEvents.Cleared.Name);
        return Content("", "text/html");
    }

    /// <summary>
    /// Tier 2: SwapResponse - Mini cart dropdown
    /// Returns multiple partials for cart summary display
    /// </summary>
}

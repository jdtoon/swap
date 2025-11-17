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

    private string SessionId
    {
        get
        {
            // Use the new GetOrInitializeSessionId helper which automatically
            // triggers session cookie persistence on first use
            return GetOrInitializeSessionId();
        }
    }

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
            return SwapEvent(CartEvents.AddFailed, new { ProductId = productId, Reason = "Product not available" }).Build();
        }

        var sessionId = SessionId;
        _cartService.AddItem(sessionId, productId, quantity);
        var cart = _cartService.GetCart(sessionId);
        var itemCount = _cartService.GetItemCount(sessionId);

        // Debug logging
        Console.WriteLine($"[AddItem] SessionId: {sessionId}, ProductId: {productId}, Cart Items: {cart.Items.Count}, Item Count: {itemCount}");

        return SwapEvent(CartEvents.ItemAdded, cart).Build();
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
            return SwapEvent(CartEvents.UpdateFailed, new { ProductId = productId, RequestedQuantity = quantity }).Build();
        }

        _cartService.UpdateQuantity(SessionId, productId, quantity);
        var cart = _cartService.GetCart(SessionId);

        return SwapEvent(CartEvents.QuantityUpdated, cart).Build();
    }

    /// <summary>
    /// Tier 3: SwapEvent - Remove item from cart
    /// </summary>
    [HttpDelete]
    public IActionResult RemoveItem(int productId)
    {
        _cartService.RemoveItem(SessionId, productId);
        var cart = _cartService.GetCart(SessionId);

        return SwapEvent(CartEvents.ItemRemoved, cart).Build();
    }

    /// <summary>
    /// Tier 3: SwapEvent - Clear entire cart
    /// </summary>
    [HttpPost]
    public IActionResult Clear()
    {
        _cartService.ClearCart(SessionId);

        return SwapEvent(CartEvents.Cleared).Build();
    }

    /// <summary>
    /// Tier 2: SwapResponse - Mini cart dropdown
    /// Returns multiple partials for cart summary display
    /// </summary>
}

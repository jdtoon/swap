using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Events;
using SwapShop.Events;
using SwapShop.Services;
using SwapShop.Models;

namespace SwapShop.Controllers;

/// <summary>
/// Checkout and order management controller
/// </summary>
public class CheckoutController : SwapController
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CheckoutController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    private string SessionId => HttpContext.Session.Id;

    /// <summary>
    /// Checkout page
    /// </summary>
    public IActionResult Index()
    {
        var cart = _cartService.GetCart(SessionId);
        if (!cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        return View(cart);
    }

    /// <summary>
    /// Tier 3: SwapEvent - Process order with comprehensive event handling
    /// Success triggers order creation, cart clearing, and success notifications
    /// Event chains configured in Program.cs
    /// </summary>
    [HttpPost]
    public IActionResult PlaceOrder()
    {
        var cart = _cartService.GetCart(SessionId);

        if (!cart.Items.Any())
        {
            return SwapEvent(OrderEvents.Failed, new { Reason = "Cart is empty" }).Build();
        }

        try
        {
            var order = _orderService.CreateOrder(cart);
            _cartService.ClearCart(SessionId);

            return SwapEvent(OrderEvents.Created, order).Build();
        }
        catch (InvalidOperationException ex)
        {
            return SwapEvent(OrderEvents.Failed, new { Reason = ex.Message }).Build();
        }
    }

    /// <summary>
    /// Order confirmation page
    /// </summary>
    public IActionResult Confirmation(int orderId)
    {
        var order = _orderService.GetById(orderId);
        if (order == null)
        {
            return NotFound();
        }

        // Show confirmation
        return SwapView(order);
    }
}

using EcomApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomApp.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    
    public CheckoutController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }
    
    private string GetSessionId()
    {
        return HttpContext.Session.GetString("CartSessionId") ?? "";
    }
    
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
        
        if (!cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }
        
        ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
        return View(cart);
    }
    
    [HttpPost]
    public async Task<IActionResult> Process(string customerName, string customerEmail, string shippingAddress)
    {
        if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(customerEmail) || string.IsNullOrEmpty(shippingAddress))
        {
            ModelState.AddModelError("", "All fields are required");
            var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
            ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
            return View("Index", cart);
        }
        
        try
        {
            var order = await _orderService.CreateOrderAsync(GetSessionId(), customerName, customerEmail, shippingAddress);
            return RedirectToAction("Confirmation", new { id = order.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
            ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
            return View("Index", cart);
        }
    }
    
    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        
        return View(order);
    }
}

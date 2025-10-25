using EcomApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomApp.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IProductService _productService;
    
    public CartController(ICartService cartService, IProductService productService)
    {
        _cartService = cartService;
        _productService = productService;
    }
    
    private string GetSessionId()
    {
        var sessionId = HttpContext.Session.GetString("CartSessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("CartSessionId", sessionId);
        }
        return sessionId;
    }
    
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
        ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
        return View(cart);
    }
    
    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        await _cartService.AddItemAsync(GetSessionId(), productId, quantity);
        
        var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
        ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
        
        // Return cart count for header update
        Response.Headers["HX-Trigger"] = "cartUpdated";
        
        return PartialView("_CartItems", cart);
    }
    
    [HttpDelete]
    public async Task<IActionResult> Remove(int id)
    {
        await _cartService.RemoveItemAsync(GetSessionId(), id);
        
        var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
        ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
        
        Response.Headers["HX-Trigger"] = "cartUpdated";
        
        return PartialView("_CartItems", cart);
    }
    
    [HttpPatch]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity)
    {
        await _cartService.UpdateQuantityAsync(GetSessionId(), id, quantity);
        
        var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
        ViewBag.Total = await _cartService.GetTotalAsync(GetSessionId());
        
        Response.Headers["HX-Trigger"] = "cartUpdated";
        
        return PartialView("_CartItems", cart);
    }
    
    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var cart = await _cartService.GetOrCreateCartAsync(GetSessionId());
        var count = cart.Items.Sum(i => i.Quantity);
        return Content(count.ToString());
    }
}

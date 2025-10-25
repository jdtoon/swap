using EcomApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomApp.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    
    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    public async Task<IActionResult> Index()
    {
        var orders = await _orderService.GetAllAsync();
        return View(orders);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        
        return View(order);
    }
}

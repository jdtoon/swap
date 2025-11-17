using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Events;
using SwapShop.Events;
using SwapShop.Models;
using SwapShop.Services;
using SwapShop.Views;

namespace SwapShop.Controllers;

/// <summary>
/// Order history and tracking controller
/// </summary>
public class OrdersController : SwapController
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private string SessionId => HttpContext.Session.Id;

    /// <summary>
    /// User's order history
    /// </summary>
    public IActionResult Index()
    {
        var orders = _orderService.GetBySessionId(SessionId);
        return View(orders);
    }

    /// <summary>
    /// Order details
    /// </summary>
    public IActionResult Details(int id)
    {
        var order = _orderService.GetById(id);
        if (order == null || order.SessionId != SessionId)
        {
            return NotFound();
        }

        return View(order);
    }

    /// <summary>
    /// Tier 1: SwapView - Order status badge
    /// </summary>
    public IActionResult Status(int id)
    {
        var order = _orderService.GetById(id);
        if (order == null)
        {
            return NotFound();
        }

        return SwapView(OrderViews.Status, order);
    }

    /// <summary>
    /// Tier 3: SwapEvent - Simulated status update
    /// In a real app, this might be triggered by a background process
    /// Here we'll simulate order progression for demo purposes
    /// </summary>
    [HttpPost]
    public IActionResult SimulateProgress(int id)
    {
        var order = _orderService.GetById(id);
        if (order == null)
        {
            return NotFound();
        }

        // Simulate progression through order statuses
        var nextStatus = order.Status switch
        {
            OrderStatus.Pending => OrderStatus.Processing,
            OrderStatus.Processing => OrderStatus.Shipped,
            OrderStatus.Shipped => OrderStatus.Delivered,
            OrderStatus.Delivered => OrderStatus.Delivered, // Already complete
            _ => order.Status
        };

        if (nextStatus != order.Status)
        {
            _orderService.UpdateStatus(id, nextStatus);
            
            var eventKey = nextStatus switch
            {
                OrderStatus.Processing => OrderEvents.Processing,
                OrderStatus.Shipped => OrderEvents.Shipped,
                OrderStatus.Delivered => OrderEvents.Delivered,
                _ => OrderEvents.StatusChanged
            };

            return SwapEvent(eventKey, order).Build();
        }

        return SwapView(OrderViews.Status, order);
    }
}

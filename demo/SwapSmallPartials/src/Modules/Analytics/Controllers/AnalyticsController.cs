using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using SwapSmallPartials.Modules.Analytics.Events;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Controllers;

public class AnalyticsController : SwapController
{
    private readonly AnalyticsState _state;
    
    private static readonly string[] _firstNames = { "John", "Jane", "Mike", "Sarah", "David", "Emily", "Chris", "Lisa", "Tom", "Anna" };
    private static readonly string[] _lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
    private static readonly string[] _regions = { "Northeast", "Southeast", "Midwest", "Southwest", "West", "Northwest", "Central", "Gulf" };
    
    public AnalyticsController(AnalyticsState state)
    {
        _state = state;
    }
    
    public IActionResult Index()
    {
        return View(_state);
    }
    
    [HttpPost]
    public IActionResult SimulatePurchase()
    {
        // Randomly select a product
        var product = _state.Products[Random.Shared.Next(_state.Products.Count)];
        
        // Skip if out of stock
        if (product.Stock == 0)
        {
            return SwapResponse()
                .WithWarningToast("Product out of stock")
                .Build();
        }
        
        // Generate random customer
        var firstName = _firstNames[Random.Shared.Next(_firstNames.Length)];
        var lastName = _lastNames[Random.Shared.Next(_lastNames.Length)];
        var customerName = $"{firstName} {lastName[0]}.";
        
        // Random customer attributes
        var isNewCustomer = Random.Shared.Next(100) < 30; // 30% new customers
        var isVip = Random.Shared.Next(100) < 15; // 15% VIP
        
        // Random region
        var region = _regions[Random.Shared.Next(_regions.Length)];
        
        // Create event
        var evt = new PurchaseCompletedEvent
        {
            ProductId = product.Id,
            Region = region,
            CustomerName = customerName,
            IsNewCustomer = isNewCustomer,
            IsVip = isVip
        };
        
        // Process the purchase in state
        _state.ProcessPurchase(evt.ProductId, evt.Region, evt.CustomerName, evt.IsNewCustomer, evt.IsVip);
        
        // Fire the event - all distributed handlers will respond
        return SwapEvent(AnalyticsEvents.Purchase.Completed, evt)
            .Build();
    }
    
    [HttpPost]
    public IActionResult SimulateCartAbandonment()
    {
        _state.ProcessCartAbandonment();
        
        return SwapResponse()
            .AlsoUpdate("partial-cart-abandonment", "_CartAbandonment", _state)
            .WithInfoToast("Cart abandoned")
            .Build();
    }
    
    [HttpPost]
    public IActionResult RestockAll()
    {
        _state.RestockAll();
        
        var response = SwapResponse();
        
        // Update all product cards
        foreach (var product in _state.Products)
        {
            response.AlsoUpdate($"partial-product-{product.Id}", "_ProductCard", product);
        }
        
        // Update inventory alerts
        response.AlsoUpdate("partial-inventory-alerts", "_InventoryAlerts", _state);
        
        return response.WithSuccessToast("All products restocked")
            .Build();
    }
    
    [HttpPost]
    public IActionResult AdvanceHour()
    {
        _state.AdvanceHour();
        
        var response = SwapResponse();
        
        // Update all hour bars
        for (int i = 0; i < 24; i++)
        {
            response.AlsoUpdate($"partial-hour-{i}", "_HourBar", new { Hour = i, Revenue = _state.HourlySales[i] });
        }
        
        return response.WithInfoToast($"Advanced to hour {_state.CurrentHour}")
            .Build();
    }
}

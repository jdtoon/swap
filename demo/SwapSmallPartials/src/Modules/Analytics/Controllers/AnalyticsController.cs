using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using SwapSmallPartials.Modules.Analytics.Events;
using SwapSmallPartials.Modules.Analytics.Models;

namespace SwapSmallPartials.Modules.Analytics.Controllers;

/// <summary>
/// Analytics Controller - Demonstrates event-driven UI orchestration
/// 
/// KEY PATTERN: Controllers fire events, handlers update partials.
/// The controller doesn't know which partials need updating—handlers decide.
/// This keeps controllers thin and makes adding new features trivial (just add a handler).
/// </summary>
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
        return SwapView(_state);
    }
    
    /// <summary>
    /// Simulates a customer purchase—fires one event, updates 15-20 partials.
    /// 
    /// PATTERN SHOWCASE:
    /// 1. Controller updates business state (_state.ProcessPurchase)
    /// 2. Controller fires typed event (SwapEvent)
    /// 3. All registered handlers respond automatically
    /// 4. Each handler updates ONE specific partial via OOB swap
    /// 5. Server returns ONE response with all updates embedded
    /// 6. HTMX swaps each partial on the client (zero JS written)
    /// 
    /// COMPARE TO REACT:
    /// React would dispatch action → reducer updates → 60 components re-render (only 15 changed).
    /// Here: 15 handlers run → 15 partials update. Surgical precision.
    /// </summary>
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
        
        // Create typed event payload
        var evt = new PurchaseCompletedEvent
        {
            ProductId = product.Id,
            Region = region,
            CustomerName = customerName,
            IsNewCustomer = isNewCustomer,
            IsVip = isVip
        };
        
        // STEP 1: Update server-side state
        // This is the source of truth. All state lives on the server.
        // No client/server synchronization complexity like React useQuery/SWR/Apollo.
        _state.ProcessPurchase(evt.ProductId, evt.Region, evt.CustomerName, evt.IsNewCustomer, evt.IsVip);
        
        // STEP 2: Fire the event
        // SwapEvent() publishes the event to all registered handlers.
        // The controller doesn't know what will update—handlers decide independently.
        // 
        // This ONE line triggers ~15 handlers:
        //   • RevenueTodayHandler → updates #partial-revenue-today
        //   • OrdersCountHandler → updates #partial-orders-count
        //   • ProductCardHandler → updates #partial-product-{id}
        //   • CategoryCardHandler → updates #partial-category-{name}
        //   • RegionCardHandler → updates #partial-region-{region}
        //   • ActivityFeedHandler → updates #partial-activity-feed
        //   • (and 10 more...)
        //
        // All OOB swaps are collected by SwapResponseBuilder and returned in ONE HTTP response.
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

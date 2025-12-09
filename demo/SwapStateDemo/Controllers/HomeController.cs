using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.State;
using SwapStateDemo.Models;

namespace SwapStateDemo.Controllers;

public class HomeController : SwapController
{
    private static readonly List<Product> Products =
    [
        new(1, "Laptop", "Electronics", 999.99m, true),
        new(2, "Mouse", "Electronics", 29.99m, true),
        new(3, "Keyboard", "Electronics", 79.99m, false),
        new(4, "Chair", "Furniture", 299.99m, true),
        new(5, "Desk", "Furniture", 499.99m, true),
        new(6, "Lamp", "Furniture", 49.99m, false),
        new(7, "Notebook", "Office", 4.99m, true),
        new(8, "Pen", "Office", 1.99m, true),
        new(9, "Monitor", "Electronics", 349.99m, true),
        new(10, "Webcam", "Electronics", 89.99m, false),
    ];

    public IActionResult Index()
    {
        var state = new ProductFilterState();
        var (products, totalCount) = FilterProducts(state);
        
        return SwapView(new ProductViewModel
        {
            State = state,
            Products = products,
            TotalCount = totalCount
        });
    }

    [HttpPost]
    public IActionResult Filter([FromSwapState] ProductFilterState state)
    {
        Console.WriteLine($"[Filter] Category={state.Category}, Page={state.Page}, Search={state.Search}, SortBy={state.SortBy}, SortDesc={state.SortDesc}, InStockOnly={state.InStockOnly}, MinPrice={state.MinPrice}, MaxPrice={state.MaxPrice}");
        
        var (products, totalCount) = FilterProducts(state);
        
        // Return _FilterContent - swaps entire filter area so UI updates correctly
        return PartialView("_FilterContent", new ProductViewModel
        {
            State = state,
            Products = products,
            TotalCount = totalCount
        });
    }

    private (List<Product> Items, int TotalCount) FilterProducts(ProductFilterState state)
    {
        var query = Products.AsEnumerable();
        
        // Category filter
        if (state.Category != "all")
            query = query.Where(p => p.Category.Equals(state.Category, StringComparison.OrdinalIgnoreCase));
        
        // Search filter
        if (!string.IsNullOrEmpty(state.Search))
            query = query.Where(p => p.Name.Contains(state.Search, StringComparison.OrdinalIgnoreCase));
        
        // In stock filter
        if (state.InStockOnly)
            query = query.Where(p => p.InStock);
        
        // Price range filters
        if (state.MinPrice.HasValue)
            query = query.Where(p => p.Price >= state.MinPrice.Value);
        if (state.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= state.MaxPrice.Value);
        
        // Sorting
        query = (state.SortBy, state.SortDesc) switch
        {
            ("price", false) => query.OrderBy(p => p.Price),
            ("price", true) => query.OrderByDescending(p => p.Price),
            ("name", true) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };
        
        var totalCount = query.Count();
        var items = query.Skip((state.Page - 1) * state.PageSize).Take(state.PageSize).ToList();
        
        return (items, totalCount);
    }

    // ==========================================
    // WIZARD - 5 Step Test
    // ==========================================
    
    public IActionResult Wizard()
    {
        return SwapView(new WizardViewModel { State = new WizardState() });
    }

    [HttpPost]
    public IActionResult WizardStep([FromSwapState] WizardState state, int goToStep)
    {
        Console.WriteLine($"[Wizard] Step={state.Step} -> {goToStep}");
        Console.WriteLine($"  Personal: {state.FirstName} {state.LastName} ({state.Email})");
        Console.WriteLine($"  Address: {state.Street}, {state.City} {state.PostalCode}");
        Console.WriteLine($"  Prefs: {state.ContactMethod}, Newsletter={state.Newsletter}, Freq={state.Frequency}");
        Console.WriteLine($"  Payment: {state.PaymentMethod}, Card={state.CardType}");
        
        state.Step = goToStep;
        
        // Return _WizardContent - swaps entire wizard area so progress steps & debug update
        return PartialView("_WizardContent", new WizardViewModel { State = state });
    }

    // ==========================================
    // DASHBOARD - OOB State Updates Demo
    // ==========================================
    
    private static readonly List<DashboardCard> DashboardCards =
    [
        new() { Id = 1, Title = "Sales", Summary = "$12,450", Details = "Up 23% from last month. Top product: Laptop. Best region: West Coast.", Icon = "💰" },
        new() { Id = 2, Title = "Users", Summary = "1,234", Details = "45 new signups today. Retention rate: 89%. Most active: Premium tier.", Icon = "👥" },
        new() { Id = 3, Title = "Orders", Summary = "89", Details = "12 pending fulfillment. Average order value: $156. Express shipping: 34%.", Icon = "📦" },
        new() { Id = 4, Title = "Support", Summary = "7 tickets", Details = "Average response time: 2.3 hours. Satisfaction: 94%. Top issue: Shipping.", Icon = "🎫" },
    ];

    public IActionResult Dashboard()
    {
        var state = new DashboardState();
        return SwapView(new DashboardViewModel 
        { 
            State = state, 
            Cards = DashboardCards 
        });
    }

    [HttpPost]
    public IActionResult ToggleCard([FromSwapState] DashboardState state, int cardId)
    {
        Console.WriteLine($"[Dashboard] Toggle card {cardId}, ExpandedCards was: '{state.ExpandedCards}', ClickCount: {state.ClickCount}");
        
        state.ToggleExpanded(cardId);
        state.ClickCount++;
        
        Console.WriteLine($"[Dashboard] ExpandedCards now: '{state.ExpandedCards}', ClickCount: {state.ClickCount}");
        
        var card = DashboardCards.First(c => c.Id == cardId);
        
        // OOB UPDATE: Swap just the card, but ALSO update the state container
        return this.SwapResponse()
            .WithView("_DashboardCard", new DashboardViewModel 
            { 
                State = state, 
                Cards = DashboardCards,
                SelectedCard = card
            })
            .WithState(state)  // <-- OOB update to state container
            .Build();
    }

    [HttpPost]
    public IActionResult SelectCard([FromSwapState] DashboardState state, int cardId)
    {
        Console.WriteLine($"[Dashboard] Select card {cardId}, was: {state.SelectedCardId}");
        
        state.SelectedCardId = cardId;
        state.ClickCount++;
        
        var card = DashboardCards.First(c => c.Id == cardId);
        
        // OOB UPDATE: Swap just the detail panel, but ALSO update the state container
        return this.SwapResponse()
            .WithView("_DashboardDetail", new DashboardViewModel 
            { 
                State = state, 
                Cards = DashboardCards,
                SelectedCard = card
            })
            .WithState(state)  // <-- OOB update to state container
            .Build();
    }
}

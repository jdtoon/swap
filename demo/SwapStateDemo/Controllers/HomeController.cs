using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.State;
using SwapStateDemo.Models;

namespace SwapStateDemo.Controllers;

public class HomeController : SwapController
{
    // Static data for demos
    private static readonly List<Product> _products = new()
    {
        new(1, "Laptop Pro", "Electronics", 1299.99m, 15, false),
        new(2, "Wireless Mouse", "Electronics", 49.99m, 50, false),
        new(3, "Office Chair", "Furniture", 299.99m, 8, false),
        new(4, "USB-C Hub", "Electronics", 79.99m, 25, false),
        new(5, "Standing Desk", "Furniture", 599.99m, 3, false),
        new(6, "Monitor 27\"", "Electronics", 449.99m, 12, false),
        new(7, "Keyboard", "Electronics", 149.99m, 30, true), // archived
        new(8, "Webcam HD", "Electronics", 89.99m, 20, false),
        new(9, "Desk Lamp", "Furniture", 45.99m, 40, false),
        new(10, "Cable Kit", "Accessories", 29.99m, 100, false),
    };

    private static readonly List<Expense> _expenses = new()
    {
        new(1, "Office Supplies", "Operations", 150.00m, DateTime.Today.AddDays(-5)),
        new(2, "Software License", "IT", 499.99m, DateTime.Today.AddDays(-10)),
        new(3, "Team Lunch", "HR", 125.50m, DateTime.Today.AddDays(-3)),
        new(4, "Cloud Hosting", "IT", 299.00m, DateTime.Today.AddDays(-15)),
        new(5, "Marketing Ads", "Marketing", 500.00m, DateTime.Today.AddDays(-7)),
    };

    private static readonly List<Order> _orders = new()
    {
        new(1, "Alice Corp", "pending", 1500.00m, DateTime.Today.AddDays(-1), new() { "urgent", "wholesale" }),
        new(2, "Bob LLC", "shipped", 750.50m, DateTime.Today.AddDays(-3), new() { "retail" }),
        new(3, "Charlie Inc", "delivered", 2200.00m, DateTime.Today.AddDays(-7), new() { "wholesale" }),
        new(4, "Delta Co", "pending", 450.00m, DateTime.Today, new() { "urgent" }),
        new(5, "Echo Ltd", "shipped", 890.00m, DateTime.Today.AddDays(-2), new() { "retail", "priority" }),
    };

    public IActionResult Index()
    {
        return View();
    }

    #region Demo 1: swap-hidden Only

    /// <summary>
    /// Simple product search with colocated hidden state
    /// </summary>
    [HttpGet]
    public IActionResult ProductSearch(int page = 1, string? search = null, string sortBy = "name", bool sortDesc = false)
    {
        var query = _products.Where(p => !p.IsArchived);
        
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        query = sortBy switch
        {
            "price" => sortDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "stock" => sortDesc ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
            _ => sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var products = query.Skip((page - 1) * 5).Take(5).ToList();
        var totalPages = (int)Math.Ceiling(query.Count() / 5.0);

        var model = new ProductListModel(products, page, totalPages, search, sortBy, sortDesc);
        return PartialView("_ProductSearchResults", model);
    }

    /// <summary>
    /// Expense filter with dates and categories - swap-hidden handles formatting
    /// </summary>
    [HttpGet]
    public IActionResult ExpenseFilter(DateTime? startDate, DateTime? endDate, string category = "all")
    {
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;

        var query = _expenses.Where(e => e.Date >= start && e.Date <= end);
        
        if (category != "all")
            query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        var model = new ExpenseListModel(query.ToList(), start, end, category, query.Sum(e => e.Amount));
        return PartialView("_ExpenseFilterResults", model);
    }

    /// <summary>
    /// Order list with collection state (tags filter)
    /// </summary>
    [HttpGet]
    public IActionResult OrderList(int page = 1, string status = "all", string? fromDate = null, string? toDate = null, string? tags = null)
    {
        var query = _orders.AsEnumerable();

        if (status != "all")
            query = query.Where(o => o.Status == status);

        if (DateTime.TryParse(fromDate, out var from))
            query = query.Where(o => o.OrderDate >= from);

        if (DateTime.TryParse(toDate, out var to))
            query = query.Where(o => o.OrderDate <= to);

        var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        if (tagList.Any())
            query = query.Where(o => o.Tags.Any(t => tagList.Contains(t)));

        var model = new OrderFilters(page, status, 
            DateTime.TryParse(fromDate, out var fd) ? fd : null,
            DateTime.TryParse(toDate, out var td) ? td : null,
            tagList);

        return PartialView("_OrderListResults", (Orders: query.ToList(), Filters: model));
    }

    #endregion

    #region Demo 2: swap-state Only

    /// <summary>
    /// Full inventory grid using SwapState
    /// </summary>
    [HttpGet]
    public IActionResult InventoryGrid([FromSwapState] InventoryState state)
    {
        var query = _products.AsEnumerable();

        if (!state.ShowArchived)
            query = query.Where(p => !p.IsArchived);

        if (state.Tab != "all")
            query = query.Where(p => p.Category.Equals(state.Tab, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(state.Search))
            query = query.Where(p => p.Name.Contains(state.Search, StringComparison.OrdinalIgnoreCase));

        query = state.SortBy switch
        {
            "price" => state.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "stock" => state.SortDescending ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
            _ => state.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var total = query.Count();
        var products = query.Skip((state.Page - 1) * state.PageSize).Take(state.PageSize).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)state.PageSize);

        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        return SwapResponse()
            .WithView("_InventoryGrid", products)
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Dashboard using SwapState for date filters
    /// </summary>
    [HttpGet]
    public IActionResult Dashboard([FromSwapState] DashboardState state)
    {
        var expenses = _expenses
            .Where(e => e.Date >= state.StartDate && e.Date <= state.EndDate);

        if (state.Category != "all")
            expenses = expenses.Where(e => e.Category.Equals(state.Category, StringComparison.OrdinalIgnoreCase));

        return SwapResponse()
            .WithView("_DashboardContent", expenses.ToList())
            .WithState(state)
            .Build();
    }

    #endregion

    #region Demo 3: Both Together

    /// <summary>
    /// Complex page with global SwapState + local swap-hidden
    /// </summary>
    [HttpGet]
    public IActionResult CombinedPage([FromSwapState] InventoryState globalState, string localWidgetSort = "date")
    {
        var products = _products.Where(p => !p.IsArchived).Take(5);
        var expenses = _expenses.OrderByDescending(e => e.Date).Take(3);

        var model = new CombinedPageModel(globalState, products, expenses, localWidgetSort);

        return SwapResponse()
            .WithView("_CombinedContent", model)
            .WithState(globalState)
            .Build();
    }

    /// <summary>
    /// Update just the products widget using local hidden state
    /// </summary>
    [HttpGet]
    public IActionResult ProductsWidget(string sortBy = "name")
    {
        var products = sortBy switch
        {
            "price" => _products.Where(p => !p.IsArchived).OrderBy(p => p.Price),
            "stock" => _products.Where(p => !p.IsArchived).OrderBy(p => p.Stock),
            _ => _products.Where(p => !p.IsArchived).OrderBy(p => p.Name)
        };

        return PartialView("_ProductsWidget", (Products: products.Take(5).ToList(), SortBy: sortBy));
    }

    /// <summary>
    /// Update just the expenses widget using local hidden state  
    /// </summary>
    [HttpGet]
    public IActionResult ExpensesWidget(string sortBy = "date")
    {
        var expenses = sortBy switch
        {
            "amount" => _expenses.OrderByDescending(e => e.Amount),
            "category" => _expenses.OrderBy(e => e.Category),
            _ => _expenses.OrderByDescending(e => e.Date)
        };

        return PartialView("_ExpensesWidget", (Expenses: expenses.Take(3).ToList(), SortBy: sortBy));
    }

    #endregion
}

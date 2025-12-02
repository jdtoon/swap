using Swap.Htmx.State;

namespace SwapStateDemo.Models;

/// <summary>
/// Full SwapState class for the inventory grid.
/// Demonstrates the container pattern with OOB sync.
/// </summary>
public class InventoryState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
    public bool ShowArchived { get; set; } = false;
}

/// <summary>
/// SwapState for the dashboard filters.
/// </summary>
public class DashboardState : SwapState
{
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string Category { get; set; } = "all";
    public bool CompareLastPeriod { get; set; } = false;
}

// View models for different demos
public record ProductListModel(
    IEnumerable<Product> Products,
    int Page,
    int TotalPages,
    string? Search,
    string SortBy,
    bool SortDescending
);

public record Product(int Id, string Name, string Category, decimal Price, int Stock, bool IsArchived);

public record ExpenseListModel(
    IEnumerable<Expense> Expenses,
    DateTime StartDate,
    DateTime EndDate,
    string Category,
    decimal Total
);

public record Expense(int Id, string Description, string Category, decimal Amount, DateTime Date);

public record OrderFilters(
    int Page,
    string Status,
    DateTime? FromDate,
    DateTime? ToDate,
    List<string> Tags
);

public record Order(int Id, string CustomerName, string Status, decimal Total, DateTime OrderDate, List<string> Tags);

public record CombinedPageModel(
    InventoryState GlobalState,
    IEnumerable<Product> Products,
    IEnumerable<Expense> RecentExpenses,
    string LocalWidgetSort
);

using Swap.Htmx.State;

namespace SwapStateDemo.Models;

public record Product(int Id, string Name, string Category, decimal Price);

/// <summary>
/// Actual SwapState class to test the feature.
/// </summary>
public class ProductFilterState : SwapState
{
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 2;
    public string? Search { get; set; }
}

public class ProductViewModel
{
    public required ProductFilterState State { get; init; }
    public required List<Product> Products { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)State.PageSize);
}

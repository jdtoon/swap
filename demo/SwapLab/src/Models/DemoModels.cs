using Swap.Htmx.State;

namespace SwapLab.Models;

/// <summary>
/// Simple product model for demos.
/// </summary>
public record Product(
    int Id,
    string Name,
    string Category,
    decimal Price,
    int Stock
);

/// <summary>
/// Product search state - demonstrates SwapState automatic binding.
/// Inheriting from SwapState provides:
/// - Automatic model binding with [FromSwapState]
/// - Automatic OOB state sync with .WithState()
/// - Change tracking
/// </summary>
public class ProductSearchState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}

/// <summary>
/// View model for product grid.
/// </summary>
public class ProductGridViewModel
{
    public required IReadOnlyList<Product> Products { get; init; }
    public required ProductSearchState State { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)State.PageSize);
}

/// <summary>
/// Simple task model for demos.
/// </summary>
public record TaskItem(
    int Id,
    string Title,
    bool IsCompleted,
    DateTime CreatedAt
);

/// <summary>
/// Task statistics view model.
/// </summary>
public class TaskStatsViewModel
{
    public int Total { get; init; }
    public int Completed { get; init; }
    public int Pending => Total - Completed;
    public double CompletionRate => Total > 0 ? (double)Completed / Total * 100 : 0;
}

/// <summary>
/// Infinite scroll item model.
/// </summary>
public record ScrollItem
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
}

/// <summary>
/// View model for infinite scroll pattern.
/// </summary>
public class InfiniteScrollViewModel
{
    public required List<ScrollItem> Items { get; init; }
    public int CurrentPage { get; init; }
    public bool HasMore { get; init; }
    public int NextPage => CurrentPage + 1;
}

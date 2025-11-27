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

/// <summary>
/// State for URL sync demo - demonstrates UrlSync feature.
/// </summary>
public class UrlSyncState : SwapState
{
    /// <summary>
    /// Enable URL synchronization - state is read from query string on load.
    /// </summary>
    public override bool UrlSync => true;
    
    public string Category { get; set; } = "all";
    public string PriceRange { get; set; } = "all";
    public string SortBy { get; set; } = "name";
}

/// <summary>
/// View model for URL sync demo.
/// </summary>
public class UrlSyncViewModel
{
    public required UrlSyncState State { get; init; }
    public required IReadOnlyList<Product> Products { get; init; }
}

/// <summary>
/// View model for conditional swaps demo.
/// </summary>
public class ConditionalSwapViewModel
{
    public string? Role { get; set; }
    public decimal OrderValue { get; set; }
}

// ==========================================
// Recipe Models
// ==========================================

/// <summary>
/// State for the multi-select picker recipe.
/// </summary>
public class RateCardPickerState : SwapState
{
    public string SelectedIds { get; set; } = "";
    
    public List<int> GetSelectedIdList() => 
        string.IsNullOrEmpty(SelectedIds) 
            ? [] 
            : SelectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
    
    public void SetSelectedIds(IEnumerable<int> ids) =>
        SelectedIds = string.Join(",", ids);
}

/// <summary>
/// A rate card for the multi-select picker.
/// </summary>
public record RateCard(int Id, string Name, decimal Price, string Description);

/// <summary>
/// View model for rate card picker.
/// </summary>
public class RateCardPickerViewModel
{
    public required RateCardPickerState State { get; init; }
    public required List<RateCard> RateCards { get; init; }
    public int SelectedCount => State.GetSelectedIdList().Count;
    public decimal SelectedTotal { get; init; }
}

/// <summary>
/// State for the split-view quote builder recipe.
/// </summary>
public class QuoteBuilderState : SwapState
{
    public string Currency { get; set; } = "USD";
    public decimal MarkupPercent { get; set; } = 15;
    public bool ShowImages { get; set; } = true;
    public bool ShowDescriptions { get; set; } = true;
    public bool IncludeTax { get; set; } = false;
}

/// <summary>
/// A line item in a quote.
/// </summary>
public record QuoteLineItem(int Id, string Name, decimal BasePrice, string? ImageUrl = null, string? Description = null);

/// <summary>
/// View model for quote builder.
/// </summary>
public class QuoteBuilderViewModel
{
    public required QuoteBuilderState State { get; init; }
    public required List<QuoteLineItem> Items { get; init; }
    public decimal Subtotal => Items.Sum(i => i.BasePrice);
    public decimal MarkupAmount => Subtotal * (State.MarkupPercent / 100);
    public decimal TaxAmount => State.IncludeTax ? (Subtotal + MarkupAmount) * 0.08m : 0;
    public decimal Total => Subtotal + MarkupAmount + TaxAmount;
    
    public string FormatCurrency(decimal amount) => State.Currency switch
    {
        "EUR" => $"€{amount:N2}",
        "GBP" => $"£{amount:N2}",
        _ => $"${amount:N2}"
    };
}

/// <summary>
/// State for the wizard/multi-step form recipe.
/// </summary>
public class CheckoutWizardState : SwapState
{
    public int CurrentStep { get; set; } = 1;
    
    // Step 1: Shipping
    public string ShippingName { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public string ShippingCity { get; set; } = "";
    
    // Step 2: Payment
    public string CardNumber { get; set; } = "";
    public string CardExpiry { get; set; } = "";
    public string CardCvv { get; set; } = "";
}

/// <summary>
/// View model for checkout wizard.
/// </summary>
public class CheckoutWizardViewModel
{
    public required CheckoutWizardState State { get; init; }
    public Dictionary<string, string> Errors { get; init; } = [];
}

/// <summary>
/// An editable item for the inline edit recipe.
/// </summary>
public class EditableItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
}

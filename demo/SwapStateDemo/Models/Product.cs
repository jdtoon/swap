using Swap.Htmx.State;

namespace SwapStateDemo.Models;

public record Product(int Id, string Name, string Category, decimal Price, bool InStock);

/// <summary>
/// Actual SwapState class to test the feature.
/// </summary>
public class ProductFilterState : SwapState
{
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 2;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
    public bool InStockOnly { get; set; } = false;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

public class ProductViewModel
{
    public required ProductFilterState State { get; init; }
    public required List<Product> Products { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)State.PageSize);
}

// ==========================================
// WIZARD STATE - 5 step wizard test
// ==========================================

public class WizardState : SwapState
{
    public int Step { get; set; } = 1;
    
    // Step 1: Personal Info
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    
    // Step 2: Address
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    
    // Step 3: Preferences
    public string ContactMethod { get; set; } = "email";
    public bool Newsletter { get; set; } = false;
    public string Frequency { get; set; } = "weekly";
    
    // Step 4: Payment
    public string PaymentMethod { get; set; } = "card";
    public string CardType { get; set; } = "";
    
    // Step 5: Review (no new fields, just displays all)
}

public class WizardViewModel
{
    public required WizardState State { get; init; }
    public Dictionary<string, string>? Errors { get; init; }
}

// ==========================================
// DASHBOARD STATE - OOB updates demo
// ==========================================

public class DashboardState : SwapState
{
    // Track which cards are expanded (comma-separated IDs)
    public string ExpandedCards { get; set; } = "";
    
    // Track selected card for detail view
    public int? SelectedCardId { get; set; }
    
    // Simple counter to prove state persists
    public int ClickCount { get; set; } = 0;
    
    public bool IsExpanded(int cardId)
    {
        if (string.IsNullOrEmpty(ExpandedCards)) return false;
        return ExpandedCards.Split(',').Contains(cardId.ToString());
    }
    
    public void ToggleExpanded(int cardId)
    {
        var ids = string.IsNullOrEmpty(ExpandedCards) 
            ? new List<string>() 
            : ExpandedCards.Split(',').ToList();
            
        var cardIdStr = cardId.ToString();
        if (ids.Contains(cardIdStr))
            ids.Remove(cardIdStr);
        else
            ids.Add(cardIdStr);
            
        ExpandedCards = string.Join(",", ids);
    }
}

public class DashboardCard
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Details { get; set; } = "";
    public string Icon { get; set; } = "📊";
}

public class DashboardViewModel
{
    public required DashboardState State { get; init; }
    public required List<DashboardCard> Cards { get; init; }
    public DashboardCard? SelectedCard { get; init; }
}

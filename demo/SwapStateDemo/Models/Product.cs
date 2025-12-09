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

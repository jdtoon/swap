using Swap.CLI.Models;

namespace Swap.CLI.ViewModels;

/// <summary>
/// View model for Product list with pagination and search
/// </summary>
public class ProductListViewModel
{
    /// <summary>
    /// List of Product items for current page
    /// </summary>
    public List<Product> Items { get; set; } = new();
    
    /// <summary>
    /// Pagination information
    /// </summary>
    public PaginationDto Pagination { get; set; } = new();
    
    /// <summary>
    /// Current search term (if any)
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Current sort column
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Current sort order (asc/desc)
    /// </summary>
    public string? SortOrder { get; set; }
    
    /// <summary>
    /// Active filters (field name -> value)
    /// </summary>
    public Dictionary<string, string?> Filters { get; set; } = new();
}

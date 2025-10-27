---
sidebar_position: 2
---

# Search

Real-time search functionality is automatically included in all generated controllers, with 500ms debouncing and multi-field support.

## Overview

When you generate a controller, search is automatically configured:

```bash
swap g c Product --fields "Name:string Description:string? SKU:string"
```

The generated search:
- Searches across all string fields
- 500ms debounce (waits for user to stop typing)
- Case-insensitive matching
- HTMX partial updates (no page reload)
- Preserves pagination, sort, and filter state

## How It Works

### Controller Search Logic

The `Index` action includes search parameter:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1,
    int pageSize = 10,
    string? searchTerm = null,  // Search query
    string? sortBy = null,
    string? sortOrder = "asc")
{
    var query = _context.Products.AsQueryable();
    
    // Apply search across all string fields
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(x => 
            x.Name.ToLower().Contains(searchTerm.ToLower()) ||
            x.Description.ToLower().Contains(searchTerm.ToLower()) ||
            x.SKU.ToLower().Contains(searchTerm.ToLower()));
    }
    
    // Continue with pagination, sorting...
}
```

### Search Input UI

The generated `Index.cshtml` includes a debounced search input:

```cshtml
<div class="mb-4">
    <input type="text" 
           name="searchTerm"
           value="@Model.SearchTerm"
           placeholder="Search products..." 
           class="input input-bordered w-full"
           hx-get="@Url.Action("Index")"
           hx-trigger="input changed delay:500ms, search"
           hx-target="#product-list"
           hx-swap="innerHTML"
           hx-include="[name='pageSize'], [name='sortBy'], [name='sortOrder']" />
</div>
```

## HTMX Integration

### Debouncing

The search input uses HTMX trigger syntax for optimal UX:

```html
hx-trigger="input changed delay:500ms, search"
```

**Breakdown:**
- `input changed` - Triggers when input value changes
- `delay:500ms` - Waits 500ms after last keystroke
- `search` - Also triggers on Enter key

**User Experience:**
1. User types "lap"
2. HTMX waits 500ms
3. If no more input, sends search request
4. If user continues typing "laptop", timer resets
5. Prevents excessive requests while typing

### State Preservation

Search preserves other state via `hx-include`:

```html
hx-include="[name='pageSize'], [name='sortBy'], [name='sortOrder'], [name='inStock']"
```

**Result:**
- Current page size maintained
- Sort column and direction preserved  
- Active filters maintained
- Seamless integration with other features

### Pagination Reset

Search automatically resets to page 1:

```cshtml
<!-- Hidden input ensures search starts at page 1 -->
<input type="hidden" name="pageNumber" value="1" />
```

## Customization

### Search Specific Fields

Limit search to specific fields:

```csharp
// Only search Name and SKU, not Description
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.Where(x => 
        x.Name.ToLower().Contains(searchTerm.ToLower()) ||
        x.SKU.ToLower().Contains(searchTerm.ToLower()));
}
```

### Change Debounce Delay

```html
<!-- Faster: 300ms -->
hx-trigger="input changed delay:300ms, search"

<!-- Slower: 1000ms (1 second) -->
hx-trigger="input changed delay:1000ms, search"

<!-- No delay: immediate -->
hx-trigger="input changed, search"
```

### Add Search Icon

```html
<div class="relative">
    <input type="text" 
           name="searchTerm"
           placeholder="Search..." 
           class="input input-bordered w-full pl-10"
           hx-get="@Url.Action("Index")"
           hx-trigger="input changed delay:500ms, search"
           hx-target="#product-list" />
    <svg class="absolute left-3 top-3 h-5 w-5 text-gray-400" 
         fill="none" 
         viewBox="0 0 24 24" 
         stroke="currentColor">
        <path stroke-linecap="round" 
              stroke-linejoin="round" 
              stroke-width="2" 
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
</div>
```

### Add Clear Button

```html
<div class="flex gap-2">
    <input type="text" 
           name="searchTerm"
           value="@Model.SearchTerm"
           placeholder="Search..." 
           class="input input-bordered flex-1"
           hx-get="@Url.Action("Index")"
           hx-trigger="input changed delay:500ms, search"
           hx-target="#product-list" />
    
    @if (!string.IsNullOrEmpty(Model.SearchTerm))
    {
        <button class="btn btn-outline"
                hx-get="@Url.Action("Index")"
                hx-vals='{"searchTerm": ""}'
                hx-target="#product-list">
            Clear
        </button>
    }
</div>
```

### Case-Sensitive Search

```csharp
// Remove .ToLower() for case-sensitive
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.Where(x => 
        x.Name.Contains(searchTerm) ||  // Case-sensitive
        x.Description.Contains(searchTerm));
}
```

### Exact Match Search

```csharp
// Use == for exact match
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.Where(x => 
        x.Name == searchTerm ||
        x.SKU == searchTerm);
}
```

### Starts With Search

```csharp
// Use StartsWith for prefix search
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.Where(x => 
        x.Name.ToLower().StartsWith(searchTerm.ToLower()) ||
        x.SKU.ToLower().StartsWith(searchTerm.ToLower()));
}
```

### Search Numeric Fields

```csharp
// Search by price or quantity
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    // Try parse as decimal
    if (decimal.TryParse(searchTerm, out var price))
    {
        query = query.Where(x => x.Price == price);
    }
    else
    {
        // Fall back to string search
        query = query.Where(x => x.Name.ToLower().Contains(searchTerm.ToLower()));
    }
}
```

## Advanced Search Patterns

### Multi-Word Search

Search for all words (AND logic):

```csharp
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    var words = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
    foreach (var word in words)
    {
        query = query.Where(x => 
            x.Name.ToLower().Contains(word) ||
            x.Description.ToLower().Contains(word));
    }
}
```

Search for any word (OR logic):

```csharp
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    var words = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
    query = query.Where(x => 
        words.Any(word => x.Name.ToLower().Contains(word) || 
                         x.Description.ToLower().Contains(word)));
}
```

### Weighted Search Results

Prioritize matches in specific fields:

```csharp
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    var term = searchTerm.ToLower();
    
    query = query
        .Where(x => 
            x.Name.ToLower().Contains(term) ||
            x.Description.ToLower().Contains(term) ||
            x.SKU.ToLower().Contains(term))
        .OrderByDescending(x => x.Name.ToLower().Contains(term) ? 1 : 0)  // Name matches first
        .ThenByDescending(x => x.SKU.ToLower().Contains(term) ? 1 : 0);   // Then SKU
}
```

### Search with Minimum Length

Prevent expensive searches on very short terms:

```csharp
if (!string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= 3)
{
    query = query.Where(x => 
        x.Name.ToLower().Contains(searchTerm.ToLower()));
}
```

```html
<!-- Show helper text -->
<input type="text" 
       name="searchTerm"
       minlength="3"
       placeholder="Search (min 3 characters)..." />
<small class="text-gray-500">Type at least 3 characters to search</small>
```

### Search with Regular Expressions

```csharp
using System.Text.RegularExpressions;

if (!string.IsNullOrWhiteSpace(searchTerm))
{
    try
    {
        var regex = new Regex(searchTerm, RegexOptions.IgnoreCase);
        var products = await _context.Products.ToListAsync();
        products = products.Where(x => 
            regex.IsMatch(x.Name) || 
            regex.IsMatch(x.Description ?? "")).ToList();
        
        // Continue with pagination...
    }
    catch (ArgumentException)
    {
        // Invalid regex, fall back to Contains
        query = query.Where(x => x.Name.Contains(searchTerm));
    }
}
```

## Performance Optimization

### Database Indexes

Add indexes on searchable columns:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.Name)
        .HasDatabaseName("IX_Product_Name");
        
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.SKU)
        .HasDatabaseName("IX_Product_SKU");
}
```

### Full-Text Search (SQL Server)

For large text fields, use full-text search:

```csharp
// Enable in OnModelCreating
modelBuilder.Entity<Product>()
    .HasIndex(p => p.Description)
    .ForSqlServerIsFull Text();

// Use in query
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    query = query.Where(x => 
        EF.Functions.Contains(x.Description, searchTerm));
}
```

### Limit Search Results

Prevent excessive results:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1,
    int pageSize = 10,
    int maxResults = 1000,  // Add limit
    string? searchTerm = null)
{
    var query = _context.Products.AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(/* search logic */);
        
        var count = await query.CountAsync();
        if (count > maxResults)
        {
            ViewBag.Message = $"Too many results ({count}). Showing first {maxResults}. Please refine your search.";
            query = query.Take(maxResults);
        }
    }
    
    // Continue with pagination...
}
```

### Cache Common Searches

```csharp
private readonly IMemoryCache _cache;

public async Task<IActionResult> Index(string? searchTerm = null)
{
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        var cacheKey = $"search_{searchTerm.ToLower()}";
        
        if (!_cache.TryGetValue(cacheKey, out List<Product> products))
        {
            var query = _context.Products.Where(x => 
                x.Name.ToLower().Contains(searchTerm.ToLower()));
            products = await query.ToListAsync();
            
            _cache.Set(cacheKey, products, TimeSpan.FromMinutes(5));
        }
        
        // Continue with pagination on cached results...
    }
}
```

## Best Practices

### 1. Always Use Case-Insensitive Search

```csharp
// ✅ Good: Case-insensitive
x.Name.ToLower().Contains(searchTerm.ToLower())

// ❌ Bad: Case-sensitive (users expect case-insensitive)
x.Name.Contains(searchTerm)
```

### 2. Sanitize Search Input

```csharp
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    // Trim whitespace and limit length
    searchTerm = searchTerm.Trim();
    if (searchTerm.Length > 100)
    {
        searchTerm = searchTerm.Substring(0, 100);
    }
    
    query = query.Where(/* search logic */);
}
```

### 3. Provide Search Feedback

```cshtml
@if (!string.IsNullOrEmpty(Model.SearchTerm))
{
    <div class="alert alert-info">
        <span>Searching for: <strong>@Model.SearchTerm</strong></span>
        <button hx-get="@Url.Action("Index")" 
                hx-vals='{"searchTerm": ""}'
                hx-target="#product-list"
                class="btn btn-sm btn-ghost">
            Clear
        </button>
    </div>
}
```

### 4. Show Empty State

```cshtml
@if (Model.Items.Count == 0 && !string.IsNullOrEmpty(Model.SearchTerm))
{
    <div class="alert alert-warning">
        <span>No results found for "@Model.SearchTerm"</span>
        <button hx-get="@Url.Action("Index")"
                hx-vals='{"searchTerm": ""}'
                hx-target="#product-list"
                class="btn btn-sm">
            Show all items
        </button>
    </div>
}
```

## Related Features

- [Pagination](./pagination.md) - Search with pagination
- [Sorting](./sorting.md) - Sort search results
- [Filtering](./filtering.md) - Combine search with filters

## See Also

- [HTMX Trigger Modifiers](https://htmx.org/docs/#trigger-modifiers)
- [Entity Framework LIKE Queries](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries)
- [SQL Server Full-Text Search](https://learn.microsoft.com/en-us/sql/relational-databases/search/full-text-search)

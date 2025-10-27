---
sidebar_position: 1
---

# Pagination

All generated controllers include comprehensive pagination support with HTMX integration for zero-page-reload navigation.

## Overview

Pagination is automatically included when you generate a controller:

```bash
swap g c Product --fields "Name:string Price:decimal"
```

The generated controller handles:
- Configurable page sizes (10, 25, 50, 100)
- Page navigation (First, Previous, Next, Last)
- Item count display ("Showing 1-10 of 45 items")
- State preservation across search, sort, and filter operations
- HTMX partial updates

## How It Works

### Controller Index Action

The generated `Index` action includes pagination parameters:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1,          // Current page (1-based)
    int pageSize = 10,            // Items per page
    string? searchTerm = null,    // Search query
    string? sortBy = null,        // Sort column
    string? sortOrder = "asc")    // Sort direction
{
    var query = _context.Products.AsQueryable();
    
    // Apply search, sort, filters...
    
    // Get total count for pagination
    var totalItems = await query.CountAsync();
    
    // Apply pagination
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    // Build pagination metadata
    var viewModel = new ProductListViewModel
    {
        Items = items,
        Pagination = new PaginationDto
        {
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            HxGetUrl = Url.Action("Index"),
            HxTarget = "#product-list",
            HxSwap = "innerHTML"
        }
    };
    
    return View(viewModel);
}
```

### PaginationDto Model

The `PaginationDto` class contains all pagination metadata:

```csharp
public class PaginationDto
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string HxGetUrl { get; set; } = string.Empty;
    public string HxTarget { get; set; } = string.Empty;
    public string HxSwap { get; set; } = "innerHTML";
}
```

### Pagination Controls

The `_PaginationControls.cshtml` partial renders the pagination UI:

```cshtml
@model PaginationDto

<div class="flex justify-between items-center mt-4">
    <!-- Page Size Selector -->
    <div class="form-control">
        <label class="label">
            <span class="label-text">Items per page:</span>
        </label>
        <select name="pageSize" 
                class="select select-bordered select-sm"
                hx-get="@Model.HxGetUrl"
                hx-target="@Model.HxTarget"
                hx-swap="@Model.HxSwap"
                hx-include="[name='searchTerm'], [name='sortBy'], [name='sortOrder']">
            <option value="10">10</option>
            <option value="25">25</option>
            <option value="50">50</option>
            <option value="100">100</option>
        </select>
    </div>
    
    <!-- Page Info -->
    <div class="text-sm">
        Showing @(((Model.CurrentPage - 1) * Model.PageSize) + 1)-@(Math.Min(Model.CurrentPage * Model.PageSize, Model.TotalItems)) of @Model.TotalItems items
    </div>
    
    <!-- Navigation Buttons -->
    <div class="join">
        <button class="join-item btn btn-sm"
                hx-get="@Model.HxGetUrl"
                hx-vals='{"pageNumber": 1}'
                disabled="@(!Model.HasPreviousPage)">
            First
        </button>
        <button class="join-item btn btn-sm"
                hx-get="@Model.HxGetUrl"
                hx-vals='{"pageNumber": @(Model.CurrentPage - 1)}'
                disabled="@(!Model.HasPreviousPage)">
            Previous
        </button>
        <button class="join-item btn btn-sm btn-active">
            Page @Model.CurrentPage of @Model.TotalPages
        </button>
        <button class="join-item btn btn-sm"
                hx-get="@Model.HxGetUrl"
                hx-vals='{"pageNumber": @(Model.CurrentPage + 1)}'
                disabled="@(!Model.HasNextPage)">
            Next
        </button>
        <button class="join-item btn btn-sm"
                hx-get="@Model.HxGetUrl"
                hx-vals='{"pageNumber": @Model.TotalPages}'
                disabled="@(!Model.HasNextPage)">
            Last
        </button>
    </div>
</div>
```

## HTMX Integration

### Partial Updates

When pagination controls are clicked, HTMX:
1. Sends GET request to controller with new `pageNumber` or `pageSize`
2. Includes current search, sort, and filter values
3. Controller returns `_EntityList` partial view
4. HTMX swaps content of `#entity-list` div
5. No page reload, instant navigation

**Example HTMX Request:**
```
GET /Product/Index?pageNumber=2&pageSize=25&searchTerm=laptop&sortBy=price&sortOrder=asc
Headers: HX-Request: true
```

### State Preservation

Pagination maintains state across all operations:

**Search**: When searching, pagination resets to page 1
```html
<input hx-get="@Url.Action("Index")"
       hx-vals='{"pageNumber": 1}'
       hx-include="[name='pageSize'], [name='sortBy'], [name='sortOrder']" />
```

**Sort**: When sorting, current page is preserved
```html
<button hx-get="@Url.Action("Index")"
        hx-vals='{"sortBy": "name"}'
        hx-include="[name='pageNumber'], [name='pageSize']">
    Name
</button>
```

**Filter**: When filtering, pagination resets to page 1
```html
<select hx-get="@Url.Action("Index")"
        hx-vals='{"pageNumber": 1}'
        hx-include="[name='pageSize'], [name='sortBy'], [name='sortOrder']">
</select>
```

## Customization

### Change Default Page Size

In the controller:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1,
    int pageSize = 25,  // Changed from 10 to 25
    // ...
)
```

In the view:

```cshtml
<select name="pageSize" class="select">
    <option value="25" selected>25</option>  <!-- Change default -->
    <option value="50">50</option>
    <option value="100">100</option>
</select>
```

### Add Custom Page Sizes

```cshtml
<select name="pageSize" class="select">
    <option value="5">5</option>
    <option value="10">10</option>
    <option value="25">25</option>
    <option value="50">50</option>
    <option value="100">100</option>
    <option value="500">500</option>  <!-- Add large size -->
</select>
```

### Disable Pagination

To disable pagination and show all items:

```csharp
public async Task<IActionResult> Index(bool showAll = false)
{
    var query = _context.Products.AsQueryable();
    
    if (showAll)
    {
        var allItems = await query.ToListAsync();
        return View(allItems);
    }
    
    // Normal pagination logic...
}
```

### Custom Pagination Styling

The generated `_Pagination.cshtml` uses DaisyUI components. You can customize it:

```cshtml
<!-- Default DaisyUI styling (generated) -->
<div class="join">
    <button class="join-item btn btn-sm @(!Model.HasPreviousPage ? "btn-disabled" : "")"
            hx-get="@Model.HxGetUrl" 
            hx-vals='{"pageNumber": 1}'>
        First
    </button>
    <button class="join-item btn btn-sm @(!Model.HasPreviousPage ? "btn-disabled" : "")"
            hx-get="@Model.HxGetUrl" 
            hx-vals='{"pageNumber": @(Model.CurrentPage - 1)}'>
        «
    </button>
    <button class="join-item btn btn-sm btn-active">
        Page @Model.CurrentPage of @Model.TotalPages
    </button>
    <button class="join-item btn btn-sm @(!Model.HasNextPage ? "btn-disabled" : "")"
            hx-get="@Model.HxGetUrl" 
            hx-vals='{"pageNumber": @(Model.CurrentPage + 1)}'>
        »
    </button>
    <button class="join-item btn btn-sm @(!Model.HasNextPage ? "btn-disabled" : "")"
            hx-get="@Model.HxGetUrl" 
            hx-vals='{"pageNumber": @Model.TotalPages}'>
        Last
    </button>
</div>

<!-- Alternative: Custom Tailwind styling -->
<div class="flex gap-2">
    <button class="px-4 py-2 bg-primary text-white rounded disabled:opacity-50 hover:bg-primary-focus"
            hx-get="@Model.HxGetUrl"
            hx-vals='{"pageNumber": 1}'
            disabled="@(!Model.HasPreviousPage)">
        First
    </button>
    <!-- ... more buttons -->
</div>
```

## Best Practices

### 1. Set Reasonable Defaults

```csharp
// Good: Start with manageable page size
int pageSize = 10

// Avoid: Too large default
int pageSize = 1000  // ❌ May cause performance issues
```

### 2. Add Loading Indicators

DaisyUI provides built-in loading spinners:

```html
<div id="product-list" 
     hx-get="@Url.Action("Index")"
     hx-indicator="#loading">
    @await Html.PartialAsync("_List", Model)
</div>

<div id="loading" class="htmx-indicator">
    <span class="loading loading-spinner loading-lg"></span>
</div>
```

<div class="htmx-indicator">
    <span class="loading loading-spinner"></span> Loading...
</div>
```

### 3. Cache Total Count for Large Tables

```csharp
// For tables with millions of rows, cache count
private async Task<int> GetTotalCountAsync()
{
    var cacheKey = "products_total_count";
    
    if (!_cache.TryGetValue(cacheKey, out int count))
    {
        count = await _context.Products.CountAsync();
        _cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));
    }
    
    return count;
}
```

### 4. Handle Empty States

```cshtml
@if (Model.TotalItems == 0)
{
    <div class="alert alert-info">
        <span>No items found. Try adjusting your search or filters.</span>
    </div>
}
else
{
    <!-- Show pagination controls -->
}
```

## Performance Considerations

### Efficient Queries

Always apply filters and search BEFORE pagination:

```csharp
// ✅ Good: Filter first, then paginate
var query = _context.Products
    .Where(p => p.InStock)           // Filter
    .Where(p => p.Name.Contains(searchTerm))  // Search
    .OrderBy(p => p.Name)            // Sort
    .Skip((pageNumber - 1) * pageSize)  // Paginate
    .Take(pageSize);

// ❌ Bad: Load all, then filter
var allProducts = await _context.Products.ToListAsync();
var filtered = allProducts.Where(p => p.InStock).Skip(...).Take(...);
```

### Index Database Columns

For frequently sorted/filtered columns:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.Name);  // Index for sorting/searching
        
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.InStock);  // Index for filtering
}
```

### Avoid N+1 Queries

Use `.Include()` for related data:

```csharp
var items = await query
    .Include(p => p.Category)  // Load related Category
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## Related Features

- [Search](./search.md) - Real-time search integration
- [Sorting](./sorting.md) - Column sorting with pagination
- [Filtering](./filtering.md) - Filters with pagination

## See Also

- [DaisyUI Pagination Components](https://daisyui.com/components/pagination/)
- [HTMX Documentation](https://htmx.org/docs/)
- [Entity Framework Core Pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination)

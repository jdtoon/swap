---
sidebar_position: 3
---

# Sorting

Column sorting with visual indicators and field-level control is automatically included in all generated controllers.

## Overview

When you generate a controller, sorting is configured automatically:

```bash
swap g c Product --fields "Name:string Price:decimal InStock:bool CreatedDate:DateTime"
```

Features:
- **Clickable headers** for all sortable fields
- **Visual indicators** (↑ ascending, ↓ descending)
- **Toggle behavior** (click again to reverse order)
- **HTMX partial updates** (no page reload)
- **Field-level control** with `:nosort` flag
- **State preservation** across search, filter, pagination

## How It Works

### Controller Sorting Logic

The `Index` action includes sort parameters:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1,
    int pageSize = 10,
    string? searchTerm = null,
    string? sortBy = null,          // Column to sort by
    string? sortOrder = "asc")      // "asc" or "desc"
{
    var query = _context.Products.AsQueryable();
    
    // Apply search and filters...
    
    // Apply sorting
    if (!string.IsNullOrWhiteSpace(sortBy))
    {
        query = ApplySorting(query, sortBy, sortOrder ?? "asc");
    }
    
    // Continue with pagination...
}

private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
{
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy?.ToLower() switch
    {
        "name" => isDescending 
            ? query.OrderByDescending(x => x.Name) 
            : query.OrderBy(x => x.Name),
        "price" => isDescending 
            ? query.OrderByDescending(x => x.Price) 
            : query.OrderBy(x => x.Price),
        "instock" => isDescending 
            ? query.OrderByDescending(x => x.InStock) 
            : query.OrderBy(x => x.InStock),
        "createddate" => isDescending 
            ? query.OrderByDescending(x => x.CreatedDate) 
            : query.OrderBy(x => x.CreatedDate),
        _ => query
    };
}
```

### Sortable Table Headers

Generated `_EntityList.cshtml` includes clickable headers:

```cshtml
<thead>
    <tr>
        <th>
            <button class="flex items-center gap-1 hover:text-primary"
                    hx-get="@Url.Action("Index")"
                    hx-target="#product-list"
                    hx-swap="innerHTML"
                    hx-include="[name='searchTerm'], [name='pageSize']"
                    hx-vals='{"sortBy": "name", "sortOrder": "@(Model.SortBy?.ToLower() == "name" && Model.SortOrder == "asc" ? "desc" : "asc")"}'
                    type="button">
                Name
                @if (Model.SortBy?.ToLower() == "name")
                {
                    @if (Model.SortOrder == "desc")
                    {
                        <span>↓</span>
                    }
                    else
                    {
                        <span>↑</span>
                    }
                }
            </button>
        </th>
        <th>Price</th>  <!-- Not sortable -->
        <!-- More headers... -->
    </tr>
</thead>
```

## Field-Level Control

### Disable Sorting on Specific Fields

Use the `:nosort` or `:ns` flag:

```bash
swap g c Product --fields "Name:string SKU:string:ns Price:decimal:nosort"
```

**Result:**
- **Name**: Sortable button with ↑/↓ indicators
- **SKU**: Plain text header (not clickable)
- **Price**: Plain text header (not clickable)

**Generated Code:**
```cshtml
<th>
    <button ...>Name @if (...) { <span>↑</span> }</button>
</th>
<th>SKU</th>  <!-- Plain text, no button -->
<th>Price</th>  <!-- Plain text, no button -->
```

### Use Cases for `:nosort`

**Identifier Fields:**
```bash
# Don't sort by SKU or GUID
--fields "Id:Guid:ns Name:string SKU:string:ns"
```

**Long Text Fields:**
```bash
# Don't sort by description or content
--fields "Title:string Content:string:ns Notes:string?:ns"
```

**Computed/Display Fields:**
```bash
# Don't sort by formatted display values
--fields "Name:string FormattedPrice:string:ns TotalDisplay:string:ns"
```

**Configuration Values:**
```bash
# Don't sort by thresholds or limits
--fields "Name:string LowStockThreshold:int:ns MaxQuantity:int:ns"
```

## Visual Indicators

### Ascending Sort (↑)

```
Name ↑    Price    Created Date
```

Indicates data is sorted A-Z, lowest-highest, oldest-newest.

### Descending Sort (↓)

```
Name ↓    Price    Created Date
```

Indicates data is sorted Z-A, highest-lowest, newest-oldest.

### No Indicator

```
Name    Price ↓    Created Date
```

Column is sortable but not currently the sort column.

## HTMX Integration

### Partial Updates

When a sort header is clicked, HTMX:
1. Sends GET request with new `sortBy` and `sortOrder`
2. Includes current search, page, filter values
3. Controller returns `_EntityList` partial
4. HTMX swaps `#entity-list` content
5. No page reload

**Example Request:**
```
GET /Product/Index?sortBy=price&sortOrder=desc&searchTerm=laptop&pageNumber=1
Headers: HX-Request: true
```

### State Preservation

Sort header includes current state:

```html
hx-include="[name='searchTerm'], [name='pageSize'], [name='inStock']"
```

**Result:**
- Search term preserved
- Page size maintained
- Filters active
- Pagination preserved (stays on current page)

### Toggle Logic

Header button toggles sort direction:

```cshtml
hx-vals='{"sortBy": "name", "sortOrder": "@(Model.SortBy?.ToLower() == "name" && Model.SortOrder == "asc" ? "desc" : "asc")"}'
```

**Behavior:**
- First click: Sort ascending
- Second click: Sort descending
- Third click: Sort ascending again

## Customization

### Change Default Sort

```csharp
public async Task<IActionResult> Index(
    string? sortBy = "name",      // Default sort by name
    string? sortOrder = "asc")    // Default ascending
{
    // If no sort specified, use default
    if (string.IsNullOrWhiteSpace(sortBy))
    {
        sortBy = "name";
    }
    
    query = ApplySorting(query, sortBy, sortOrder ?? "asc");
}
```

### Add Custom Sort Logic

```csharp
private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
{
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy?.ToLower() switch
    {
        "name" => isDescending 
            ? query.OrderByDescending(x => x.Name) 
            : query.OrderBy(x => x.Name),
        
        // Custom: Sort by stock status (InStock first, then OutOfStock)
        "stockstatus" => isDescending
            ? query.OrderByDescending(x => x.InStock).ThenBy(x => x.Quantity)
            : query.OrderBy(x => x.InStock).ThenByDescending(x => x.Quantity),
        
        // Custom: Sort by full name (first + last)
        "fullname" => isDescending
            ? query.OrderByDescending(x => x.FirstName + " " + x.LastName)
            : query.OrderBy(x => x.FirstName + " " + x.LastName),
        
        _ => query
    };
}
```

### Multi-Column Sort

```csharp
private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
{
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy?.ToLower() switch
    {
        "name" => isDescending 
            ? query.OrderByDescending(x => x.Name).ThenByDescending(x => x.CreatedDate)  // Then by date
            : query.OrderBy(x => x.Name).ThenBy(x => x.CreatedDate),
        
        "category" => isDescending
            ? query.OrderByDescending(x => x.CategoryName).ThenBy(x => x.Name)  // Then by name
            : query.OrderBy(x => x.CategoryName).ThenByDescending(x => x.Name),
        
        _ => query
    };
}
```

### Case-Insensitive Sort

```csharp
private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
{
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy?.ToLower() switch
    {
        "name" => isDescending 
            ? query.OrderByDescending(x => x.Name.ToLower())  // Case-insensitive
            : query.OrderBy(x => x.Name.ToLower()),
        
        _ => query
    };
}
```

### Custom Sort Icons

Replace ↑/↓ with custom icons:

```cshtml
@if (Model.SortBy?.ToLower() == "name")
{
    @if (Model.SortOrder == "desc")
    {
        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path d="M10 3l-7 7h4v7h6v-7h4l-7-7z"/>
        </svg>
    }
    else
    {
        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path d="M10 17l7-7h-4V3H7v7H3l7 7z"/>
        </svg>
    }
}
```

### Disable Sort on Empty Lists

```cshtml
@if (Model.Items.Count > 0)
{
    <button hx-get="..." class="sortable-header">
        Name
    </button>
}
else
{
    <span>Name</span>  <!-- Not clickable when empty -->
}
```

## Advanced Patterns

### Related Entity Sorting

Sort by navigation property:

```csharp
private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
{
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy?.ToLower() switch
    {
        "category" => isDescending 
            ? query.OrderByDescending(x => x.Category.Name)  // Sort by related Category.Name
            : query.OrderBy(x => x.Category.Name),
        
        _ => query
    };
}

// Don't forget to .Include() the navigation property
var query = _context.Products.Include(p => p.Category);
```

### Null-Safe Sorting

Handle nullable fields:

```csharp
"description" => isDescending 
    ? query.OrderByDescending(x => x.Description ?? "")  // Nulls last when desc
    : query.OrderBy(x => x.Description ?? "ZZZZZZZ"),    // Nulls last when asc
```

### Performance-Optimized Sorting

For large datasets, use database-level sorting:

```csharp
// ✅ Good: Sort before loading data
var items = await query
    .OrderBy(x => x.Name)  // Database sorts
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();  // Load only current page

// ❌ Bad: Load all then sort
var allItems = await query.ToListAsync();  // Load all
var sorted = allItems.OrderBy(x => x.Name);  // Sort in memory
```

## Best Practices

### 1. Index Sorted Columns

Add database indexes for frequently sorted columns:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.Name);
        
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.Price);
        
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.CreatedDate);
}
```

### 2. Provide Default Sort

Always have a sensible default:

```csharp
// Default to most recently created
if (string.IsNullOrWhiteSpace(sortBy))
{
    query = query.OrderByDescending(x => x.CreatedDate);
}
```

### 3. Show Current Sort State

Visual feedback helps users:

```cshtml
<!-- Highlight active sort column -->
<th class="@(Model.SortBy == "name" ? "bg-primary bg-opacity-10" : "")">
    <button ...>Name</button>
</th>
```

### 4. Limit Sortable Fields

Don't make everything sortable:

```bash
# Strategic sorting
swap g c Product --fields "Name:string Description:string:ns SKU:string:ns Price:decimal CreatedDate:DateTime"
# Only Name, Price, CreatedDate sortable
```

## Performance Considerations

### Database Indexes

```sql
-- SQL Server
CREATE INDEX IX_Product_Name ON Products (Name);
CREATE INDEX IX_Product_Price ON Products (Price);
CREATE INDEX IX_Product_CreatedDate ON Products (CreatedDate);
```

### Avoid Sorting Large Text

```bash
# Don't sort by long text fields
--fields "Title:string Content:string:ns"  # Content not sortable
```

### Use Composite Indexes

For multi-column sorts:

```csharp
modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.CategoryId, p.Name });  // Composite index
```

## Related Features

- [Pagination](./pagination.md) - Sort paginated results
- [Search](./search.md) - Sort search results
- [Filtering](./filtering.md) - Sort filtered results
- [Field Flags](../cli/generate-controller.md#field-flags) - Control sortable fields

## See Also

- [HTMX Attributes](https://htmx.org/attributes/)
- [Entity Framework Sorting](https://learn.microsoft.com/en-us/ef/core/querying/sorting)
- [DaisyUI Table Component](https://daisyui.com/components/table/)

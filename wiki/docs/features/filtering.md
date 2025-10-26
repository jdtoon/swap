---
sidebar_position: 4
---

# Filtering

Boolean field filtering with dropdown controls and field-level configuration is automatically included in generated controllers.

## Overview

When you generate a controller with boolean fields marked as filterable:

```bash
swap g c Product --fields "Name:string InStock:bool:f FeaturedProduct:bool:f"
```

Features:
- **Dropdown filters** for filterable boolean fields (All/Yes/No)
- **Field-level control** with `:filterable` or `:f` flag
- **HTMX partial updates** (no page reload)
- **Multiple filters** work together
- **State preservation** across search, sort, pagination

## How It Works

### Controller Filter Logic

```csharp
public async Task<IActionResult> Index(
    bool? inStock = null,           // Filter parameter
    bool? featuredProduct = null)   // Filter parameter
{
    var query = _context.Products.AsQueryable();
    
    // Apply filters
    query = ApplyFilters(query, inStock, featuredProduct);
    
    // Continue with sort, pagination...
}

private IQueryable<Product> ApplyFilters(
    IQueryable<Product> query,
    bool? inStock = null,
    bool? featuredProduct = null)
{
    if (inStock.HasValue)
    {
        query = query.Where(x => x.InStock == inStock.Value);
    }
    
    if (featuredProduct.HasValue)
    {
        query = query.Where(x => x.FeaturedProduct == featuredProduct.Value);
    }
    
    return query;
}
```

### Filter Dropdowns UI

Generated `Index.cshtml` includes filter section:

```cshtml
<!-- Filters -->
<div class="mb-4">
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div class="form-control">
            <label class="label">
                <span class="label-text">InStock</span>
            </label>
            <select name="inStock" 
                    class="select select-bordered w-full"
                    hx-get="@Url.Action("Index")"
                    hx-target="#product-list"
                    hx-swap="innerHTML"
                    hx-include="[name='searchTerm'], [name='pageSize'], [name='sortBy'], [name='sortOrder']">
                <option value="">All</option>
                <option value="true" @(Model.Filters["inStock"] == "true" ? "selected" : "")>Yes</option>
                <option value="false" @(Model.Filters["inStock"] == "false" ? "selected" : "")>No</option>
            </select>
        </div>
        
        <div class="form-control">
            <label class="label">
                <span class="label-text">FeaturedProduct</span>
            </label>
            <select name="featuredProduct" class="select select-bordered w-full" ...>
                <option value="">All</option>
                <option value="true">Yes</option>
                <option value="false">No</option>
            </select>
        </div>
    </div>
</div>
```

## Field-Level Control

### Enable Filtering with `:f` Flag

By default, boolean fields are **NOT filterable**. Use `:filterable` or `:f` to enable:

```bash
# Only InStock and Active are filterable
swap g c Product --fields "Name:string InStock:bool:f IsDeleted:bool Active:bool:filterable"
```

**Result:**
- **InStock**: Has filter dropdown
- **IsDeleted**: No filter (default)
- **Active**: Has filter dropdown

### Use Cases for Filterable Booleans

**Status Fields:**
```bash
--fields "Title:string IsPublished:bool:f IsArchived:bool:f"
```

**Flags:**
```bash
--fields "Name:string IsFeatured:bool:f IsOnSale:bool:f IsPremium:bool:f"
```

**Inventory:**
```bash
--fields "ProductName:string InStock:bool:f LowStock:bool:f"
```

**User Management:**
```bash
--fields "Username:string IsActive:bool:f IsVerified:bool:f IsAdmin:bool:f"
```

## Multiple Filters

Filters work together (AND logic):

```csharp
// User selects: InStock=true AND FeaturedProduct=true
var query = _context.Products
    .Where(x => x.InStock == true)           // First filter
    .Where(x => x.FeaturedProduct == true);  // Second filter
// Result: Products that are BOTH in stock AND featured
```

## HTMX Integration

### Filter Change Triggers

```html
<select hx-get="@Url.Action("Index")"
        hx-target="#product-list"
        hx-swap="innerHTML"
        hx-trigger="change"
        hx-include="[name='searchTerm'], [name='sortBy'], [name='sortOrder'], [name='inStock']">
```

**Behavior:**
1. User changes filter dropdown
2. HTMX sends GET request immediately (on `change` event)
3. Includes all other state (search, sort, other filters)
4. Controller returns filtered partial view
5. List updates without page reload

### State Preservation

Filters preserve selected values:

```cshtml
<option value="true" @(Model.Filters["inStock"] == "true" ? "selected" : "")>Yes</option>
```

View model includes filter dictionary:

```csharp
public class ProductListViewModel
{
    public Dictionary<string, string?> Filters { get; set; } = new();
}
```

## Customization

### Add Custom Filter Logic

```csharp
private IQueryable<Product> ApplyFilters(
    IQueryable<Product> query,
    bool? inStock = null,
    string? category = null,      // Custom string filter
    decimal? minPrice = null)     // Custom numeric filter
{
    if (inStock.HasValue)
    {
        query = query.Where(x => x.InStock == inStock.Value);
    }
    
    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(x => x.Category == category);
    }
    
    if (minPrice.HasValue)
    {
        query = query.Where(x => x.Price >= minPrice.Value);
    }
    
    return query;
}
```

### Change Filter Labels

```cshtml
<label class="label">
    <span class="label-text">Stock Status</span>  <!-- Custom label -->
</label>
<select name="inStock" ...>
    <option value="">All Products</option>
    <option value="true">In Stock</option>
    <option value="false">Out of Stock</option>
</select>
```

### Add Filter Reset Button

```cshtml
<button hx-get="@Url.Action("Index")"
        hx-vals='{"inStock": null, "featuredProduct": null}'
        hx-target="#product-list"
        class="btn btn-outline">
    Clear All Filters
</button>
```

### Custom Filter UI

Radio buttons instead of dropdown:

```cshtml
<div class="form-control">
    <label class="label">In Stock</label>
    <div class="flex gap-2">
        <label class="label cursor-pointer">
            <input type="radio" name="inStock" value="" 
                   hx-get="@Url.Action("Index")"
                   hx-target="#product-list"
                   class="radio" />
            <span class="label-text">All</span>
        </label>
        <label class="label cursor-pointer">
            <input type="radio" name="inStock" value="true" class="radio" />
            <span class="label-text">Yes</span>
        </label>
        <label class="label cursor-pointer">
            <input type="radio" name="inStock" value="false" class="radio" />
            <span class="label-text">No</span>
        </label>
    </div>
</div>
```

## Advanced Patterns

### Enum Filters (Future Feature)

```csharp
public enum ProductStatus { Draft, Published, Archived }

// Future support for enum filters
--fields "Title:string Status:ProductStatus:f"
```

### Date Range Filters

```csharp
public async Task<IActionResult> Index(
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    var query = _context.Products.AsQueryable();
    
    if (startDate.HasValue)
    {
        query = query.Where(x => x.CreatedDate >= startDate.Value);
    }
    
    if (endDate.HasValue)
    {
        query = query.Where(x => x.CreatedDate <= endDate.Value);
    }
    
    return View(await query.ToListAsync());
}
```

### Numeric Range Filters

```csharp
public async Task<IActionResult> Index(
    decimal? minPrice = null,
    decimal? maxPrice = null)
{
    var query = _context.Products.AsQueryable();
    
    if (minPrice.HasValue)
    {
        query = query.Where(x => x.Price >= minPrice.Value);
    }
    
    if (maxPrice.HasValue)
    {
        query = query.Where(x => x.Price <= maxPrice.Value);
    }
    
    return View(await query.ToListAsync());
}
```

## Best Practices

### 1. Only Filter Important Fields

```bash
# Good: Only key status filters
--fields "Name:string InStock:bool:f IsActive:bool:f"

# Avoid: Too many filters clutter UI
--fields "Name:string Flag1:bool:f Flag2:bool:f Flag3:bool:f Flag4:bool:f Flag5:bool:f"
```

### 2. Show Filter Count

```cshtml
@if (Model.Filters.Values.Any(v => !string.IsNullOrEmpty(v)))
{
    var activeCount = Model.Filters.Values.Count(v => !string.IsNullOrEmpty(v));
    <span class="badge badge-primary">@activeCount active filters</span>
}
```

### 3. Provide Clear Labels

```cshtml
<!-- Good: Clear label -->
<span class="label-text">Stock Status</span>

<!-- Avoid: Technical field name -->
<span class="label-text">InStock</span>
```

## Performance

### Index Boolean Columns

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.InStock);
        
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.FeaturedProduct);
}
```

### Composite Indexes

For common filter combinations:

```csharp
modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.InStock, p.FeaturedProduct });
```

## Related Features

- [Pagination](./pagination.md) - Filter paginated results
- [Search](./search.md) - Filter search results  
- [Sorting](./sorting.md) - Sort filtered results
- [Field Flags](../cli/generate-controller.md#field-flags) - Control filterable fields

## See Also

- [HTMX Documentation](https://htmx.org/docs/)
- [DaisyUI Select Component](https://daisyui.com/components/select/)

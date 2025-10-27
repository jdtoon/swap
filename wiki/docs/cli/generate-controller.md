---
sidebar_position: 4
---

# swap generate controller

Generate complete CRUD controllers with HTMX-powered views, pagination, search, sorting, filtering, and Entity Framework Core integration.

## Synopsis

```bash
swap generate controller <name> --fields <field-definitions>
swap g c <name> --fields <field-definitions>  # Short alias
```

## Description

The `generate controller` command creates a modern, full-featured MVC controller with:

- **Complete CRUD operations** (Create, Read, Update, Delete) via HTMX modals
- **Pagination** with configurable page sizes (10, 25, 50, 100)
- **Search** with real-time filtering (500ms debounce)
- **Column Sorting** with ascending/descending toggle and field-level control
- **Boolean Filtering** with dropdown filters (All/Yes/No) and field-level control
- **Field-Level Flags** to control sortable/filterable behavior per field
- **HTMX Integration** for zero-page-reload user experience
- **DaisyUI Styling** for modern, accessible UI components
- **Async/await patterns** for all database operations
- **Model validation** with client and server-side support
- **Entity Framework Core** DbContext integration
- **Automatic DbContext** updates (adds DbSet if not exists)

## Arguments

### `<name>`

**Required.** The name of the entity for which to generate a controller.

- Will create the model class if it doesn't exist
- Used for controller, model, view model, and view names
- Must be PascalCase (e.g., `Product`, `CustomerOrder`)

**Examples:**
```bash
swap g c Product --fields "Name:string Price:decimal"
swap g c Customer --fields "Name:string Email:string"
swap g c BlogPost --fields "Title:string Content:string"
```

### `--fields <field-definitions>`

**Required.** Space-separated list of field definitions.

**Format:** `FieldName:Type[:Flags]`

**Supported Types (11 total):**
- `string` - Text data
- `int` - 32-bit integer
- `long` - 64-bit integer
- `short` - 16-bit integer
- `byte` - 8-bit unsigned integer
- `decimal` - High-precision decimal numbers (financial data)
- `float` - Single-precision floating point
- `double` - Double-precision floating point
- `bool` - Boolean true/false values
- `DateTime` - Date and time values
- `Guid` - Globally unique identifiers

**Nullable Types:**
Add `?` after the type name to make it nullable:
```bash
--fields "Description:string? Notes:string?"
```

### Field Flags

Control sorting and filtering behavior per field:

- `:sortable` or `:s` - Enable sorting (default for all fields)
- `:nosort` or `:ns` - Disable sorting
- `:filterable` or `:f` - Enable filtering (for bool fields only)

**Default Behavior:**
- All fields are sortable by default
- Bool fields are NOT filterable by default

**Examples:**
```bash
# Simple fields (all sortable by default)
--fields "Name:string Price:decimal Quantity:int"

# Nullable fields
--fields "Name:string Description:string? Price:decimal"

# Disable sorting on specific fields
--fields "Name:string SKU:string:ns Price:decimal:nosort"

# Enable filtering on bool fields
--fields "Name:string InStock:bool:f Active:bool:filterable"

# Combine flags (not sortable but filterable)
--fields "Name:string InStock:bool:ns,f"

# Real-world example: Product with control flags
--fields "Name:string Price:decimal:ns SKU:string:ns InStock:bool:f CreatedDate:DateTime"
# Result:
# - Name: sortable (default), no filter
# - Price: not sortable, no filter
# - SKU: not sortable, no filter
# - InStock: sortable (default), has filter dropdown
# - CreatedDate: sortable (default), no filter
```

## Generated Files

### Controller

**Location:** `Controllers/[Name]Controller.cs`

**Contains:**
- `Index(pageNumber, pageSize, searchTerm, sortBy, sortOrder, ...filters)` - Paginated, searchable, sortable, filterable list with HTMX support
- `Create()` [GET] - Returns create modal partial view
- `Create(entity)` [POST] - Saves new entity, returns HTMX trigger for list refresh
- `Edit(id)` [GET] - Returns edit modal partial view with entity data
- `Edit(id, entity)` [POST] - Updates entity, returns HTMX trigger for list refresh
- `Details(id)` [GET] - Returns read-only details partial view
- `Delete(id)` [POST] - Deletes entity, returns HTMX trigger for list refresh
- **`BulkDelete(ids)` [POST]** - Deletes multiple entities in transaction, returns JSON with count
- `ApplySorting(query, sortBy, sortOrder)` - Private method applying column sorting (only for sortable fields)
- `ApplyFilters(query, ...filters)` - Private method applying boolean filters (only for filterable fields)

**Key Features:**
- HTMX-first design with `HX-Request` detection
- Returns partial views for HTMX, full views for initial load
- Uses `HX-Trigger` header for client-side events (list refresh, toast notifications)
- Uses `HX-Retarget` and `HX-Reswap` for validation errors
- Async/await for all database operations

### Model

**Location:** `Models/[Name].cs`

**Contains:**
- Entity class with all specified fields
- Auto-generated `Id` property (int, primary key)
- Data annotations for validation
- Nullable reference/value type support
- DateTime fields default to `DateTime.Now` in create modal

**Example:**
```csharp
public class Product
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public decimal Price { get; set; }
    
    public bool InStock { get; set; }
    
    public DateTime CreatedDate { get; set; }
}
```

### View Model

**Location:** `ViewModels/[Name]ListViewModel.cs`

**Contains:**
- `Items` - List of entities for current page
- `Pagination` - PaginationDto with page metadata
- `SearchTerm` - string? for current search query
- `SortBy` - string? for current sort column
- `SortOrder` - string? for current sort direction
- `Filters` - Dictionary with string keys and nullable string values for active filters

**Example:**
```csharp
public class ProductListViewModel
{
    public List<Product> Items { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
    public Dictionary<string, string> Filters { get; set; } = new();
}
```

### Views

**Location:** `Views/[Name]/`

#### `Index.cshtml` - Main Container
- Hero section with title
- "Create Entity" button (opens modal via HTMX)
- Search input with 500ms debounce
- Filter section with dropdowns (only for filterable bool fields)
- `#entity-list` div container for partial updates
- Hidden inputs for sort state preservation
- Modal container div for HTMX-loaded modals

#### `_EntityList.cshtml` - Table Partial
- **Bulk selection checkbox column** with select-all in header
- **Bulk actions bar** (appears when items selected)
- Table with sortable/non-sortable headers based on field flags
- Sortable headers: clickable buttons with HTMX, sort indicators (↑/↓)
- Non-sortable headers: plain `<th>` text
- **Row checkboxes** for bulk selection
- Action buttons (Details, Edit, Delete) with HTMX
- Empty state with helpful message
- Pagination controls at bottom
- **JavaScript functions** for selection management and bulk delete

#### `_EntityCreateModal.cshtml` - Create Modal
- DaisyUI modal dialog
- Form with validation
- DateTime fields pre-populated with DateTime.Now
- HTMX post with validation support
- Cancel button closes modal

#### `_EntityEditModal.cshtml` - Edit Modal
- Similar to create modal
- Pre-populated with entity data
- HTMX post with validation support

#### `_EntityDetails.cshtml` - Details View
- Read-only display of entity
- Formatted values (dates, decimals, booleans as badges)
- Close button

#### `_EntityForm.cshtml` - Shared Form Partial
- Used by both create and edit modals
- All field types with proper inputs:
  - `string`: text input
  - `int/long/short/byte`: number input
  - `decimal/float/double`: number input with step="any"
  - `bool`: checkbox
  - `DateTime`: datetime-local input
  - `Guid`: text input
- Validation spans for each field
- Nullable field support

**Location:** `Views/Shared/`

#### `_PaginationControls.cshtml`
- Reusable pagination component
- Page size dropdown (10, 25, 50, 100)
- First/Previous/Next/Last buttons
- Page info display ("Showing X-Y of Z items")
- HTMX-powered for partial updates
- Preserves search, sort, and filter state

## Examples

### Basic Product Controller

```bash
swap g c Product --fields "Name:string Price:decimal InStock:bool CreatedDate:DateTime"
```

**Generated Index Action:**
```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1, 
    int pageSize = 10, 
    string? searchTerm = null, 
    string? sortBy = null, 
    string? sortOrder = "asc",
    bool? inStock = null)  // Filter parameter (InStock marked :f)
{
    var isHtmxRequest = Request.Headers.ContainsKey("HX-Request");
    
    var query = _context.Products.AsQueryable();
    
    // Search across string fields
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(x => x.Name.ToLower().Contains(searchTerm.ToLower()));
    }
    
    // Apply filters
    query = ApplyFilters(query, inStock);
    
    // Apply sorting
    if (!string.IsNullOrWhiteSpace(sortBy))
    {
        query = ApplySorting(query, sortBy, sortOrder ?? "asc");
    }
    
    var totalItems = await query.CountAsync();
    var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    
    // Build view model
    var viewModel = new ProductListViewModel
    {
        Items = items,
        SearchTerm = searchTerm,
        SortBy = sortBy,
        SortOrder = sortOrder,
        Filters = new Dictionary<string, string> { { "inStock", inStock?.ToString().ToLower() } },
        Pagination = new PaginationDto { /* ... */ }
    };
    
    return isHtmxRequest 
        ? PartialView("_ProductList", viewModel)  // HTMX partial
        : View(viewModel);  // Full page
}
```

### Field Flags in Action

```bash
# E-Commerce Product with controlled sorting/filtering
swap g c Product --fields "Name:string SKU:string:ns Price:decimal:ns Description:string? InStock:bool:f FeaturedProduct:bool:f CreatedDate:DateTime"
```

**Result:**
- **Name**: Sortable header with ↑/↓ indicators
- **SKU**: Plain text header (`:ns` = not sortable)
- **Price**: Plain text header (`:ns` = not sortable)
- **Description**: Sortable header (default)
- **InStock**: Sortable header + filter dropdown (`:f` = filterable)
- **FeaturedProduct**: Sortable header + filter dropdown (`:f`)
- **CreatedDate**: Sortable header (default)

**Generated ApplySorting Method:**
```csharp
private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
{
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy?.ToLower() switch
    {
        "name" => isDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
        "description" => isDescending ? query.OrderByDescending(x => x.Description) : query.OrderBy(x => x.Description),
        "instock" => isDescending ? query.OrderByDescending(x => x.InStock) : query.OrderBy(x => x.InStock),
        "featuredproduct" => isDescending ? query.OrderByDescending(x => x.FeaturedProduct) : query.OrderBy(x => x.FeaturedProduct),
        "createddate" => isDescending ? query.OrderByDescending(x => x.CreatedDate) : query.OrderBy(x => x.CreatedDate),
        // Note: SKU and Price are NOT included (marked :ns)
        _ => query
    };
}
```

**Generated ApplyFilters Method:**
```csharp
private IQueryable<Product> ApplyFilters(IQueryable<Product> query, bool? inStock = null, bool? featuredProduct = null)
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

### Blog System

```bash
# Blog posts with rich content
swap g c Post --fields "Title:string Slug:string:ns Content:string? AuthorName:string Published:bool:f PublishedDate:DateTime? ViewCount:int:ns"
```

**Features:**
- Title: sortable
- Slug: NOT sortable (`:ns` - don't want to sort by URL slug)
- Content: sortable (though unlikely to use)
- AuthorName: sortable
- Published: sortable + filterable (`:f` - filter published/draft posts)
- PublishedDate: sortable
- ViewCount: NOT sortable (`:ns` - popularity metric, not sort field)

### Inventory Management

```bash
# Warehouse items with detailed tracking
swap g c InventoryItem --fields "SKU:string:ns ProductName:string Quantity:int LowStockThreshold:int:ns InStock:bool:f Location:string ReorderRequired:bool:f LastRestocked:DateTime"
```

**Strategic Flags:**
- SKU: Not sortable (identifier, not sort field)
- ProductName: Sortable (main sort field)
- Quantity: Sortable (sort by stock level)
- LowStockThreshold: Not sortable (configuration value)
- InStock: Filterable (quick filter for out-of-stock)
- Location: Sortable (sort by warehouse location)
- ReorderRequired: Filterable (flag items needing reorder)
- LastRestocked: Sortable (sort by freshness)

## Workflow

### Complete CRUD Setup with Migrations

```bash
# 1. Generate controller (creates model, views, controller automatically)
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# 2. Create migration
dotnet ef migrations add AddProduct

# 3. Apply to database
dotnet ef database update

# 4. Run application
dotnet run

# 5. Navigate to http://localhost:5000/Product
#    - See paginated list
#    - Click "Create Product" button (opens modal)
#    - Fill form and submit (HTMX updates list)
#    - Click column headers to sort
#    - Use InStock filter dropdown
#    - Search by name
```

### Multiple Related Controllers

```bash
# E-commerce system
swap g c Category --fields "Name:string Description:string?"
swap g c Product --fields "Name:string CategoryId:int Price:decimal InStock:bool:f"
swap g c Customer --fields "Name:string Email:string Phone:string?"
swap g c Order --fields "CustomerId:int OrderDate:DateTime Total:decimal Status:string"

# Run migrations
dotnet ef migrations add InitialEcommerce
dotnet ef database update
```

## HTMX Features

### Modal CRUD Operations

**Create Button:**
```html
<button class="btn btn-primary"
        hx-get="@Url.Action("Create")"
        hx-target="#modal-container"
        hx-swap="innerHTML">
    Create Product
</button>
```

**Edit Button:**
```html
<button hx-get="@Url.Action("Edit", new { id = item.Id })"
        hx-target="#modal-container"
        hx-swap="innerHTML"
        class="btn btn-sm btn-primary">
    Edit
</button>
```

**Form Submission:**
```html
<form hx-post="@Url.Action("Create")"
      hx-target="#modal-container"
      hx-swap="innerHTML">
    <!-- form fields -->
</form>
```

### Search with Debouncing

```html
<input type="text" 
       name="searchTerm"
       placeholder="Search products..." 
       hx-get="@Url.Action("Index")"
       hx-trigger="input changed delay:500ms, search"
       hx-target="#product-list"
       hx-swap="innerHTML"
       hx-include="[name='pageSize'], [name='sortBy'], [name='sortOrder']" />
```

### Sortable Column Headers

```html
<th>
    <button hx-get="@Url.Action("Index")"
            hx-target="#product-list"
            hx-swap="innerHTML"
            hx-vals='{"sortBy": "name", "sortOrder": "@(Model.SortOrder == "asc" ? "desc" : "asc")"}'
            class="flex items-center gap-1">
        Name
        @if (Model.SortBy == "name")
        {
            <span>@(Model.SortOrder == "desc" ? "↓" : "↑")</span>
        }
    </button>
</th>
```

### Filter Dropdowns

```html
<select name="inStock" 
        hx-get="@Url.Action("Index")"
        hx-target="#product-list"
        hx-swap="innerHTML"
        hx-include="[name='searchTerm'], [name='sortBy'], [name='sortOrder']">
    <option value="">All</option>
    <option value="true">Yes</option>
    <option value="false">No</option>
</select>
```

### Success Notifications

```csharp
// In controller after successful create
Response.Headers.Append("HX-Trigger", 
    "{\"refreshProductList\": null, \"showToast\": {\"type\": \"success\", \"message\": \"Product created!\"}}");
```

## Customization Tips

### Disable Sorting on Identifier Fields

```bash
# Don't allow sorting by SKU, GUID, or internal IDs
swap g c Product --fields "Id:Guid:ns Name:string SKU:string:ns"
```

### Enable Filtering Only on Key Status Fields

```bash
# Only filter by important boolean flags
swap g c Task --fields "Title:string IsComplete:bool:f IsPriority:bool:f IsArchived:bool:f CreatedDate:DateTime"
```

### Combine Flags for Special Cases

```bash
# Field that's filterable but not sortable
swap g c Product --fields "Name:string InStock:bool:ns,f"
# InStock will have a filter dropdown but plain text header (not sortable)
```

## Related Commands

- [`swap generate model`](./generate-model.md) - Generate model class only
- [`swap new`](./new.md) - Create new Swap project

## Next Steps

After generating your controller:

1. **Review Generated Code** - Check controller, model, views
2. **Run Migrations** - `dotnet ef migrations add` and `dotnet ef database update`
3. **Customize Views** - Adjust DaisyUI styling, add custom fields
4. **Add Business Logic** - Implement validation, authorization, custom methods
5. **Test HTMX Features** - Verify modals, sorting, filtering, pagination
6. **Add Related Controllers** - Generate controllers for related entities

## See Also

- [Features: Pagination](../features/pagination.md)
- [Features: Search](../features/search.md)
- [Features: Sorting](../features/sorting.md)
- [Features: Filtering](../features/filtering.md)
- [Features: Bulk Operations](../features/bulk-operations.md)


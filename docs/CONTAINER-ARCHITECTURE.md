# Container Architecture Pattern

**Last Updated**: October 29, 2025  
**Status**: ✅ Production Ready  
**Framework**: Swap.Htmx v0.0.1

---

## Overview

The **Container Architecture Pattern** is the foundational approach for building HTMX-powered applications with Swap. It provides a clean separation between static page structure and dynamic content, enabling lightning-fast navigation and seamless component updates.

This pattern is the result of extensive testing and refinement, representing the **core vision of Swap**: simple, server-rendered applications with modern SPA-like interactions.

---

## 🎯 The Three-Layer Architecture

### Layer 1: Shell (`_Layout.cshtml`)
The outermost container - loaded once per session.

```html
<body>
    <nav><!-- Navigation menu --></nav>
    
    <!-- Main content container - swapped on navigation -->
    <main id="main-content">
        @RenderBody()
    </main>
    
    <footer><!-- Footer --></footer>
</body>
```

**Characteristics:**
- ✅ Loads once when user first visits
- ✅ Contains site-wide elements (nav, footer)
- ✅ Defines `#main-content` as primary swap target
- ✅ Never reloads unless full page refresh

---

### Layer 2: Page Container (`Index.cshtml`)
The page-specific container with static elements.

```html
@{
    ViewData["Title"] = "Products";
}

<!-- Static Hero Section -->
<div class="hero bg-base-200 py-8">
    <div class="hero-content text-center">
        <h1 class="text-5xl font-bold">Products</h1>
        <p class="py-6">Manage your products with HTMX-powered interactions</p>
    </div>
</div>

<div class="container mx-auto px-4 py-8">
    <div class="card bg-base-100 shadow-xl">
        <div class="card-body">
            <!-- Static Controls -->
            <div class="flex justify-between items-center mb-4">
                <h2 class="card-title">Product List</h2>
                <button class="btn btn-primary"
                        hx-get="@Url.Action("Create")"
                        hx-target="#modal-container"
                        hx-swap="innerHTML">
                    Create Product
                </button>
            </div>
            
            <!-- Static Search Bar -->
            <div class="mb-4">
                <input type="text" 
                       name="searchTerm"
                       placeholder="Search products..." 
                       class="input input-bordered w-full"
                       hx-get="@Url.Action("GetProductList")"
                       hx-trigger="input changed delay:500ms, search"
                       hx-target="#product-list"
                       hx-swap="outerHTML"
                       hx-include="[name='pageSize'], [name='sortBy'], [name='sortOrder']" />
                
                <!-- Hidden inputs for state preservation -->
                <input type="hidden" name="sortBy" value="" />
                <input type="hidden" name="sortOrder" value="" />
            </div>

            <!-- Dynamic Component (Layer 3) - Loads via HTMX -->
            <div hx-get="@Url.Action("GetProductList")" 
                 hx-trigger="load, refreshProductList from:body"
                 hx-swap="outerHTML">
                <div class="flex items-center justify-center py-8">
                    <span class="loading loading-spinner loading-lg"></span>
                    <span class="ml-2">Loading products...</span>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Modal Container -->
<div id="modal-container"></div>
```

**Characteristics:**
- ✅ Contains static page elements (hero, search, buttons)
- ✅ No heavy data loading - renders instantly
- ✅ Defines containers for dynamic components
- ✅ Swapped into `#main-content` on menu navigation

---

### Layer 3: Dynamic Component (`_ProductList.cshtml`)
The data-driven component that updates frequently.

```html
@model ProductListViewModel

<div id="product-list" 
     hx-get="@Url.Action("GetProductList")" 
     hx-trigger="refreshProductList from:body"
     hx-swap="outerHTML"
     hx-include="[name='searchTerm'], [name='pageSize'], [name='sortBy'], [name='sortOrder']">
     
    @if (!Model.Items.Any())
    {
        <div class="alert alert-info">
            <span>No products found.</span>
        </div>
    }
    else
    {
        <!-- Bulk Actions Bar -->
        <div id="bulk-actions" 
             hx-get="@Url.Action("BulkActionsBar")"
             hx-trigger="selectionChanged from:body"
             hx-swap="outerHTML">
            @* Server-rendered selection state *@
        </div>
    
        <!-- Data Table -->
        <div class="overflow-x-auto">
            <table class="table table-zebra w-full">
                <thead>
                    <tr>
                        <th>
                            <input type="checkbox" 
                                   id="select-all" 
                                   class="checkbox checkbox-sm"
                                   hx-post="@Url.Action("ToggleSelectAll")?pageNumber=@Model.Pagination.CurrentPage"
                                   hx-target="#product-list"
                                   hx-swap="outerHTML" />
                        </th>
                        <th>
                            <button class="flex items-center gap-1 hover:text-primary"
                                    hx-get="@Url.Action("GetProductList")"
                                    hx-target="#product-list"
                                    hx-swap="outerHTML"
                                    hx-vals='{"sortBy": "name", "sortOrder": "asc"}'>
                                Name
                            </button>
                        </th>
                        <th>Price</th>
                        <th>Stock</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model.Items)
                    {
                        <tr>
                            <td>
                                <input type="checkbox" 
                                       hx-post="@Url.Action("ToggleSelection", new { id = item.Id })"
                                       hx-target="#product-list"
                                       hx-swap="outerHTML" />
                            </td>
                            <td>@item.Name</td>
                            <td>@item.Price</td>
                            <td>@item.Stock</td>
                            <td>
                                <button hx-get="@Url.Action("Edit", new { id = item.Id })"
                                        hx-target="#modal-container">
                                    Edit
                                </button>
                                <button hx-delete="@Url.Action("Delete", new { id = item.Id })"
                                        hx-confirm="Delete this product?"
                                        hx-target="#product-list"
                                        hx-swap="outerHTML">
                                    Delete
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
        
        <!-- Pagination -->
        @await Html.PartialAsync("_PaginationControls", Model.Pagination)
    }
</div>
```

**Characteristics:**
- ✅ Loads data from database
- ✅ Wraps entire content in container with `id` and `hx-trigger`
- ✅ Listens for custom events (`refreshProductList`)
- ✅ Swaps itself (`outerHTML`) on updates
- ✅ Preserves state via hidden inputs in parent

---

## 🏗️ Controller Architecture

### Index Action - Returns Static Page
```csharp
/// <summary>
/// Index action - returns the static page container
/// </summary>
public IActionResult Index()
{
    // Index page is just static content - search bar, hero, etc.
    // The list loads separately via GetProductList
    var viewModel = new ProductListViewModel
    {
        SearchTerm = "",
        SortBy = "",
        SortOrder = "asc"
    };
    
    return SwapView(viewModel);
}
```

**Key Points:**
- ✅ No database queries - returns immediately
- ✅ Uses `SwapView()` from `Swap.Htmx` framework
- ✅ Returns full `Index.cshtml` on initial load
- ✅ Returns partial `Index.cshtml` on menu navigation (HTMX request)

---

### Component Action - Returns Dynamic Data
```csharp
/// <summary>
/// Separate endpoint for the product list component with pagination, search, sorting, and filtering
/// </summary>
[HttpGet]
public async Task<IActionResult> GetProductList(
    int pageNumber = 1, 
    int pageSize = 10, 
    string? searchTerm = null, 
    string? sortBy = null, 
    string? sortOrder = "asc")
{
    // Build query with search filter
    var query = _context.Products.AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(x => x.Name.ToLower().Contains(searchTerm.ToLower()));
    }

    // Apply sorting
    if (!string.IsNullOrWhiteSpace(sortBy))
    {
        query = ApplySorting(query, sortBy, sortOrder ?? "asc");
    }

    // Get total count for pagination
    var totalItems = await query.CountAsync();
    
    // Apply pagination
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // Build view model
    var viewModel = new ProductListViewModel
    {
        Items = items,
        SearchTerm = searchTerm ?? string.Empty,
        SortBy = sortBy ?? string.Empty,
        SortOrder = sortOrder,
        Pagination = new PaginationDto
        {
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            HxGetUrl = Url.Action("GetProductList") ?? string.Empty,
            HxTarget = "#product-list",
            HxSwap = "outerHTML"
        }
    };

    // Pass selection state to view
    ViewBag.SelectedIds = HttpContext.Session.GetObject<HashSet<int>>("Product_Selection") 
        ?? new HashSet<int>();

    // Return just the _ProductList partial
    return PartialView("_ProductList", viewModel);
}
```

**Key Points:**
- ✅ Performs database queries
- ✅ Returns only the `_ProductList` partial
- ✅ Always returns partial (no `SwapView()` needed)
- ✅ Handles search, sort, pagination parameters

---

## 🔄 Navigation Flow

### Initial Page Load (Full Page)
```
User visits /Product
    ↓
Browser requests /Product (no HX-Request header)
    ↓
Index() returns full Index.cshtml with layout
    ↓
Browser renders page
    ↓
HTMX detects hx-trigger="load" on list container
    ↓
HTMX requests /Product/GetProductList
    ↓
GetProductList() returns _ProductList partial
    ↓
HTMX swaps partial into list container
    ↓
Page fully loaded
```

---

### Menu Navigation (HTMX)
```
User clicks "Products" menu link (hx-get="/Product" hx-target="#main-content")
    ↓
HTMX requests /Product (WITH HX-Request header)
    ↓
Index() detects HTMX request via SwapView()
    ↓
Index() returns Index.cshtml WITHOUT layout
    ↓
HTMX swaps Index.cshtml into #main-content
    ↓
HTMX detects hx-trigger="load" on list container
    ↓
HTMX requests /Product/GetProductList
    ↓
GetProductList() returns _ProductList partial
    ↓
HTMX swaps partial into list container
    ↓
Page fully loaded (without full page refresh)
```

---

### Component Update (CRUD Operations)
```
User clicks "Create Product" button
    ↓
HTMX requests /Product/Create (GET)
    ↓
Create() returns _ProductCreateModal partial
    ↓
HTMX swaps modal into #modal-container
    ↓
User fills form and submits
    ↓
HTMX posts to /Product/Create (POST)
    ↓
Create(model) validates and saves
    ↓
Create(model) returns empty response with header:
    HX-Trigger: {"refreshProductList": null}
    ↓
HTMX triggers custom event "refreshProductList" on body
    ↓
#product-list container listens for this event
    ↓
#product-list requests /Product/GetProductList
    ↓
GetProductList() returns updated _ProductList
    ↓
HTMX swaps updated list (product now appears)
    ↓
Modal closes automatically
```

---

## 🎨 SwapView() Framework

The `SwapView()` method is the heart of this architecture, provided by the `Swap.Htmx` framework.

### Basic Usage
```csharp
public class ProductController : SwapController
{
    public IActionResult Index()
    {
        return SwapView(model); // Automatically handles HTMX detection
    }
}
```

### How It Works
```csharp
protected IActionResult SwapView(object? model = null)
{
    return SwapView(null, model);
}

protected IActionResult SwapView(string? viewName, object? model)
{
    // Check if this is an HTMX request
    bool isHtmxRequest = Request.Headers.ContainsKey("HX-Request");
    
    if (isHtmxRequest)
    {
        // Return partial view (no layout)
        return model == null 
            ? PartialView(viewName) 
            : PartialView(viewName, model);
    }
    else
    {
        // Return full view (with layout)
        return model == null 
            ? View(viewName) 
            : View(viewName, model);
    }
}
```

**Benefits:**
- ✅ No manual `IsHtmxRequest()` checks in controllers
- ✅ Single action serves both full page and HTMX requests
- ✅ Consistent pattern across all controllers
- ✅ Framework-level abstraction

---

## ⚡ Performance Benefits

### Fast Initial Load
```
Traditional Full Page:
- Layout: 100ms
- Index.cshtml: 50ms
- Database query: 200ms
- Render list: 150ms
TOTAL: 500ms
```

```
Container Architecture:
- Layout: 100ms
- Index.cshtml: 50ms (no DB query)
- Show loading spinner: 0ms
VISIBLE: 150ms ⚡ (3.3x faster)

Then async:
- Database query: 200ms
- Render list: 150ms
- Swap into place: 0ms
COMPLETE: 500ms (same total, but feels faster)
```

### Lightning-Fast Navigation
```
Traditional Navigation:
- Full page reload: 500ms
- Layout re-renders: wasteful
- JavaScript re-initializes: slow
```

```
Container Architecture:
- HTMX swap: 50ms ⚡
- No layout reload: 0ms
- No JS re-init: 0ms
TOTAL: 50ms (10x faster)
```

---

## 🛠️ Implementation Checklist

### ✅ Controller Setup
- [ ] Inherit from `SwapController`
- [ ] Create `Index()` action that returns `SwapView()` with minimal data
- [ ] Create `Get{Entity}List()` action that queries database and returns `PartialView()`
- [ ] All CRUD actions return appropriate responses:
  - GET Create/Edit: Return modal partial
  - POST Create/Edit: Return empty response with `HX-Trigger` header
  - DELETE: Return empty response with `HX-Trigger` header

### ✅ View Setup
- [ ] `Index.cshtml` contains only static elements (hero, search, buttons)
- [ ] `Index.cshtml` has container with `hx-get` pointing to `Get{Entity}List`
- [ ] `Index.cshtml` has `hx-trigger="load"` for initial load
- [ ] `_EntityList.cshtml` wraps content in `<div id="entity-list">`
- [ ] `_EntityList.cshtml` has `hx-trigger="refresh{Entity}List from:body"`
- [ ] `_EntityList.cshtml` uses `hx-swap="outerHTML"` on self

### ✅ Navigation Setup
- [ ] Menu links use `hx-get="/Controller" hx-target="#main-content"`
- [ ] Menu links include `hx-push-url="true"` for browser history
- [ ] `_Layout.cshtml` defines `<main id="main-content">` container

### ✅ Event System
- [ ] CRUD actions trigger custom events: `{"refresh{Entity}List": null}`
- [ ] List component listens for event: `hx-trigger="refresh{Entity}List from:body"`
- [ ] Modals use `#modal-container` as target
- [ ] Modal closes on successful operation (automatic via event)

---

## 📝 Code Generation

The `swap generate controller` command automatically implements this pattern:

```bash
swap generate controller Product --fields "Name:string Price:decimal Stock:int"
```

**Generates:**
- ✅ Controller with `Index()` and `GetProductList()` actions
- ✅ `Index.cshtml` with static container structure
- ✅ `_ProductList.cshtml` with dynamic list and event listeners
- ✅ All CRUD modals with proper HTMX attributes
- ✅ Pagination, sorting, searching, bulk selection
- ✅ Full event-driven refresh system

---

## 🎯 When to Use This Pattern

### ✅ Perfect For:
- CRUD applications
- Admin panels
- Dashboards with multiple widgets
- List/table views with filtering
- Any page with static + dynamic content

### ❌ Not Ideal For:
- Single-page forms (use simpler partial pattern)
- Static content pages (no dynamic component needed)
- Real-time streaming data (consider WebSockets)

---

## 🚀 Next Steps

1. **Generate your first controller**: `swap generate controller Task`
2. **Study the generated code**: See the pattern in action
3. **Customize as needed**: The pattern is a starting point
4. **Read the docs**: Check out the [HTMX documentation](https://htmx.org/)

---

## 📚 Related Documentation

- [Swap.Htmx Framework](../framework/Swap.Htmx/README.md)
- [Pattern Library](./PATTERNS-LIBRARY.md)
- [CLI Commands](../tools/Swap.CLI/README.md)
- [Testing Guide](../framework/Swap.Testing/README.md)

---

**This pattern represents the core vision of Swap: simple, server-rendered applications with modern SPA-like interactions. It's fast, maintainable, and scales beautifully.**

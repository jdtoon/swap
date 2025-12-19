# Swap.Htmx Patterns Cheatsheet

Quick reference for common patterns. Copy-paste ready.

---

## Table of Contents

1. [SwapController Patterns](#swapcontroller-patterns)
2. [SwapResponse Patterns](#swapresponse-patterns)
3. [SwapState Patterns](#swapstate-patterns)
4. [Event Patterns](#event-patterns)
5. [Navigation Patterns](#navigation-patterns)
6. [OOB Swap Patterns](#oob-swap-patterns)
7. [Form Patterns](#form-patterns)
8. [Anti-Patterns](#anti-patterns)

---

## SwapController Patterns

### Basic Controller

```csharp
public class ProductsController : SwapController
{
    public IActionResult Index()
    {
        // Returns View for normal requests, PartialView for HTMX
        return SwapView(model);
    }
}
```

### Extension Method (No Inheritance)

```csharp
public class ProductsController : Controller
{
    public IActionResult Index()
    {
        return this.SwapView(model);
    }
}
```

### Explicit View Name

```csharp
return SwapView("_CustomView", model);
```

### Force Full Page

```csharp
return View(model);  // Always includes layout
```

### Force Partial

```csharp
return PartialView("_Partial", model);  // Never includes layout
```

---

## SwapResponse Patterns

### Simple Response with Toast

```csharp
return SwapResponse()
    .WithView("_Item", item)
    .WithSuccessToast("Saved!")
    .Build();
```

### Multiple OOB Updates

```csharp
return SwapResponse()
    .WithView("_Main", mainModel)
    .AlsoUpdate("sidebar", "_Sidebar", sidebarModel)
    .AlsoUpdate("header", "_Header", headerModel)
    .Build();
```

### With Client-Side Event

```csharp
return SwapResponse()
    .WithView("_Item", item)
    .WithTrigger("itemSaved")
    .WithTrigger("refreshList")
    .Build();
```

### Delete/Remove Element

```csharp
return SwapResponse()
    .WithDelete("#item-" + id)
    .WithSuccessToast("Deleted")
    .Build();
```

### Error Response

```csharp
return SwapResponse()
    .WithError("Something went wrong")
    .WithErrorToast("Failed to save")
    .Build();
```

### Redirect

```csharp
return SwapResponse()
    .WithRedirect("/products")
    .Build();
```

---

## SwapState Patterns

### Define State Class

```csharp
public class FilterState : SwapState
{
    public string Category { get; set; } = "all";
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "name";
    public bool Descending { get; set; }
}
```

### Render State Container

```html
@model ProductViewModel
<swap-state state="Model.State" />
```

### Bind State in Controller

```csharp
[HttpGet]
public IActionResult Filter([FromSwapState] FilterState state)
{
    var products = _service.Get(state.Category, state.Search, state.Page);
    return PartialView("_ProductList", new ViewModel { State = state, Products = products });
}
```

### Filter with URL Override (FIRST value wins)

```html
<!-- Category buttons: click sets Category=electronics, Page resets to 1 -->
<button hx-get="/Products/Filter?Category=electronics&Page=1" 
        hx-target="#results" 
        hx-include="#filter-state">
    Electronics
</button>

<!-- Search: keyup sends Search from input, Page resets to 1 -->
<input type="text" 
       name="Search"
       value="@Model.State.Search"
       hx-get="/Products/Filter?Page=1"
       hx-target="#results"
       hx-include="#filter-state"
       hx-trigger="keyup changed delay:300ms" />
```

### Pagination

```html
<button hx-get="/Products/Filter?Page=@(Model.State.Page - 1)"
        hx-target="#results"
        hx-include="#filter-state"
        disabled="@(Model.State.Page <= 1)">
    Previous
</button>

<button hx-get="/Products/Filter?Page=@(Model.State.Page + 1)"
        hx-target="#results"
        hx-include="#filter-state">
    Next
</button>
```

### Sort Toggle

```html
<button hx-get="/Products/Filter?SortBy=price&Descending=@(!Model.State.Descending)&Page=1"
        hx-target="#results"
        hx-include="#filter-state">
    Price @(Model.State.SortBy == "price" ? (Model.State.Descending ? "↓" : "↑") : "")
</button>
```

### Clear All Filters

```html
<!-- Reset to defaults by not including state -->
<button hx-get="/Products/Filter?Category=all&Search=&Page=1"
        hx-target="#results">
    Clear Filters
</button>
```

---

## Event Patterns

### Define Events

```csharp
public static class ProductEvents
{
    public static readonly EventKey Added = new("product:added");
    public static readonly EventKey Updated = new("product:updated");
    public static readonly EventKey Deleted = new("product:deleted");
}
```

### Event Payloads

```csharp
public record ProductPayload(
    int Id,
    string Name,
    string Category,
    decimal Price
);
```

### Event Handler (DI Supported)

```csharp
[SwapHandler(typeof(ProductEvents), nameof(ProductEvents.Added))]
public class UpdateCategoryCountHandler : ISwapEventHandler<ProductPayload>
{
    private readonly ICategoryService _categories;
    
    public UpdateCategoryCountHandler(ICategoryService categories)
    {
        _categories = categories;
    }
    
    public void Handle(SwapEventContext<ProductPayload> context)
    {
        var counts = _categories.GetCounts();
        context.Response.AlsoUpdate("category-counts", "_CategoryCounts", counts);
    }
}
```

### Multiple Handlers for Same Event

```csharp
// Handler 1: Update stats
[SwapHandler(typeof(ProductEvents), nameof(ProductEvents.Added))]
public class StatsHandler : ISwapEventHandler<ProductPayload>
{
    public void Handle(SwapEventContext<ProductPayload> context)
    {
        context.Response.AlsoUpdate("stats", "_Stats", GetStats());
    }
}

// Handler 2: Update recent activity
[SwapHandler(typeof(ProductEvents), nameof(ProductEvents.Added))]
public class ActivityHandler : ISwapEventHandler<ProductPayload>
{
    public void Handle(SwapEventContext<ProductPayload> context)
    {
        context.Response.AlsoUpdate("activity", "_RecentActivity", GetRecent());
    }
}
```

### Fire Event from Controller

```csharp
public IActionResult Add(ProductInput input)
{
    var product = _service.Add(input);
    
    return SwapEvent(ProductEvents.Added, new ProductPayload(
        product.Id, 
        product.Name, 
        product.Category, 
        product.Price
    ))
    .WithView("_ProductRow", product)
    .WithSuccessToast("Product added!")
    .Build();
}
```

### Event Configuration (Program.cs)

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.ConfigureEvents(events =>
    {
        events.On(ProductEvents.Added)
            .AlsoUpdate("product-count", "_ProductCount");
            
        events.On(ProductEvents.Deleted)
            .AlsoUpdate("product-count", "_ProductCount")
            .WithSuccessToast("Product deleted");
    });
});
```

---

## Navigation Patterns

### Basic Navigation

```html
<swap-nav to="/products">Products</swap-nav>
<swap-nav to="/orders">Orders</swap-nav>
```

### With Custom Target

```html
<swap-nav to="/products" target="#sidebar">Products</swap-nav>
```

### Navigation That Doesn't Push URL

```html
<swap-nav to="/quick-view/123" push-url="false">Quick View</swap-nav>
```

### Active State Styling

```html
<swap-nav to="/products" class="nav-link" active-class="active">Products</swap-nav>
```

### Configure Default Target

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.DefaultNavigationTarget = "#main-content";
});
```

### Layout with Navigation

```html
<nav>
    <swap-nav to="/" active-class="active">Home</swap-nav>
    <swap-nav to="/products" active-class="active">Products</swap-nav>
    <swap-nav to="/about" active-class="active">About</swap-nav>
</nav>

<main id="main-content">
    @RenderBody()
</main>
```

---

## OOB Swap Patterns

### Standard OOB

```csharp
return SwapResponse()
    .WithView("_Main", main)
    .AlsoUpdate("target-id", "_Partial", model)
    .Build();
```

### Multiple OOB

```csharp
return SwapResponse()
    .WithView("_Item", item)
    .AlsoUpdate("count", "_Count", count)
    .AlsoUpdate("total", "_Total", total)
    .AlsoUpdate("notifications", "_Badge", badge)
    .Build();
```

### OOB with Custom Swap Strategy

```csharp
return SwapResponse()
    .WithView("_Main", main)
    .AlsoUpdate("list", "_NewItem", item, HxSwap.BeforeEnd)  // Append
    .Build();
```

### OOB Remove

```csharp
return SwapResponse()
    .WithView("_Success", success)
    .AlsoRemove("#temp-banner")
    .Build();
```

---

## Form Patterns

### Basic Form

```html
<form hx-post="/products/add" hx-target="#product-list" hx-swap="beforeend">
    <input name="Name" />
    <input name="Price" type="number" />
    <button type="submit">Add</button>
</form>
```

### Form with Validation

```html
<form hx-post="/products/add" hx-target="#form-container">
    <div id="validation-errors">
        <swap-validation />
    </div>
    <input name="Name" required />
    <input name="Price" type="number" min="0" />
    <button type="submit">Add</button>
</form>
```

### Validation Response

```csharp
public IActionResult Add(ProductInput input)
{
    if (!ModelState.IsValid)
    {
        return SwapValidationErrors();
    }
    
    // Save and return success
}
```

### Form Reset After Success

```html
<form hx-post="/products/add" 
      hx-target="#product-list" 
      hx-swap="beforeend"
      hx-on::after-request="if(event.detail.successful) this.reset()">
```

---

## Anti-Patterns

### ❌ Kitchen Sink Response

```csharp
// BAD: Too many unrelated updates
return SwapResponse()
    .WithView("_Item", item)
    .AlsoUpdate("header", "_Header", header)
    .AlsoUpdate("sidebar", "_Sidebar", sidebar)
    .AlsoUpdate("footer", "_Footer", footer)
    .AlsoUpdate("notifications", "_Notifications", notifications)
    .AlsoUpdate("breadcrumbs", "_Breadcrumbs", crumbs)
    .AlsoUpdate("recent", "_Recent", recent)
    // ... controllers shouldn't know this much about layout
    .Build();
```

**Fix:** Use event handlers to decouple

### ❌ JavaScript State Management

```html
<!-- BAD: Don't manage state in JavaScript -->
<script>
    let filterState = { category: 'all', page: 1 };
</script>
```

**Fix:** Use SwapState with hidden fields

### ❌ Inline HTMX Attributes Everywhere

```html
<!-- BAD: Verbose and repetitive -->
<a href="/products" hx-get="/products" hx-target="#main" hx-push-url="true" hx-swap="innerHTML">Products</a>
<a href="/orders" hx-get="/orders" hx-target="#main" hx-push-url="true" hx-swap="innerHTML">Orders</a>
```

**Fix:** Use swap-nav

```html
<swap-nav to="/products">Products</swap-nav>
<swap-nav to="/orders">Orders</swap-nav>
```

### ❌ Polling Everything

```html
<!-- BAD: Polling for everything -->
<div hx-get="/notifications" hx-trigger="every 1s"></div>
<div hx-get="/messages" hx-trigger="every 1s"></div>
<div hx-get="/updates" hx-trigger="every 1s"></div>
```

**Fix:** Use SSE or WebSockets for real-time

### ❌ Giant Partial Views

```csharp
// BAD: Returning entire page sections
return PartialView("_EntirePageContent", hugeModel);
```

**Fix:** Return minimal HTML, use OOB for related updates

### ❌ Putting State in Query Strings

```csharp
// BAD: Complex state in URLs
"/products?category=electronics&search=phone&page=3&sort=price&dir=asc&min=100&max=500"
```

**Fix:** Use SwapState for complex filter state

---

## Decision Tree

```
Need to respond to HTMX request?
├── Simple view return → SwapView()
├── Need multiple updates → SwapResponse().AlsoUpdate().Build()
├── Need toast notification → .WithSuccessToast() / .WithErrorToast()
├── Need client event → .WithTrigger()
├── Multiple unrelated things need updating → Use Event Handlers
└── Delete element → .WithDelete()

Managing form/filter state?
├── Simple one-off values → Query parameters
├── Complex persistent state → SwapState + hidden fields
└── URL param + hidden field → URL param wins (FIRST value)

Real-time updates?
├── Occasional polling → hx-trigger="every 30s"
├── Server-pushed updates → SSE (SwapRealtimeController.ServerSentEvents())
└── Bidirectional → WebSockets
```

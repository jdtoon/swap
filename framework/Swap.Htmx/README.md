# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Minimal HTMX framework for ASP.NET Core MVC** that provides automatic page/partial detection, toast notifications, out-of-band swaps, and a powerful event system for decoupling domain logic from UI updates.

## Features

- ✅ **SwapController** - Automatic page vs partial rendering based on HX-Request header
- ✅ **Toast Notifications** - Built-in success/error/warning/info toasts with zero JavaScript
- ✅ **Out-of-Band Swaps** - Update multiple page sections in one response
- ✅ **Event System** - Chain domain events to UI updates with static typing
- ✅ **Middleware** - Validates responses and headers automatically
- ✅ **Extension Methods** - Fluent API for HTMX headers and responses

## Installation

```bash
dotnet add package Swap.Htmx
```

## Quick Start

### 1. Register Services & Middleware

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();

var app = builder.Build();

app.UseSwapHtmxShell(); // Validates HTMX responses
app.UseSwapHtmx();      // Adds event handling middleware

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### 2. Create Controller

```csharp
public class ProductController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var products = await _service.GetAllAsync();
        return SwapView(products); // Auto-detects page vs partial
    }
    
    public async Task<IActionResult> Create(ProductDto dto)
    {
        await _service.CreateAsync(dto);
        
        // Show success toast
        Response.ShowSuccessToast("Product created!");
        
        return SwapView("Success");
    }
}
```

### 3. Create View

```razor
@model List<Product>

<div id="product-list">
    <h1>Products</h1>
    
    @foreach (var product in Model)
    {
        <div class="product-card">
            <h3>@product.Name</h3>
            <p>$@product.Price</p>
            
            <button hx-post="/products/delete/@product.Id" 
                    hx-target="#product-list"
                    hx-confirm="Delete this product?">
                Delete
            </button>
        </div>
    }
</div>
```

## Core Concepts

### SwapView() - Automatic Rendering

`SwapView()` automatically returns the correct response type:

```csharp
public async Task<IActionResult> Details(int id)
{
    var product = await _service.GetAsync(id);
    
    // Initial page load: Returns View() with layout
    // HTMX request: Returns PartialView() without layout
    return SwapView("Details", product);
}
```

**How it works:**
- Checks for `HX-Request` header
- HTMX request → `PartialView()` (no layout)
- Normal request → `View()` (with layout)
- Adds `Vary: HX-Request` header for caching

### Toast Notifications

Show user feedback with simple extension methods:

```csharp
Response.ShowSuccessToast("Product saved!");
Response.ShowErrorToast("Something went wrong!");
Response.ShowWarningToast("Please review your changes.");
Response.ShowInfoToast("Processing in background...");
```

**Features:**
- 4 toast types with different colors
- Auto-dismiss after 3 seconds
- Configurable positioning (top-right, bottom-right, etc.)
- Multiple toasts stack vertically
- Pure HTMX - no JavaScript required

[📖 Full Toast Documentation](./TOASTS.md)

### Out-of-Band (OOB) Swaps

Update multiple page sections in a single response:

```csharp
public async Task<IActionResult> AddToCart(int productId)
{
    await _cartService.AddItemAsync(productId);
    
    // Main content
    var main = SwapView("ItemAdded");
    
    // Also update cart total in header (out-of-band)
    var total = await _cartService.GetTotalAsync();
    ViewData["OobCartTotal"] = $@"
        <div id=""cart-total"" hx-swap-oob=""true"">
            {total.ItemCount} items - ${total.Total}
        </div>";
    
    return main;
}
```

**Common use cases:**
- Update header badge counts
- Refresh sidebar panels
- Update multiple dashboard widgets
- Sync item in list after editing details

[📖 Full OOB Swap Documentation](./OOB-SWAPS.md)

### Event System

Chain domain events to UI updates without coupling:

```csharp
// Define event keys (static typing enforced)
public static class ProductEvents
{
    public static readonly EventKey Created = new("product.created");
    public static readonly EventKey Updated = new("product.updated");
}

public static class UiEvents
{
    public static readonly EventKey RefreshList = new("ui.refreshList");
    public static readonly EventKey ShowToast = new("ui.toast.success");
}

// Configure event chains
builder.Services.AddSwapHtmx(events =>
{
    // When product is created, refresh list and show toast
    events.Chain(ProductEvents.Created, 
                 UiEvents.RefreshList, 
                 UiEvents.ShowToast);
});

// In controller, emit domain event
public async Task<IActionResult> Create(ProductDto dto)
{
    await _service.CreateAsync(dto);
    
    // Emit domain event (triggers UI events via chain)
    await _publisher.EmitAsync(ProductEvents.Created);
    
    return SwapView("Success");
}
```

[📖 Full Event System Documentation](./EVENTS.md)

## Extension Methods

### Request Extensions

```csharp
if (Request.IsHtmxRequest())
{
    // Handle HTMX request
}

if (Request.IsBoosted())
{
    // Handle boosted request
}

var currentUrl = Request.GetCurrentUrl();
var target = Request.GetHtmxTarget();
```

### Response Extensions

```csharp
// Set HX-Redirect
Response.HxRedirect("/products");

// Set HX-Refresh
Response.HxRefresh();

// Set HX-Location with context
Response.HxLocation("/products/details/1", new { target = "#main" });

// Trigger client-side events
Response.HxTrigger("productUpdated");
Response.HxTrigger(new { showModal = new { id = 123 } });

// Set HX-Retarget
Response.HxRetarget("#different-element");

// Set HX-Reswap
Response.HxReswap("outerHTML");
```

## Testing

The framework includes comprehensive test coverage:

- **38 Unit Tests** - Verify methods, headers, event chains
- **16 E2E Tests** - Playwright tests in real browsers
  - 6 toast tests
  - 5 OOB swap tests  
  - 4 combined feature tests
  - 1 debug test

```bash
# Run unit tests
cd framework/Swap.Htmx.Tests
dotnet test

# Run E2E tests (requires test app running)
cd framework/Swap.Htmx.E2ETests
dotnet test
```

## Documentation

- [Toast Notifications](./TOASTS.md) - Complete toast API and examples
- [Out-of-Band Swaps](./OOB-SWAPS.md) - Multiple element updates
- [Event System](./EVENTS.md) - Domain event → UI event chains
- [Templates](./TEMPLATES.md) - Project templates and patterns

## Examples

See the test app for complete working examples:

```bash
cd framework/Swap.Htmx.TestApp/src
dotnet run
# Visit http://localhost:5000/test
```

## Philosophy

Swap.Htmx is **minimal by design**:

1. **Automatic View Rendering** - `SwapView()` handles page vs partial logic
2. **Domain→UI Event Mapping** - Emit domain events, UI updates follow
3. **HTMX-Native** - Leverage HTMX's capabilities, don't fight them
4. **Static Typing** - No magic strings for event names

Everything else is just sensible defaults and extension methods.

## Requirements

- .NET 8.0 or higher
- ASP.NET Core MVC
- HTMX 2.0+ (via CDN or npm)

## License

MIT License - see [LICENSE](../../LICENSE) file for details.

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

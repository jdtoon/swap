# Swap.Htmx# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Minimal HTMX framework for ASP.NET Core MVC** that provides automatic page/partial detection, toast notifications, out-of-band swaps, enhanced Server-Sent Events, and a powerful event system for decoupling domain logic from UI updates.

## Features

- ✅ **SwapController** - Automatic page vs partial rendering based on HX-Request header
- ✅ **Toast Notifications** - Built-in success/error/warning/info toasts with zero JavaScript
- ✅ **Out-of-Band Swaps** - Update multiple page sections in one response
- ✅ **Event System** - Chain domain events to UI updates with static typing
- ✅ **Enhanced SSE** - Real-time Server-Sent Events with automatic polling fallback
- ✅ **Connection Management** - Room-based broadcasting with authentication support
- ✅ **Middleware** - Validates responses and headers automatically
- ✅ **Extension Methods** - Fluent API for HTMX headers and responses

- **Event system** - Decouple domain logic from UI updates with typed events

## Installation

## Quick Example

````bash

```csharpdotnet add package Swap.Htmx

// Controller```

public class TodoController : SwapController

{> **📖 [Complete Setup Guide](./GETTING-STARTED.md)** - Step-by-step instructions for setting up toasts, OOB swaps, and all features

    public async Task<IActionResult> Create(TodoDto dto)

    {## Quick Start

        await _service.CreateAsync(dto);

        ### 1. Register Services & Middleware

        Response.ShowSuccessToast("Todo created!");

        return SwapView("TodoList", await _service.GetAllAsync());```csharp

    }var builder = WebApplication.CreateBuilder(args);



    // Real-time updates with SSEbuilder.Services.AddControllersWithViews();

    public IActionResult Stream() => ServerSentEvents(async (stream, ct) =>builder.Services.AddSwapHtmx();

    {

        await foreach (var update in GetUpdatesAsync(ct))var app = builder.Build();

        {

            var html = await this.RenderPartialToStringAsync("_TodoItem", update);app.UseSwapHtmxShell(); // Validates HTMX responses

            await stream.SendEventAsync("todo-added", html);app.UseSwapHtmx();      // Adds event handling middleware

        }

    });app.MapControllerRoute(

}    name: "default",

```    pattern: "{controller=Home}/{action=Index}/{id?}");



```htmlapp.Run();

<!-- View - Works for both full page loads and HTMX requests -->```

<div hx-ext="sse" sse-connect="/todos/stream">

    <div id="todo-list" sse-swap="todo-added" hx-swap="afterbegin">### 2. Create Controller

        @foreach (var todo in Model)

        {```csharp

            <div class="todo-item">public class ProductController : SwapController

                <input type="checkbox" hx-post="/todos/@todo.Id/toggle" />{

                <span>@todo.Title</span>    public async Task<IActionResult> Index()

            </div>    {

        }        var products = await _service.GetAllAsync();

    </div>        return SwapView(products); // Auto-detects page vs partial

</div>    }

````

    public async Task<IActionResult> Create(ProductDto dto)

## Installation {

        await _service.CreateAsync(dto);

````bash

dotnet add package Swap.Htmx        // Show success toast

```        Response.ShowSuccessToast("Product created!");



Then register in `Program.cs`:        return SwapView("Success");

    }

```csharp}

builder.Services.AddSwapHtmx();```



// ...### 3. Create View



app.UseSwapHtmxShell();```razor

app.UseSwapHtmx();@model List<Product>

````

<div id="product-list">

## Documentation <h1>Products</h1>

### Getting Started @foreach (var product in Model)

- **[Complete Setup Guide](./Docs/GETTING-STARTED.md)** - Step-by-step setup for new projects {

        <div class="product-card">

### Core Features <h3>@product.Name</h3>

- **[Toast Notifications](./Docs/TOASTS.md)** - User feedback without JavaScript <p>$@product.Price</p>

- **[Out-of-Band Swaps](./Docs/OOB-SWAPS.md)** - Update multiple elements at once

- **[Server-Sent Events](./Docs/SERVER-SENT-EVENTS.md)** - Real-time HTML streaming <button hx-post="/products/delete/@product.Id"

- **[Event System](./Docs/EVENTS.md)** - Domain events → UI updates hx-target="#product-list"

                    hx-confirm="Delete this product?">

### Reference Delete

- **[Templates](./Docs/TEMPLATES.md)** - Project templates and patterns </button>

- **[Detailed API Reference](./Docs/README.md)** - Complete framework documentation </div>

- **[E2E Testing Guide](./Swap.Htmx.E2ETests/README.md)** - Browser-based testing }

</div>

## Philosophy```

Swap.Htmx embraces simplicity:## Core Concepts

1. **Minimal abstraction** - Leverage HTMX's power, don't hide it### SwapView() - Automatic Rendering

2. **Convention over configuration** - Sensible defaults, minimal setup

3. **Type safety** - No magic strings for events or view names`SwapView()` automatically returns the correct response type:

4. **Progressive enhancement** - Works as a traditional MVC app, HTMX adds interactivity

````csharp

## Examplespublic async Task<IActionResult> Details(int id)

{

See the test app for complete working examples:    var product = await _service.GetAsync(id);



```bash    // Initial page load: Returns View() with layout

cd framework/Swap.Htmx.TestApp/src    // HTMX request: Returns PartialView() without layout

dotnet run    return SwapView("Details", product);

# Visit http://localhost:5000/test}

````

Examples include:**How it works:**

- Toast notifications- Checks for `HX-Request` header

- Out-of-band swaps- HTMX request → `PartialView()` (no layout)

- Server-Sent Events (real-time updates)- Normal request → `View()` (with layout)

- Form handling- Adds `Vary: HX-Request` header for caching

- Partial rendering

### Toast Notifications

## Requirements

Show user feedback with simple extension methods:

- .NET 9.0 or higher

- ASP.NET Core MVC```csharp

- HTMX 2.0+ (via CDN or npm)Response.ShowSuccessToast("Product saved!");

Response.ShowErrorToast("Something went wrong!");

## LicenseResponse.ShowWarningToast("Please review your changes.");

Response.ShowInfoToast("Processing in background...");

MIT - See [LICENSE](../../LICENSE) for details```

## Contributing**Features:**

- 4 toast types with different colors

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.- Auto-dismiss after 3 seconds

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

### Enhanced Server-Sent Events (NEW!)

Real-time communication with automatic fallback support:

```csharp
// 1. Register enhanced SSE services
builder.Services.AddSseEventBridge();
builder.Services.AddSseFallback(options => {
    options.DefaultPollInterval = 5000;
    options.MaxSseRetries = 3;
    options.EnableFallback = true;
});

app.UseSseEventBridge();

// 2. Enhanced SSE endpoint with connection management
public IActionResult LiveDashboard()
{
    return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) => {
        connectionBuilder.WithRooms("dashboard", "admin")
                        .WithEventPrefix("ui.dashboard");

        var connection = connectionBuilder.Connection;

        // Send initial data
        await connection.SendEventAsync("initial-data", html);

        // Keep connection alive
        await connectionBuilder.KeepAlive(TimeSpan.FromSeconds(30), cancellationToken);
    });
}

// 3. Automatic fallback polling endpoint
public async Task<IActionResult> DashboardData()
{
    if (_fallbackService.ShouldUsePolling(HttpContext))
    {
        return await this.CachedPollingFallback(
            cacheKey: "dashboard-data",
            getContentFunc: async () => await RenderDashboardAsync(),
            cacheDuration: TimeSpan.FromSeconds(10)
        );
    }

    // Regular SSE handling...
}
```

**Client-side with automatic fallback:**

```html
<div
  id="live-dashboard"
  hx-ext="sse-fallback"
  hx-sse="connect:/dashboard/live"
  hx-sse-swap="dashboard-update"
  hx-sse-fallback-url="/dashboard/data"
  hx-sse-fallback-interval="3000"
>
  <!-- Content updated in real-time -->
</div>

<script>
  // Monitor connection status
  document.addEventListener("sse:connected", (e) => {
    console.log("SSE connected:", e.detail.elementId);
  });

  document.addEventListener("sse:fallback", (e) => {
    console.log("Switched to polling fallback");
  });
</script>
```

**Cross-module broadcasting:**

```csharp
// Broadcast to specific rooms
await _connectionRegistry.BroadcastToRoomsAsync(
    eventName: "notification",
    html: notificationHtml,
    rooms: new[] { "users", "dashboard" }
);

// Broadcast to users with specific roles
await _connectionRegistry.BroadcastToRolesAsync(
    eventName: "admin-alert",
    html: alertHtml,
    roles: new[] { "Admin", "Manager" }
);
```

**Features:**

- 🔄 **Automatic Fallback** - Seamless HTTP polling when SSE fails
- 🏠 **Room Management** - Target broadcasts to specific user groups
- 🔐 **Authentication** - User-aware connections with role-based filtering
- 📡 **Event Bridge** - Automatic domain event → SSE broadcasting
- ⚡ **Performance** - Cached polling endpoints for optimal fallback
- 🔧 **Monitoring** - Connection status tracking and debugging tools

[📖 Full SSE Documentation](./SERVER-SENT-EVENTS.md)

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

### Getting Started

- [Complete Setup Guide](./GETTING-STARTED.md) - Step-by-step setup for new projects

### Features

- [Toast Notifications](./TOASTS.md) - Complete toast API and examples
- [Out-of-Band Swaps](./OOB-SWAPS.md) - Multiple element updates
- [Event System](./EVENTS.md) - Domain event → UI event chains

### Reference

- [Templates](./TEMPLATES.md) - Project templates and patterns
- [E2E Testing](../Swap.Htmx.E2ETests/README.md) - Browser-based testing guide

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

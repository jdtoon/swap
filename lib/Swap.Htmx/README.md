# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)

HTMX + ASP.NET Core MVC, but ergonomic.

`Swap.Htmx` is a lightweight orchestration layer for HTMX‑powered ASP.NET Core apps. It gives you:

- A `SwapController` base class and controller/PageModel extensions
- A fluent response builder for coordinated partial updates, toasts, and triggers
- **SwapState** – Strongly-typed state management with automatic model binding
- A type‑safe event system and event chains
- Built‑in real‑time support (WebSockets + Server‑Sent Events)
- Observability hooks (logging + OpenTelemetry)

If you want the *conceptual* overview of Swap as a whole, see the root `README.md`. This document focuses specifically on the `Swap.Htmx` package.

---

## When Should I Use Swap.Htmx?

Use `Swap.Htmx` when:

- You're building an ASP.NET Core MVC / Razor Pages / Minimal API app with HTMX
- You want to avoid scattered `ViewData`, magic IDs, and ad‑hoc headers
- You need coordinated updates (multiple partials, toasts, triggers) per action
- You want a central place to define “when X happens, update Y on the UI”
- You’d like real‑time HTML updates without going full SPA

If you just need a few custom HTMX headers, the low‑level helpers here work fine; as your app grows, the fluent builder and event chains keep things sane.

---

## Core Building Blocks

- **`SwapController`** – Base controller that:
  - Auto‑detects HTMX vs full page requests
  - Chooses full views or partials appropriately
  - Exposes helpers like `SwapView`, `SwapResponse`, `SwapEvent`, `ServerSentEvents`, etc.

- **Controller / PageModel Extensions** – Use Swap without inheriting:
  - `this.SwapView(...)` / `this.SwapResponse()` on `Controller`/`ControllerBase`
  - `this.SwapResponse()` on `PageModel`
  - `SwapResults` for Minimal APIs

- **`<swap-nav>` Tag Helper** (NEW)
  - `<swap-nav to="/path">` – Clean SPA navigation without verbose HTMX attributes
  - Auto-generates `hx-get`, `hx-target`, `hx-push-url`
  - Configurable default target via `SwapHtmxOptions.DefaultNavigationTarget`
  - See: `docs/SwapNavTagHelper.md`

- **Auto-Layout Suppression** (NEW)
  - `SwapHtmxOptions.AutoSuppressLayout = true` – HTMX requests get partials
  - `Context.ShouldSuppressLayout()` – Extension for `_ViewStart.cshtml`
  - Eliminates per-module `_ViewStart.cshtml` files

- **Fluent Response Builder** (`SwapResponseBuilder`)
  - `WithView(viewName, model)` – main response payload
  - `AlsoUpdate(targetId, viewName, model, swapMode)` – out‑of‑band swaps
  - `WithNavigation(path, target?, swap?)` – SPA-style navigation via `HX-Location`
  - `WithSuccessToast(...)`, `WithErrorToast(...)`, `WithInfoToast(...)`, `WithWarningToast(...)`
  - `WithTrigger(eventKeyOrName, payload?)` – strongly‑typed or string events
  - All triggers and toasts are merged into a single `HX-Trigger` header.

- **Event System & Event Chains**
  - Type‑safe `EventKey` and `SwapEvents` helpers
  - `ISwapEventConfiguration` for central “when X happens, update Y” definitions
  - Declarative chains that render partials, show toasts, and trigger additional events

- **Realtime**
  - Server‑Sent Events helpers
  - WebSocket integration and connection registry
  - Optional Redis backplane (`ISseBackplane`) for multi‑server setups

- **Dev & Diagnostics**
  - Dev endpoints (`app.MapSwapHtmxDevEndpoints()`) for inspecting chains and connections
  - Structured logs and OpenTelemetry hooks

---

## Installation

### Option 1 – Templates (Recommended)

Install the templates and scaffold a ready‑to‑go project:

```bash
dotnet new install Swap.Templates
dotnet new swap-mvc -n MyProject
```

This wires up `Swap.Htmx`, HTMX, dev tooling, and sample views for you. See `templates/README.md` and the demo apps under `demo/` for patterns.

### Option 2 – Manual Installation

Add the package to an existing ASP.NET Core app:

```bash
dotnet add package Swap.Htmx
```

Then follow the setup below.

---

## Basic Setup

### 1. Register Services

In `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Registers core Swap services, event bus, middleware, etc.
builder.Services.AddSwapHtmx();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Enables Swap middleware (HTMX helpers, event handling, dev tooling hooks)
app.UseSwapHtmx();

app.MapControllers();

app.Run();
```

See: `docs/GettingStarted.md` for a step‑by‑step walkthrough.

### 2. Add Client Assets

In your main layout (e.g. `Views/Shared/_Layout.cshtml`), add Swap’s CSS and JS along with HTMX:

```html
<head>
    <!-- ... existing head content ... -->

    <!-- Swap.Htmx toasts/styles -->
    <link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />

    <!-- HTMX (from CDN or LibMan) -->
    <script src="https://unpkg.com/htmx.org@2.0.8"></script>

    <!-- Swap.Htmx client script (toasts + client events) -->
    <script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
</head>
```

> For LibMan‑managed HTMX and assets, see the templates and `docs/GettingStarted.md`.

---

## Using Swap in Controllers

### 1. Simple View (80% of cases)

Use `SwapController` or the `SwapView` extension to automatically handle full vs partial renders:

```csharp
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

public class ProductsController : SwapController
{
    public IActionResult Details(int id)
    {
        var product = _db.Products.Find(id);
        return SwapView("Details", product);
        // Normal request  -> full view
        // HTMX request    -> partial view
    }
}
```

On a regular controller:

```csharp
public class ProductsController : Controller
{
    public IActionResult Details(int id)
    {
        var product = _db.Products.Find(id);
        return this.SwapView("Details", product);
    }
}
```

See: `docs/GettingStarted.md` and `docs/RazorPages.md` for more patterns.

### 2. Coordinated Updates (Fluent Response Builder)

When an action needs to update multiple parts of the page, send a main view and out‑of‑band swaps in one chain:

```csharp
public class CartController : SwapController
{
    public IActionResult AddToCart(int productId)
    {
        _cart.Add(productId);

        return SwapResponse()
            .WithView(CartViews.ProductAdded)
            .AlsoUpdate(CartElements.Count, CartViews.Count, _cart.Count, SwapMode.InnerHTML)
            .AlsoUpdate(CartElements.Total, CartViews.Total, _cart.Total)
            .WithSuccessToast("Added to cart!")
            .WithTrigger(CartEvents.Updated, new { itemCount = _cart.Count });
    }
}
```

Key APIs:

- `WithView(viewName, model)` – main HTML payload
- `AlsoUpdate(targetId, viewName, model, swapMode?)` – out‑of‑band swaps
- `WithNavigation(path, target?, swap?)` – SPA-style navigation
- `WithSuccessToast(message)` / `WithErrorToast` / `WithWarningToast` / `WithInfoToast`
- `WithTrigger(eventKeyOrName, payload?)` – add HTMX triggers (merged into `HX-Trigger`)

Swap modes are strongly typed via `SwapMode` (OuterHTML, InnerHTML, BeforeBegin, AfterBegin, BeforeEnd, AfterEnd, Delete).

See: `docs/OutOfBandSwaps.md`.

### 3. Navigation with Toasts

Use `.WithNavigation()` to redirect users while preserving toasts and triggers:

```csharp
public class OrderController : SwapController
{
    [HttpPost]
    public IActionResult Create(OrderForm form)
    {
        var order = _service.CreateOrder(form);
        
        // Navigate to order detail AND show success toast
        return SwapResponse()
            .WithNavigation($"/orders/{order.Id}")
            .WithCreatedToast("Order", order.OrderNumber)
            .Build();
    }
}
```

This uses `HX-Location` instead of a redirect, so the toast is displayed during the navigation.

See: `docs/Navigation.md`.

### 4. Minimal APIs

`SwapResults` lets Minimal API endpoints return Swap responses:

```csharp
using Swap.Htmx;

app.MapPost("/todo", (TodoItem item, ITodoService service) =>
{
    service.Add(item);

    return SwapResults.Response()
        .WithView("_TodoItem", item)
        .WithSuccessToast("Added!");
});
```

See: `docs/MinimalApis.md`.

### 5. Razor Pages

Use Swap directly from `PageModel` via extension methods:

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Swap.Htmx;

public class IndexModel : PageModel
{
    public IActionResult OnPostUpdate()
    {
        return this.SwapResponse()
            .WithView("_Partial", this)
            .Build();
    }
}
```

See: `docs/RazorPages.md`.

### 6. Composition Over Inheritance

All fluent APIs are available as extension methods on `ControllerBase` and `PageModel`, so you can opt‑out of inheriting from `SwapController` entirely:

```csharp
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

public class ReviewsController : Controller
{
    [HttpPost]
    public IActionResult Add(Review review)
    {
        if (!ModelState.IsValid)
        {
            return this.SwapValidationErrors(ModelState)
                .AlsoUpdate("review-form", "_ReviewForm", review)
                .Build();
        }

        _service.Add(review);

        return this.SwapResponse()
            .WithSuccessToast("Review added!")
            .WithTrigger(ReviewEvents.Added, review)
            .Build();
    }
}
``;

See: `Extensions/SwapControllerExtensions.cs`, `Extensions/SwapPageModelExtensions.cs`, `Extensions/SwapValidationExtensions.cs`.

---

## State Management with SwapState

SwapState provides strongly-typed state containers with automatic model binding:

```csharp
using Swap.Htmx.State;

// 1. Define your state
public class InventoryState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
}

// 2. Bind automatically in actions
public IActionResult Grid([FromSwapState] InventoryState state)
{
    var items = _service.GetItems(state);
    
    return this.SwapResponse()
        .WithView("_Grid", items)
        .WithState(state)  // Auto-updates state via OOB swap
        .Build();
}
```

```html
<!-- 3. Render in views -->
<swap-state state="Model.State" />

<button hx-get="/Inventory/Grid"
        hx-target="#results"
        hx-include="#inventory-state">
    Load
</button>
```

**Benefits:**
- **Strongly-typed** – No magic strings for hidden fields
- **Automatic binding** – `[FromSwapState]` handles model binding
- **Auto-sync** – `.WithState()` updates hidden fields via OOB swap
- **Change tracking** – `state.HasChanges`, `state.ChangedProperties`

See: `docs/SwapState.md` for full documentation.

---

## Form Validation

Use the `<swap-validation>` tag helper and `SwapValidationErrors()` for seamless server-side validation:

```html
<!-- In your form -->
<div class="form-group">
    <label asp-for="Name"></label>
    <input asp-for="Name" />
    <swap-validation for="Name" />
</div>

<div class="form-group">
    <label asp-for="Email"></label>
    <input asp-for="Email" />
    <swap-validation for="Email" />
</div>
```

```csharp
[HttpPost]
public IActionResult Create(CreateDto dto)
{
    if (!ModelState.IsValid)
    {
        return this.SwapValidationErrors(ModelState)
            .WithView("_CreateForm", dto)
            .Build();
    }
    
    var item = _service.Create(dto);
    return this.SwapRedirect("/Items", "Item created!");
}
```

The validation errors are automatically displayed in the corresponding `<swap-validation>` elements.

See: `docs/Validation.md` for full documentation.

---

## CRUD Toast Presets

Standard success messages for common operations:

```csharp
// After creating
.WithCreatedToast("Product", product.Name)  // "Product 'Widget' created successfully"

// After updating
.WithUpdatedToast("Settings")               // "Settings updated"

// After deleting
.WithDeletedToast("User", "john@test.com")  // "User 'john@test.com' deleted"

// Generic CRUD toast
.WithCrudToast(CrudOperation.Archived, "Record")  // "Record archived"
```

See: `docs/CrudToasts.md` for all operations.

---

## State Coordination with hx-include

Use standard HTMX `hx-include` to send state with requests:

```html
<!-- Include state container by ID -->
<button hx-get="/Items/Search"
        hx-include="#filter-state">
```

Multiple state containers:
```html
<div hx-get="/Report" hx-include="#filter-state, #sort-state">
```

---

## Event System & Event Chains

As your UI grows, you can centralize "when event X happens, refresh Y and show Z toast" declarations.

### Configuration

Create a config class implementing `ISwapEventConfiguration` and register it via `AddSwapHtmx`:

```csharp
using Swap.Htmx;
using Swap.Htmx.Events;

public static class ProductViews
{
    public const string List = "_ProductList";
    public const string Count = "_ProductCount";
}

public static class ProductElements
{
    public const string List = "product-list";
    public const string Count = "product-count";
}

public class ProductEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        events.When(SwapEvents.Entity.Created("Product"))
              .RefreshPartial(ProductElements.List, ProductViews.List, ctx => GetProducts(ctx))
              .RefreshPartial(ProductElements.Count, ProductViews.Count, ctx => GetProductCount(ctx))
              .SuccessToast("Product created!");
    }
}

// Program.cs
builder.Services.AddSwapHtmx(options =>
{
    options.AddConfig<ProductEventConfig>();
});
```

From a controller, just emit the event:

```csharp
public class ProductsController : SwapController
{
    public IActionResult Create(Product product)
    {
        _db.Products.Add(product);
        _db.SaveChanges();

        return SwapEvent(SwapEvents.Entity.Created("Product"));
    }
}
```

Async model factories avoid thread starvation for DB work:

```csharp
events.When(ProductEvents.StockChecked)
      .RefreshPartialAsync(ProductElements.Stock, ProductViews.Stock, async ctx =>
      {
          var service = ctx.RequestServices.GetRequiredService<IProductService>();
          return await service.GetStockAsync();
      });
```

See: `docs/Events.md` and `docs/EventChains.md`.

---

## Realtime: SSE & WebSockets

`Swap.Htmx` includes primitives for streaming HTML over SSE and WebSockets.

### Server‑Sent Events (SSE)

Basic SSE endpoint from a `SwapController`:

```csharp
public const string NotificationView = "_Notification";

[HttpGet("/sse/notifications")]
public IActionResult StreamNotifications()
{
    return ServerSentEvents(async (connection, ct) =>
    {
        while (!ct.IsCancellationRequested)
        {
            var html = await RenderPartialToStringAsync(NotificationView, GetLatestNotification());
            await connection.SendEventAsync("notification", html);
            await Task.Delay(5000, ct);
        }
    });
}
```

Enhanced SSE with rooms and subscriptions:

```csharp
public static class SseRooms
{
    public const string Dashboard = "dashboard";
}

public static class SseEventNames
{
    public const string MetricsUpdated = "metrics-updated";
}

[HttpGet("/sse/dashboard")]
public IActionResult Dashboard()
{
    return EnhancedServerSentEvents(async (builder, ct) =>
    {
        var connection = builder.Connection;

        connection.JoinRoom(SseRooms.Dashboard);
        connection.SubscribeToEvent(SseEventNames.MetricsUpdated);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(30000, ct);
        }
    });
}
```

To broadcast from services, inject `ISseEventBridge`:

```csharp
public class OrderService
{
    private readonly ISseEventBridge _sse;

    public OrderService(ISseEventBridge sse) => _sse = sse;

    public async Task CompleteOrder(int orderId)
    {
        await _sse.BroadcastAsync("OrderCompleted", new { id = orderId });
        await _sse.SendToUserAsync("user-123", "Notification", "Your order is ready!");
        await _sse.SendToRoomAsync("admin-dashboard", "StatsUpdated", new { id = orderId });
    }
}
```

Configure in `Program.cs`:

```csharp
// Single‑server, in‑memory
builder.Services.AddSwapHtmx()
                .AddSseEventBridge();

// With Redis backplane
builder.Services.AddSwapHtmx()
                .AddSseEventBridge()
                .AddSwapRedisBackplane(options =>
                {
                    options.Configuration = "localhost:6379";
                    options.ChannelName = "my-app-events";
                });
```

See: `docs/ServerSentEvents.md`, `docs/WebSockets.md`, `docs/Realtime.md`, and `docs/RedisBackplane.md`.

---

## Low‑Level HTMX Helpers

If you just need to set headers manually, use the `HttpRequest`/`HttpResponse` extensions in `Swap.Htmx.Extensions`:

```csharp
if (Request.IsHtmxRequest())
{
    Response.HxTrigger("todoCreated");
    Response.HxTrigger("todoCreated", new { id = 123 });
    Response.HxRedirect("/todos");
    Response.HxRefresh();
}
```

There are also helpers for `HX-Location`, `HX-Reswap`, and more. See: `Extensions/SwapHtmxExtensions.cs`.

---

## Dev Tooling & Observability

- **Dev endpoints** – expose Swap dev UIs:

  ```csharp
  app.MapSwapHtmxDevEndpoints(); // /_swap/dev/*
  ```

  Useful for inspecting event chains, SSE/WebSocket connections, and troubleshooting.

- **Logging & Telemetry** – `SwapTelemetry` and `SwapLog` integrate with ASP.NET logging and OpenTelemetry.

  - To see verbose logs, add e.g. to `appsettings.Development.json`:

    ```json
    "Logging": {
      "LogLevel": {
        "Swap.Htmx": "Debug"
      }
    }
    ```

See: `docs/DebuggingAndLogging.md`.

---

## Demos & Templates

This repo ships with several demos that exercise different parts of `Swap.Htmx`:

- `demo/SwapMinimal` – minimal API + Swap example
- `demo/SwapShop` – e‑commerce style MVC app showing controllers, events, and chains
- `demo/SwapNavDemo` – navigation patterns with `.WithNavigation()` ⭐ *NEW*
- `demo/SwapStateDemo` – server-driven state management with `<swap-state>` ⭐ *NEW*
- `demo/TaskFlow` – team task management with realtime features
- `demo/SwapWebSockets`, `demo/SwapRedisDemo`, `demo/SwapPhase15`, etc. – focused samples for realtime and orchestration features

For a production‑ready starting point, use the `swap-mvc` template from `Swap.Templates`.

---

## Documentation Map

All library docs live under `docs/` in this folder:

### Core Concepts
- **Getting Started** – `docs/GettingStarted.md`
- **Navigation** – `docs/Navigation.md` ⭐ *NEW*
- **Migration Guide** – `docs/MigrationGuide.md`
- **Multi-Component Coordination** – `docs/MultiComponentCoordination.md`
- **State Management** – `docs/StateManagement.md` ⭐ *NEW*
- **Anti-Patterns** – `docs/AntiPatterns.md` ⭐ *NEW*

### Events & Updates
- **Events & Triggers** – `docs/Events.md`
- **Event Chains** – `docs/EventChains.md`
- **Out‑of‑Band Swaps** – `docs/OutOfBandSwaps.md`

### Realtime
- **Realtime Overview** – `docs/Realtime.md`
- **Server‑Sent Events** – `docs/ServerSentEvents.md`
- **WebSockets** – `docs/WebSockets.md`
- **Redis Backplane** – `docs/RedisBackplane.md`

### Framework Integration
- **Minimal APIs** – `docs/MinimalApis.md`
- **Razor Pages** – `docs/RazorPages.md`
- **Source Generators** – `docs/SourceGenerators.md`
- **User Context & Identity** – `docs/UserContext.md`

### Development
- **Debugging & Logging** – `docs/DebuggingAndLogging.md`

For a higher‑level view of all Swap packages (`Swap.Htmx`, `Swap.Testing`, templates, etc.), see the root‑level `README.md`.

---

## License

MIT
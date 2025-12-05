# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)

**A UI orchestration library for HTMX and ASP.NET Core.**

Swap.Htmx coordinates what updates where when events fire. Type-safe events, decoupled handlers, multi-target partial updates, server-side state — all working together so you define the rules once and the UI updates itself.

---

## What It Does

**Trigger an event. UI updates itself.**

```csharp
// Controller just emits an event
return this.SwapEvent(new TaskCompletedEvent { TaskId = id }).Build();
```

```csharp
// Handler 1: Removes the task row
[SwapHandler]
public class TaskRowHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate(TaskElements.Row(e.TaskId), Views.Empty, null, SwapMode.Delete);
        return Task.CompletedTask;
    }
}

// Handler 2: Updates the progress bar (completely decoupled)
[SwapHandler]
public class ProgressHandler : ISwapEventHandler<TaskCompletedEvent>
{
    private readonly ITaskService _tasks;
    public ProgressHandler(ITaskService tasks) => _tasks = tasks;
    
    public async Task HandleAsync(TaskCompletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        var progress = await _tasks.GetProgressAsync();
        builder.AlsoUpdate(TaskElements.ProgressBar, TaskViews.Partials.Progress, progress);
    }
}
```

The controller doesn't know which parts of the UI need updating. The handlers don't know about each other. **That's the point.**

> Element IDs and view paths above use [source-generated constants](#source-generators). Strings work too, but static types catch typos at compile time.

---

## Core Capabilities

### Type-Safe Events

No magic strings. Define events once, use everywhere.

**Option 1: Source Generated (Recommended)**

```csharp
[SwapEventSource]
public partial class CartEvents
{
    public const string ItemAdded = "cart.item.added";
    public const string Cleared = "cart.cleared";
}

// Generated: CartEvents.Cart.Item.Added, CartEvents.Cart.Cleared
```

**Option 2: Manual EventKey**

```csharp
public static class CartEvents
{
    public static readonly EventKey ItemAdded = new("cart.itemAdded");
    public static readonly EventKey Cleared = new("cart.cleared");
}
```

**Usage:**

```csharp
return SwapResponse()
    .WithTrigger(CartEvents.Cart.Item.Added, new { productId, count })
    .Build();
```

→ [Events Documentation](lib/Swap.Htmx/docs/Events.md)

---

### Event Chains

Two approaches to coordinate UI updates when events fire.

**Option 1: Distributed Handlers**

Each handler updates one piece of the UI. Add new handlers without touching controllers.

```csharp
[SwapHandler]
public class CartBadgeHandler : ISwapEventHandler<CartItemAddedEvent>
{
    public Task HandleAsync(CartItemAddedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate(CartElements.Badge, CartViews.Partials.Badge, e.NewCount);
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class CartTotalHandler : ISwapEventHandler<CartItemAddedEvent>
{
    public Task HandleAsync(CartItemAddedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate(CartElements.Total, CartViews.Partials.Total, e.NewTotal);
        return Task.CompletedTask;
    }
}
```

**Option 2: Centralized Configuration**

Define all event reactions in one place with `ISwapEventConfiguration`.

```csharp
public class CartEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        events.When(CartEvents.Cart.Item.Added)
            .RefreshPartial(CartElements.Badge, CartViews.Partials.Badge, ctx => GetCount(ctx))
            .RefreshPartial(CartElements.Total, CartViews.Partials.Total, ctx => GetTotal(ctx))
            .SuccessToast("Added to cart!");
    }
}
```

```csharp
// Register in Program.cs
builder.Services.AddSwapHtmx(options => options.AddConfig<CartEventConfig>());
```

Both approaches work. Distributed handlers scale better for complex apps. Centralized config is easier to read for simpler cases.

→ [Event Chains Documentation](lib/Swap.Htmx/docs/EventChains.md)

---

### Fluent Response Builder

Coordinate multiple partial updates, toasts, and triggers in a single response.

```csharp
return SwapResponse()
    .WithView("_ProductDetails", product)
    .AlsoUpdate("cart-count", "_CartCount", count)
    .AlsoUpdate("sidebar-total", "_Total", total)
    .WithSuccessToast("Added to cart!")
    .WithTrigger(CartEvents.Updated)
    .Build();
```

All out-of-band swaps, toasts, and triggers merge into a single `HX-Trigger` header.

**Navigate with toasts:**

```csharp
return SwapResponse()
    .WithNavigation($"/orders/{order.Id}")
    .WithCreatedToast("Order", order.OrderNumber)
    .Build();
```

Uses `HX-Location` for SPA-style navigation while preserving toast display.

→ [Navigation](lib/Swap.Htmx/docs/Navigation.md)  
→ [Out-of-Band Swaps](lib/Swap.Htmx/docs/OutOfBandSwaps.md)

---

### SwapState

Strongly-typed server-side state with automatic model binding and sync.

```csharp
public class WizardState : SwapState
{
    public int Step { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
```

```csharp
public IActionResult NextStep([FromSwapState] WizardState state)
{
    state.Step++;
    
    return this.SwapResponse()
        .WithView($"_Step{state.Step}", state)
        .WithState(state)  // Auto-syncs to client
        .Build();
}
```

```html
<swap-state state="Model.State" />
```

State is encrypted, tamper-proof, and automatically round-trips.

→ [SwapState Documentation](lib/Swap.Htmx/docs/SwapState.md)

---

### Real-Time (SSE & WebSockets)

Push events to connected clients.

```csharp
await _eventService.BroadcastAsync("notification", new { message = "New order received" });
```

```html
<div swap-sse="notification" swap-target="#alerts" swap-partial="_Alert"></div>
```

Multi-server? Use the Redis backplane.

→ [Server-Sent Events](lib/Swap.Htmx/docs/ServerSentEvents.md)  
→ [WebSockets](lib/Swap.Htmx/docs/WebSockets.md)  
→ [Redis Backplane](lib/Swap.Htmx/docs/RedisBackplane.md)

---

### Form Validation

Server-side validation that displays inline.

```html
<input asp-for="Email" />
<swap-validation for="Email" />
```

```csharp
if (!ModelState.IsValid)
{
    return this.SwapValidationErrors(ModelState)
        .WithView("_Form", model)
        .Build();
}
```

→ [Validation Documentation](lib/Swap.Htmx/docs/Validation.md)

---

### `<swap-nav>` Tag Helper

**SPA-style navigation without the boilerplate.**

```html
<!-- ❌ Before: Repetitive, error-prone -->
<a href="/products" hx-get="/products" hx-target="#main-content" hx-push-url="true">Products</a>
<a href="/orders" hx-get="/orders" hx-target="#main-content" hx-push-url="true">Orders</a>

<!-- ✅ After: Clean, consistent -->
<swap-nav to="/products">Products</swap-nav>
<swap-nav to="/orders">Orders</swap-nav>
```

The tag helper automatically adds `hx-get`, `hx-target`, and `hx-push-url`. Configure the target once:

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.DefaultNavigationTarget = "#main-content";
    options.AutoSuppressLayout = true;  // HTMX requests get partials
});
```

With `AutoSuppressLayout`, your `_ViewStart.cshtml` becomes:

```razor
@{ Layout = Context.ShouldSuppressLayout() ? null : "_Layout"; }
```

- **Browser navigation** → Full page with layout
- **`<swap-nav>` clicks** → Partial content only

→ [`<swap-nav>` Documentation](lib/Swap.Htmx/docs/SwapNavTagHelper.md)

---

### Source Generators

Compile-time safety for events, view paths, and element IDs. Typos become compiler errors.

**Auto-Scan (Recommended)** — Zero configuration, just add files:

```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

The generator automatically creates:
- `SwapViews.Home.Index`, `SwapViews.Products.Details` — Constants for every view
- `SwapElements.Home.Index.UserPanel` — Constants for every `id` attribute

**Manual Attributes** — For custom naming or selective generation:

```csharp
// Events — generates nested hierarchy from dot notation
[SwapEventSource]
public partial class AppEvents
{
    public const string CartItemAdded = "cart.item.added";  // → AppEvents.Cart.Item.Added
}

// Views — scans folder, generates constants for .cshtml files
[SwapViewSource("Views/Cart")]
public static partial class CartViews { }  // → CartViews.Index, CartViews.Partials.Badge

// Elements — scans .cshtml files for id="..." attributes
[SwapElementSource("Views/Cart")]
public static partial class CartElements { }  // → CartElements.Badge, CartElements.Total
```

→ [Auto-Generated Constants](lib/Swap.Htmx/docs/AutoScanGenerator.md)  
→ [Source Generators (Attributes)](lib/Swap.Htmx/docs/SourceGenerators.md)

---

## Installation

### Quick Start (Recommended)

The **Modular Monolith template** is the best way to start a new Swap.Htmx project. It provides a well-structured foundation with modular architecture, database setup, and production-ready infrastructure.

```bash
dotnet new install Swap.Templates
dotnet new swap-modular -n MyApp
cd MyApp/src
libman restore
dotnet run
```

This gives you:
- **Modular architecture** — Self-contained feature modules
- **SQLite + EF Core** — Database with audit fields
- **Docker ready** — Dockerfile and docker-compose included
- **Sample module** — Full CRUD example showing the patterns
- **Test project** — Integration tests with WebApplicationFactory

→ [Full template documentation](templates/README.md)

### Minimal Setup

For a lightweight starting point or adding to existing projects:

```bash
dotnet new swap-mvc -n MyProject
```

### Manual

```bash
dotnet add package Swap.Htmx
```

```csharp
// Program.cs
builder.Services.AddSwapHtmx();
app.UseSwapHtmx();
```

```html
<!-- Layout -->
<link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />
<script src="https://unpkg.com/htmx.org@2.0.8"></script>
<script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
```

→ [Getting Started](lib/Swap.Htmx/docs/GettingStarted.md)

---

## Works With

- **Controllers** — Inherit `SwapController` or use extension methods
- **Razor Pages** — Extension methods on `PageModel`
- **Minimal APIs** — `SwapResults.Response()`

→ [Razor Pages](lib/Swap.Htmx/docs/RazorPages.md)  
→ [Minimal APIs](lib/Swap.Htmx/docs/MinimalApis.md)

---

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](lib/Swap.Htmx/docs/GettingStarted.md) | Setup and first steps |
| [`<swap-nav>` Tag Helper](lib/Swap.Htmx/docs/SwapNavTagHelper.md) | **SPA navigation without boilerplate** |
| [Navigation](lib/Swap.Htmx/docs/Navigation.md) | Programmatic navigation with toasts |
| [Events](lib/Swap.Htmx/docs/Events.md) | Type-safe event system |
| [Event Chains](lib/Swap.Htmx/docs/EventChains.md) | Distributed handlers and decoupled updates |
| [SwapState](lib/Swap.Htmx/docs/SwapState.md) | Server-side state management |
| [Out-of-Band Swaps](lib/Swap.Htmx/docs/OutOfBandSwaps.md) | Multi-target updates |
| [Validation](lib/Swap.Htmx/docs/Validation.md) | Form validation |
| [Server-Sent Events](lib/Swap.Htmx/docs/ServerSentEvents.md) | Real-time push |
| [WebSockets](lib/Swap.Htmx/docs/WebSockets.md) | Full-duplex real-time |
| [Redis Backplane](lib/Swap.Htmx/docs/RedisBackplane.md) | Multi-server real-time |
| [Auto-Generated Constants](lib/Swap.Htmx/docs/AutoScanGenerator.md) | **Zero-config view/element constants** |
| [Source Generators](lib/Swap.Htmx/docs/SourceGenerators.md) | Attribute-based generation |
| [Recipes](lib/Swap.Htmx/docs/Recipes.md) | Common patterns |
| [Anti-Patterns](lib/Swap.Htmx/docs/AntiPatterns.md) | What to avoid |
| [Debugging](lib/Swap.Htmx/docs/DebuggingAndLogging.md) | Diagnostics and logging |

---

## Demo Applications

Working examples in `/demo`:

| Demo | What It Shows |
|------|---------------|
| [SwapMinimal](demo/SwapMinimal) | Basic setup and patterns |
| [SwapNavDemo](demo/SwapNavDemo) | **`<swap-nav>` tag helper and auto-layout suppression** |
| [SwapPages](demo/SwapPages) | Razor Pages integration |
| [SwapLab](demo/SwapLab) | Feature showcase |
| [SwapShop](demo/SwapShop) | E-commerce (cart, checkout) |
| [SwapChat](demo/SwapChat) | Real-time chat with SSE |
| [SwapExpenses](demo/SwapExpenses) | Full CRUD application |
| [SwapDashboard](demo/SwapDashboard) | Complex UI orchestration (20+ partials, distributed handlers) |
| [TaskFlow](demo/TaskFlow) | Kanban board |

---

## Requirements

- .NET 9.0+
- ASP.NET Core (MVC, Razor Pages, or Minimal APIs)
- HTMX 2.x

---

## License

MIT — see [LICENSE](LICENSE)

# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**HTMX + ASP.NET Core, made simple.**

Build interactive web apps with server-rendered HTML. No JavaScript frameworks, no complex state management, no build tools.

---

## Quick Start (5 minutes)

### 1. Install

```bash
dotnet add package Swap.Htmx
```

### 2. Setup (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();  // ← Add this

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();              // ← Add this
app.MapControllers();
app.Run();
```

### 3. Layout (_Layout.cshtml)

```html
<head>
    <link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />
    <script src="https://unpkg.com/htmx.org@2.0.4"></script>
    <script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
</head>
<body>
    @RenderBody()
</body>
```

### 4. Your First Controller

```csharp
public class ProductsController : SwapController
{
    public IActionResult Index()
    {
        var products = GetProducts();
        return SwapView(products);  // Auto-detects HTMX vs full page
    }

    [HttpPost]
    public IActionResult Add(Product product)
    {
        SaveProduct(product);
        
        return SwapResponse()
            .WithView("_ProductRow", product)           // Main response
            .AlsoUpdate("product-count", "_Count", GetCount())  // OOB update
            .WithSuccessToast("Product added!")         // Toast notification
            .Build();
    }
}
```

**That's it!** You're ready to build interactive UIs.

---

## Core Concepts

### 1. SwapController & SwapView

`SwapController` auto-detects HTMX requests:

```csharp
public class HomeController : SwapController
{
    public IActionResult Index()
    {
        // Normal request → View with layout
        // HTMX request  → PartialView (no layout)
        return SwapView(model);
    }
}
```

**Don't want to inherit?** Use extensions:

```csharp
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SwapView(model);  // Extension method
    }
}
```

---

### 2. SwapResponse Builder

For complex responses with multiple updates:

```csharp
return SwapResponse()
    .WithView("_MainContent", model)              // Primary response
    .AlsoUpdate("sidebar", "_Sidebar", sidebar)   // OOB swap
    .AlsoUpdate("header", "_Header", header)      // Another OOB
    .WithSuccessToast("Done!")                    // Toast
    .WithTrigger("dataUpdated")                   // Client event
    .Build();
```

---

### 3. Swap Navigation

SPA-style navigation without JavaScript:

```html
<!-- Instead of this verbose HTMX: -->
<a href="/products" hx-get="/products" hx-target="#main" hx-push-url="true">Products</a>

<!-- Use this: -->
<swap-nav to="/products">Products</swap-nav>
```

Configure the default target:

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.DefaultNavigationTarget = "#main-content";
});
```

---

### 4. SwapState (Server-Driven State)

Manage UI state with hidden fields—no JavaScript state management:

**Define state:**
```csharp
public class FilterState : SwapState
{
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
}
```

**Render in view:**
```html
<swap-state state="Model.State" />

<input type="text" 
       name="Search"
       value="@Model.State.Search"
       hx-get="/Products/Filter?Page=1"
       hx-target="#results"
       hx-include="#filter-state"
       hx-trigger="keyup changed delay:300ms" />
```

**Bind in controller:**
```csharp
[HttpGet]
public IActionResult Filter([FromSwapState] FilterState state)
{
    var products = GetProducts(state.Category, state.Search, state.Page);
    return PartialView("_ProductList", new ViewModel { State = state, Products = products });
}
```

**Key Pattern:** URL parameters override hidden fields (first value wins).

📖 [Full SwapState Guide](docs/SwapState.md)

---

### 5. Event System

Three approaches, from simple to powerful:

#### A. Direct Builder (Simple)

```csharp
// You know exactly what to update
return SwapResponse()
    .WithView("_Item", item)
    .AlsoUpdate("count", "_Count", count)
    .Build();
```

#### B. Event Configuration (Medium)

```csharp
// Configure once in Program.cs
builder.Services.AddSwapHtmx(options =>
{
    options.ConfigureEvents(events =>
    {
        events.On(CartEvents.ItemAdded)
            .AlsoUpdate("cart-count", "_CartCount")
            .AlsoUpdate("cart-total", "_CartTotal");
    });
});

// Controller just fires event
return SwapEvent(CartEvents.ItemAdded, item).WithView("_Added", item).Build();
```

#### C. Event Handlers (Powerful) ⭐

```csharp
// Define events
public static class TaskEvents
{
    public static readonly EventKey Completed = new("task:completed");
}

// Handler updates stats (DI supported!)
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class StatsHandler : ISwapEventHandler<TaskPayload>
{
    private readonly IStatsService _stats;
    public StatsHandler(IStatsService stats) => _stats = stats;
    
    public void Handle(SwapEventContext<TaskPayload> context)
    {
        var stats = _stats.Calculate();
        context.Response.AlsoUpdate("stats-panel", "_Stats", stats);
    }
}

// Handler updates activity feed
[SwapHandler(typeof(TaskEvents), nameof(TaskEvents.Completed))]
public class ActivityHandler : ISwapEventHandler<TaskPayload>
{
    public void Handle(SwapEventContext<TaskPayload> context)
    {
        context.Response.AlsoUpdate("activity", "_Activity", GetRecent());
    }
}

// Controller stays thin
public IActionResult Complete(int id)
{
    var task = _service.Complete(id);
    return SwapEvent(TaskEvents.Completed, new TaskPayload(task))
        .WithView("_TaskCompleted", task)
        .Build();
}
// One event → multiple handlers → one response with all updates
```

📖 [Full Events Guide](docs/Events.md)

---

### 6. When to Use OOB Swaps

**✅ Use OOB for related updates:**
```csharp
// Add to cart → update count AND total
return SwapResponse()
    .WithView("_ProductAdded", product)
    .AlsoUpdate("cart-count", "_Count", count)
    .AlsoUpdate("cart-total", "_Total", total)
    .Build();
```

**❌ Don't stuff unrelated updates:**
```csharp
// BAD: Kitchen sink response
return SwapResponse()
    .WithView("_Item", item)
    .AlsoUpdate("header", "_Header", header)
    .AlsoUpdate("sidebar", "_Sidebar", sidebar)
    .AlsoUpdate("footer", "_Footer", footer)
    .AlsoUpdate("notifications", "_Notifications", notifications)
    // ... 10 more unrelated things
    .Build();
```

**Instead:** Use event handlers or let components refresh themselves:
```html
<div hx-get="/notifications" hx-trigger="load, every 30s"></div>
```

---

## Feature Reference

| Feature | Usage |
|---------|-------|
| Auto HTMX detection | `SwapView()` / `this.SwapView()` |
| Multiple updates | `SwapResponse().AlsoUpdate()` |
| SPA navigation | `<swap-nav to="/path">` |
| State management | `<swap-state>` + `[FromSwapState]` |
| Toast notifications | `.WithSuccessToast()`, `.WithErrorToast()` |
| Client events | `.WithTrigger("eventName")` |
| Event handlers | `ISwapEventHandler<T>` |
| Form validation | `<swap-validation>` + `SwapValidationErrors()` |
| Real-time (SSE) | `ServerSentEvents()` |
| Real-time (WebSocket) | WebSocket registry |
| Source generators | `[SwapEventSource]`, auto `SwapViews`/`SwapElements` |

---

## Source Generators

Eliminate magic strings with compile-time code generation:

### 1. Type-Safe Event Keys

```csharp
// Define your events
[SwapEventSource]
public static partial class CartEvents
{
    public const string ItemAdded = "cart.itemAdded";
    public const string CheckoutCompleted = "cart.checkoutCompleted";
}

// Generated at build time:
// CartEvents.Cart.ItemAdded          → EventKey("cart.itemAdded")
// CartEvents.Cart.CheckoutCompleted  → EventKey("cart.checkoutCompleted")

// Use in controller
return SwapEvent(CartEvents.Cart.ItemAdded, item).Build();
```

### 2. Auto-Generated View & Element Constants (Zero Config)

With zero configuration, the generators scan your `.cshtml` files and group by controller folder:

```csharp
// Auto-generated from your views
public static class SwapViews
{
    public static class Products
    {
        public const string Index = "Index";
        public const string Details = "Details";
        public const string _Grid = "_Grid";           // Partials keep underscore
        public const string _Pagination = "_Pagination";
    }
}

public static class SwapElements
{
    public const string ProductGrid = "product-grid";
    public const string CartCount = "cart-count";
}

// Use instead of magic strings
builder.AlsoUpdate(SwapElements.CartCount, SwapViews.Cart._Count, count);
```

### 3. Setup (Zero Config!)

**No configuration required!** The `Swap.Htmx.targets` auto-includes common folders:

- `Views/**/*.cshtml`
- `Modules/**/Views/**/*.cshtml`
- `Pages/**/*.cshtml`
- `Components/**/*.cshtml`
- `Areas/**/Views/**/*.cshtml`

Just reference Swap.Htmx and build — views are scanned automatically.

**Optional:** To inspect generated code:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>obj\Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
<ItemGroup>
  <Compile Remove="obj\Generated\**\*.cs" />
</ItemGroup>
```

### 4. Compile-Time Validation

The `HandlerValidationAnalyzer` warns you about:
- `SWAP001`: Events without handlers
- `SWAP002`: Undefined event keys
- `SWAP003`: Circular event chains

📖 [Full Source Generators Guide](../../framework/Swap.Htmx.Generators/README.md)

---

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](docs/GettingStarted.md) | Full setup walkthrough |
| [SwapState](docs/SwapState.md) | Server-driven state management |
| [Events](docs/Events.md) | Event system deep dive |
| [Navigation](docs/Navigation.md) | SPA-style navigation |
| [Patterns](docs/Patterns.md) | Common patterns cheatsheet |
| [Real-time](docs/Realtime.md) | SSE & WebSocket |
| [Validation](docs/Validation.md) | Form validation |

---

## Demos

| Demo | Description |
|------|-------------|
| [SwapStateDemo](../../demo/SwapStateDemo) | State management patterns |
| [SwapLab](../../demo/SwapLab) | Pattern showcase |
| [SwapShop](../../demo/SwapShop) | E-commerce example |
| [SwapDashboard](../../demo/SwapDashboard) | Dashboard with events |
| [SwapSmallPartials](../../demo/SwapSmallPartials) | Complex UI orchestration |

---

## Philosophy

1. **HTML is the source of truth** — State lives in the DOM, not JavaScript
2. **Server renders everything** — No client-side templating
3. **One response, many updates** — OOB swaps coordinate UI
4. **Events decouple UI from logic** — Controllers don't know about layout
5. **No build tools** — Just .NET and HTML

---

## License

MIT License - see [LICENSE](../../LICENSE)

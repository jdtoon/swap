# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)

`Swap.Htmx` is a lightweight library that makes it easy to build HTMX-powered ASP.NET Core applications with a clean, fluent API.

## Key Features

- **Fluent response builder** - Coordinate view rendering, out-of-band swaps, toasts, and triggers in one clean chain
- **Minimal API Support** - Use `SwapResults` to return HTMX responses from Minimal API endpoints
- **Razor Pages Support** - Use `this.SwapResponse()` directly in your `PageModel`
- **Type-safe API** - No magic strings for swap modes or event names
- **Source Generators** - Automatically generate strongly-typed event keys from your constants
- **SwapController base class** - Automatically handles HTMX requests vs full page loads
- **Real-time updates** - Built-in WebSockets and Server-Sent Events (SSE) support with automatic connection management, room-based broadcasting, and `ISseBackplane` for distributed scaling
- **Observability** - Full OpenTelemetry support (Tracing & Metrics) and structured logging
- **Event system** - Build `HX-Trigger` headers declaratively and chain complex UI updates

## Install

**Using Templates (Recommended):**

```bash
dotnet new install Swap.Templates
dotnet new swap-mvc -n MyProject
```

**Manual Installation:**

```bash
dotnet add package Swap.Htmx
```

## Setup

Add the following to your `_Layout.cshtml` to enable the Toast system and client-side event handling:

```html
<head>
    <!-- ... other styles ... -->
    <link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />
    
    <!-- ... htmx script ... -->
    <script src="~/_content/Swap.Htmx/js/swap.js"></script>
</head>
```

## Four Ways to Build Responses

### 1. Simple View (80% of use cases)

For single updates with automatic partial/full view detection:

```csharp
public class ProductsController : SwapController
{
    public IActionResult Details(int id)
    {
        var product = _db.Products.Find(id);
        return SwapView("Details", product);
        // Returns full view for normal requests
        // Returns partial for HTMX requests
    }
}
```

### 2. Coordinated Updates (Manual Control)

When you need to update multiple parts of the page:

```csharp
public class CartController : SwapController
{
    public ActionResult AddToCart(int productId)
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

**Available methods:**
- `WithView(viewName, model)` - Set the main view to render
- `AlsoUpdate(targetId, viewName, model, swapMode)` - Add out-of-band swap
- `WithSuccessToast(message)` / `WithErrorToast(message)` / `WithWarningToast(message)` / `WithInfoToast(message)`
- `WithTrigger(eventName, payload)` - Add custom HX-Trigger event

**Note:** All triggers and toasts are automatically merged into a single `HX-Trigger` header. Multiple calls to `WithTrigger` and toast methods will create a JSON object containing all events.

**SwapMode options:**
- `SwapMode.OuterHTML` (default) - Replace entire element
- `SwapMode.InnerHTML` - Replace inner content only
- `SwapMode.BeforeBegin` / `AfterBegin` / `BeforeEnd` / `AfterEnd` - Insert content
- `SwapMode.Delete` - Remove the element

### 3. Minimal APIs

Swap provides first-class support for Minimal APIs via `SwapResults`.

```csharp
app.MapPost("/todo", (TodoItem item, ITodoService service) => 
{
    service.Add(item);
    
    return SwapResults.Response()
        .WithView("_TodoItem", item)
        .WithSuccessToast("Added!");
});
```

### 4. Razor Pages

Swap works natively with Razor Pages using extension methods.

```csharp
public class IndexModel : PageModel
{
    public IActionResult OnGetUpdate()
    {
        return this.SwapResponse()
            .WithView("_Partial", this)
            .Build();
    }
}
```
        .AlsoUpdate("todo-count", "_TodoCount", service.Count())
        .WithSuccessToast("Item added!");
});
```

### 5. Event-Driven (Configuration-Based Updates)

For complex apps where multiple controllers need coordinated updates, configure event chains once and trigger them anywhere:

```csharp
// Define constants for element IDs and view names (avoids magic strings)
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

// Configuration in Program.cs
builder.Services.AddSwapHtmx(options => 
{
    options.AddConfig<ProductEventConfig>();
});

// Define configuration class
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
```

### 4. Composition Over Inheritance (New in v1.2)

You don't have to inherit from `SwapController`. You can use standard ASP.NET Core controllers and access all Swap features via extension methods:

```csharp
using Swap.Htmx; // Import extension methods

public class ReviewsController : Controller
{
    [HttpPost]
    public IActionResult Add(Review review)
    {
        if (!ModelState.IsValid)
        {
            // New in v1.3: First-class validation support
            return this.SwapValidationErrors(ModelState)
                .AlsoUpdate("review-form", "_ReviewForm", review)
                .Build();
        }

        _service.Add(review);
        
        // Use extension methods on 'this' (ControllerBase)
        return this.SwapResponse()
            .WithSuccessToast("Review added!")
            .WithTrigger(ReviewEvents.Added, review)
            .Build();
    }

    [HttpGet]
    public IActionResult List(int productId)
    {
        var reviews = _service.GetByProductId(productId);
        
        // Use SwapView extension to handle partial/full view automatically
        return this.SwapView("_ReviewList", reviews);
    }
}
```

## Documentation

- [Getting Started](docs/GettingStarted.md)
- [Events & Triggers](docs/Events.md)
- [Event Chains](docs/EventChains.md)
- [Out-of-Band Swaps](docs/OutOfBandSwaps.md)
- [Server-Sent Events](docs/ServerSentEvents.md)
- [Debugging & Logging](docs/DebuggingAndLogging.md)

## License

MIT

    
    events.When(SwapEvents.Entity.Updated("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx => GetProducts(ctx))
          .InfoToast("Product updated!");
    
    events.When(SwapEvents.Entity.Deleted("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx => GetProducts(ctx))
          .RefreshPartial(ProductElements.Count, ProductViews.Count, ctx => GetProductCount(ctx))
          .WarningToast("Product deleted!");
});

// In your controller - just emit the event
public class ProductsController : SwapController
{
    public IActionResult Create(Product product)
    {
        _db.Products.Add(product);
        _db.SaveChangesAsync();
        
        // Event chain handles ALL UI updates based on configuration
        return SwapEvent(SwapEvents.Entity.Created("Product"));
    }
}
```

### Async Event Chains (v1.1)

Avoid thread starvation by using async model factories for database operations:

```csharp
// Configuration
events.When(ProductEvents.StockChecked)
      .RefreshPartialAsync(ProductElements.Stock, ProductViews.Stock, async ctx => 
      {
          var service = ctx.RequestServices.GetRequiredService<IProductService>();
          return await service.GetStockAsync(); // Safe async execution
      });

// Controller
public async Task<IActionResult> CheckStock(int id)
{
    // ... logic ...
    return await SwapEventAsync(ProductEvents.StockChecked);
}
```

**Benefits:**
- Centralized UI update configuration
- No repetition across controllers
- Easy to understand what updates when an event fires
- Change UI behavior without touching controller code

**Event chain builder methods:**
- `RefreshPartial(targetId, viewName, modelFactory, swapMode)` - Render and swap a partial
- `SuccessToast(message)` / `ErrorToast(message)` / `WarningToast(message)` / `InfoToast(message)` - Show toast
- `AlsoTrigger(eventKey)` - Trigger additional client-side events
- `Build()` - Complete the chain configuration

See [Events Documentation](Events/README_EVENTS.md) for more details on type-safe events.

## Setup

### Basic Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(); // Registers core services

var app = builder.Build();

app.UseSwapHtmx(); // Adds middleware for event handling
app.MapControllers();

app.Run();
```

### With Server-Sent Events

```csharp
builder.Services
    .AddSwapHtmx()
    .AddSseEventBridge(); // Enables SSE with connection management

app.UseSwapHtmx(); // Handles both HTTP responses and SSE broadcasts
```

## Server-Sent Events (SSE)

Stream real-time HTML updates to connected clients:

### Basic SSE

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

### Enhanced SSE with Connection Management

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
        
        // Join rooms for targeted broadcasting
        connection.JoinRoom(SseRooms.Dashboard);
        connection.SubscribeToEvent(SseEventNames.MetricsUpdated);
        
        // Keep connection alive
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(30000, ct);
        }
    });
}
```

## Manual HTMX Helpers

For low-level control, extension methods are available:

```csharp
// Check if request is from HTMX
if (Request.IsHtmxRequest())
{
    // Set response headers
    Response.HxTrigger("todoCreated");
    Response.HxTrigger("todoCreated", new { id = 123 });
    Response.HxRedirect("/todos");
    Response.HxRefresh();
}
```

## Dev Tooling

Visualize event chains and inspect SSE connections in development:

```csharp
app.MapSwapHtmxDevEndpoints(); // Available at /_swap/dev/*
```

## Migration from Manual Patterns

### Before (Manual Coordination)

```csharp
public static class TodoViews { public const string Todo = "_Todo"; }
public static class TodoElements { public const string Counter = "counter"; public const string Status = "status"; }
public static class TodoEvents { public static readonly EventKey Created = new("todo.created"); }

public IActionResult Create(TodoInput input)
{
    var todo = _service.Create(input);
    
    Response.ShowSuccessToast("Todo created!");
    Response.HxTrigger(TodoEvents.Created.Name);
    
    ViewData["OobCounter"] = RenderOobPanel(TodoElements.Counter, GetCount());
    ViewData["OobStatus"] = RenderOobPanel(TodoElements.Status, "Active");
    
    return SwapView(TodoViews.Todo, todo);
}
```

### After (Fluent Builder)

```csharp
public static class TodoViews 
{ 
    public const string Todo = "_Todo";
    public const string Counter = "_Counter";
    public const string Status = "_Status";
}

public ActionResult Create(TodoInput input)
{
    var todo = _service.Create(input);
    
    return SwapResponse()
        .WithView(TodoViews.Todo, todo)
        .AlsoUpdate(TodoElements.Counter, TodoViews.Counter, GetCount(), SwapMode.InnerHTML)
        .AlsoUpdate(TodoElements.Status, TodoViews.Status, "Active")
        .WithSuccessToast("Todo created!")
        .WithTrigger(TodoEvents.Created);
}
```

## Examples

### Complete Product Management Example

This example shows all three approaches in a single controller:

```csharp
// Constants for type safety (no magic strings!)
public static class ProductViews
{
    public const string List = "_ProductList";
    public const string Count = "_ProductCount";
    public const string Added = "_ProductAdded";
}

public static class ProductElements
{
    public const string List = "product-list";
    public const string Count = "product-count";
}

public static class CartElements
{
    public const string Badge = "cart-badge";
    public const string Total = "cart-total";
}

public static class CartViews
{
    public const string Badge = "_CartBadge";
    public const string Total = "_CartTotal";
}

// 1. Configure event chains (Program.cs)
builder.Services.AddSwapHtmx(events =>
{
    events.When(SwapEvents.Entity.Created("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx => 
              ctx.RequestServices.GetRequiredService<ProductService>().GetAll())
          .RefreshPartial(ProductElements.Count, ProductViews.Count, ctx => 
              ctx.RequestServices.GetRequiredService<ProductService>().GetCount())
          .SuccessToast("Product created successfully!");
});

// 2. Controller using all three approaches
public class ProductsController : SwapController
{
    private readonly ProductService _service;
    
    public ProductsController(ProductService service) => _service = service;
    
    // Approach 1: Simple view (list page)
    public IActionResult Index()
    {
        var products = _service.GetAll();
        return SwapView(products);
        // Auto-detects: full page on first load, partial on HTMX requests
    }
    
    // Approach 2: Coordinated updates (manual control for cart)
    public IActionResult AddToCart(int id)
    {
        var product = _service.Get(id);
        _cart.Add(product);
        
        return SwapResponse()
            .WithView(ProductViews.Added, product)
            .AlsoUpdate(CartElements.Badge, CartViews.Badge, _cart.ItemCount, SwapMode.InnerHTML)
            .AlsoUpdate(CartElements.Total, CartViews.Total, _cart.Total)
            .WithSuccessToast($"{product.Name} added to cart!")
            .WithTrigger(SwapEvents.UI.UpdateCounter, new { count = _cart.ItemCount });
    }
    
    // Approach 3: Event-driven (DRY for repeated patterns)
    public IActionResult Create(Product product)
    {
        _service.Create(product);
        
        // All UI updates happen automatically via configured event chain
        return SwapEvent(SwapEvents.Entity.Created("Product"));
    }
}
```

### Migration from Manual Patterns

**Before (manual ViewData + Response headers):**
```csharp
public IActionResult Create(Todo todo)
{
    _db.Todos.Add(todo);
    _db.SaveChangesAsync();
    
    // Scattered UI logic with magic strings everywhere
    ViewData["OobTodoList"] = await RenderPartialAsync("_TodoList", _db.Todos.ToList());
    ViewData["OobTodoCount"] = await RenderPartialAsync("_TodoCount", _db.Todos.Count());
    Response.AddSuccessToast("Todo created!");
    Response.AddTrigger("todoCreated");
    
    return PartialView("_TodoCreated", todo);
}
```

**After (fluent API with type-safe constants):**
```csharp
// Define once, use everywhere
public static class TodoViews
{
    public const string Created = "_TodoCreated";
    public const string List = "_TodoList";
    public const string Count = "_TodoCount";
}

public static class TodoElements
{
    public const string List = "todo-list";
    public const string Count = "todo-count";
}

public IActionResult Create(Todo todo)
{
    _db.Todos.Add(todo);
    _db.SaveChangesAsync();
    
    // Clean, discoverable, type-safe - no magic strings!
    return SwapResponse()
        .WithView(TodoViews.Created, todo)
        .AlsoUpdate(TodoElements.List, TodoViews.List, _db.Todos.ToList())
        .AlsoUpdate(TodoElements.Count, TodoViews.Count, _db.Todos.Count())
        .WithSuccessToast("Todo created!")
        .WithTrigger(SwapEvents.UI.RefreshList);
}
```

For complete working examples, see the `Swap.Htmx.TestApp` project in this repository.

## Documentation

- **[Getting Started Guide](docs/GettingStarted.md)** - Step-by-step tutorial for building your first HTMX app
- **[Type-Safe Events Guide](docs/Events.md)** - Learn how to define and use strongly-typed event keys to eliminate magic strings
- **[Event Chains Guide](docs/EventChains.md)** - Configure automatic UI updates when events are triggered
- **[Realtime (WebSockets & SSE)](docs/WebSockets.md)** - Detailed guide on realtime features
- **[Server-Sent Events Guide](docs/ServerSentEvents.md)** - Complete guide to real-time updates with SSE
- **[Out-of-Band Swaps Guide](docs/OutOfBandSwaps.md)** - Complete guide to multi-part page updates
- **[Minimal APIs Guide](docs/MinimalApis.md)** - Using Swap with Minimal APIs
- **[Razor Pages Guide](docs/RazorPages.md)** - Using Swap with Razor Pages
- **[Source Generators Guide](docs/SourceGenerators.md)** - Automatically generate type-safe event keys
- **[User Context & Identity](docs/UserContext.md)** - Abstracted user ID resolution
- **[Debugging & Observability](docs/DebuggingAndLogging.md)** - Configure OpenTelemetry tracing, metrics, and structured logging for production monitoring

## License

MIT
# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx)

`Swap.Htmx` is a lightweight library that makes it easy to build HTMX-powered ASP.NET Core applications with a clean, fluent API.

## Key Features

- **Fluent response builder** - Coordinate view rendering, out-of-band swaps, toasts, and triggers in one clean chain
- **Type-safe API** - No magic strings for swap modes or event names
- **SwapController base class** - Automatically handles HTMX requests vs full page loads
- **Server-sent events (SSE)** - Stream HTML updates with connection management
- **Event system** - Build `HX-Trigger` headers declaratively

## Install

```bash
dotnet add package Swap.Htmx
```

## Three Ways to Build Responses

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
            .WithView("_ProductAdded")
            .AlsoUpdate("cart-count", "_CartCount", _cart.Count, SwapMode.InnerHTML)
            .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)
            .WithSuccessToast("Added to cart!")
            .WithTrigger("cart.updated", new { itemCount = _cart.Count });
    }
}
```

**Available methods:**
- `WithView(viewName, model)` - Set the main view to render
- `AlsoUpdate(targetId, viewName, model, swapMode)` - Add out-of-band swap
- `WithSuccessToast(message)` / `WithErrorToast(message)` / `WithWarningToast(message)` / `WithInfoToast(message)`
- `WithTrigger(eventName, payload)` - Add custom HX-Trigger event

**SwapMode options:**
- `SwapMode.OuterHTML` (default) - Replace entire element
- `SwapMode.InnerHTML` - Replace inner content only
- `SwapMode.BeforeBegin` / `AfterBegin` / `BeforeEnd` / `AfterEnd` - Insert content
- `SwapMode.Delete` - Remove the element

### 3. Event-Driven (Coming Soon)

For complex apps where multiple controllers need coordinated updates:

```csharp
// Configuration in Program.cs
builder.Services.AddSwapHtmx(events => 
{
    events.When(OrderEvents.Created)
          .RefreshPartial("order-list", ctx => RenderOrderList(ctx))
          .RefreshPartial("order-count", ctx => RenderCount(ctx))
          .Toast("Order created!", ToastType.Success);
});

// In your controller
public ActionResult CreateOrder()
{
    var order = _service.CreateOrder(...);
    return SwapEvent(OrderEvents.Created, new { order.Id });
    // Event chains handle all UI updates automatically
}
```

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
[HttpGet("/sse/notifications")]
public IActionResult Notifications()
{
    return ServerSentEvents(async (stream, ct) =>
    {
        while (!ct.IsCancellationRequested)
        {
            var html = await RenderPartialToStringAsync("_Notification", GetLatestNotification());
            await stream.SendEventAsync("notification", html);
            await Task.Delay(5000, ct);
        }
    });
}
```

### Enhanced SSE with Connection Management

```csharp
[HttpGet("/sse/dashboard")]
public IActionResult Dashboard()
{
    return EnhancedServerSentEvents(async (builder, ct) =>
    {
        var connection = builder.Connection;
        
        // Join rooms for targeted broadcasting
        connection.JoinRoom("dashboard");
        connection.SubscribeToEvent("metrics-updated");
        
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
public IActionResult Create(TodoInput input)
{
    var todo = _service.Create(input);
    
    Response.ShowSuccessToast("Todo created!");
    Response.HxTrigger("todoCreated");
    
    ViewData["OobCounter"] = RenderOobPanel("counter", GetCount());
    ViewData["OobStatus"] = RenderOobPanel("status", "Active");
    
    return SwapView("_Todo", todo);
}
```

### After (Fluent Builder)

```csharp
public ActionResult Create(TodoInput input)
{
    var todo = _service.Create(input);
    
    return SwapResponse()
        .WithView("_Todo", todo)
        .AlsoUpdate("counter", "_Counter", GetCount(), SwapMode.InnerHTML)
        .AlsoUpdate("status", "_Status", "Active")
        .WithSuccessToast("Todo created!")
        .WithTrigger("todoCreated");
}
```

## Examples

For complete working examples, see the `Swap.Htmx.TestApp` project in this repository.

## License

MIT
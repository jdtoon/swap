# Event-Driven HTTP Responses

This guide explains how to use event chains to automatically coordinate UI updates when events are triggered.

## Overview

Instead of manually building responses in every controller action, you can configure **event chains** that define what should happen when specific events occur. This eliminates repetition and centralizes UI update logic.

## Quick Start

### 1. Configure Event Chains

In your `Program.cs`, define what should happen when events are triggered:

```csharp
builder.Services.AddSwapHtmx(events =>
{
    // When a product is created, refresh the list and show a toast
    events.When(SwapEvents.Entity.Created("Product"))
          .RefreshPartial("product-list", "_ProductList", ctx =>
          {
              var service = ctx.RequestServices.GetRequiredService<ProductService>();
              return service.GetAll();
          })
          .RefreshPartial("product-count", "_ProductCount", ctx =>
          {
              var service = ctx.RequestServices.GetRequiredService<ProductService>();
              return new { Count = service.GetCount() };
          })
          .SuccessToast("Product created successfully!");
});
```

### 2. Trigger Events in Controllers

In your controller, just emit the event:

```csharp
public class ProductsController : SwapController
{
    private readonly ProductService _service;
    
    public ProductsController(ProductService service) => _service = service;
    
    public IActionResult Create(Product product)
    {
        _service.Create(product);
        
        // All UI updates happen automatically based on configuration
        return SwapEvent(SwapEvents.Entity.Created("Product"));
    }
}
```

## How It Works

1. **Configuration Phase** - You define event chains in `Program.cs`
2. **Execution Phase** - When `SwapEvent()` is called:
   - The event chain executor looks up the configured chain
   - All model factories are invoked with the current `HttpContext`
   - A `SwapResponseBuilder` is created with all configured actions
   - The response is rendered and returned

## Event Chain Builder API

### RefreshPartial

Render a partial view and send it as an out-of-band swap:

```csharp
.RefreshPartial(
    targetId: "product-list",              // ID of element to update
    viewName: "_ProductList",               // Partial view to render
    modelFactory: ctx => GetProducts(ctx),  // Optional: function to create model
    swapMode: SwapMode.OuterHTML            // Optional: how to swap content
)
```

**Parameters:**
- `targetId` - The ID of the HTML element to update
- `viewName` - The partial view to render
- `modelFactory` (optional) - Function that receives `HttpContext` and returns the model
- `swapMode` (optional) - How to swap the content (defaults to `OuterHTML`)

### Toasts

Show toast notifications:

```csharp
.SuccessToast("Product created!")
.ErrorToast("Failed to delete product")
.WarningToast("Product stock is low")
.InfoToast("Product updated")
```

### AlsoTrigger

Trigger additional client-side events:

```csharp
.AlsoTrigger(SwapEvents.UI.UpdateCounter)
.AlsoTrigger(SwapEvents.Notification.Info)
```

This adds events to the `HX-Trigger` header that you can listen for on the client.

## Multiple Event Chains

You can configure multiple independent event chains:

```csharp
builder.Services.AddSwapHtmx(events =>
{
    // Product created
    events.When(SwapEvents.Entity.Created("Product"))
          .RefreshPartial("product-list", "_ProductList", ctx => GetProducts(ctx))
          .SuccessToast("Product created!");
    
    // Product updated
    events.When(SwapEvents.Entity.Updated("Product"))
          .RefreshPartial("product-list", "_ProductList", ctx => GetProducts(ctx))
          .InfoToast("Product updated!");
    
    // Product deleted
    events.When(SwapEvents.Entity.Deleted("Product"))
          .RefreshPartial("product-list", "_ProductList", ctx => GetProducts(ctx))
          .RefreshPartial("product-count", "_ProductCount", ctx => GetCount(ctx))
          .WarningToast("Product deleted!");
});
```

## Model Factories

Model factories give you access to the current `HttpContext` so you can:
- Resolve services from DI
- Access user information
- Read request data

```csharp
.RefreshPartial("cart-total", "_CartTotal", ctx =>
{
    // Get service from DI
    var cart = ctx.RequestServices.GetRequiredService<ICartService>();
    var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    // Build model
    return new CartTotalViewModel
    {
        Total = cart.GetTotal(userId),
        ItemCount = cart.GetItemCount(userId)
    };
})
```

## When to Use Event Chains

**Use event chains when:**
- Multiple UI elements need to update for the same business action
- The same update pattern repeats across controllers
- You want centralized UI update configuration
- Changes to UI behavior shouldn't require controller changes

**Use coordinated responses (`SwapResponse()`) when:**
- The update pattern is unique to one action
- You need fine control over the response
- The updates are simple and don't repeat

**Use simple views (`SwapView()`) when:**
- You're just rendering a single view
- No additional UI coordination needed

## Complete Example

```csharp
// Program.cs - Configure all product-related event chains
builder.Services.AddSwapHtmx(events =>
{
    events.When(SwapEvents.Entity.Created("Product"))
          .RefreshPartial("product-list", "_ProductList", GetProducts)
          .RefreshPartial("product-count", "_ProductCount", GetProductCount)
          .RefreshPartial("recent-activity", "_RecentActivity", GetRecentActivity)
          .SuccessToast("Product created successfully!")
          .AlsoTrigger(SwapEvents.UI.RefreshPage);
    
    events.When(SwapEvents.Entity.Updated("Product"))
          .RefreshPartial("product-list", "_ProductList", GetProducts)
          .RefreshPartial("recent-activity", "_RecentActivity", GetRecentActivity)
          .InfoToast("Product updated!");
    
    events.When(SwapEvents.Entity.Deleted("Product"))
          .RefreshPartial("product-list", "_ProductList", GetProducts)
          .RefreshPartial("product-count", "_ProductCount", GetProductCount)
          .RefreshPartial("recent-activity", "_RecentActivity", GetRecentActivity)
          .WarningToast("Product deleted!");
});

// Helper methods for model factories
static object GetProducts(HttpContext ctx)
{
    var service = ctx.RequestServices.GetRequiredService<ProductService>();
    return service.GetAll();
}

static object GetProductCount(HttpContext ctx)
{
    var service = ctx.RequestServices.GetRequiredService<ProductService>();
    return new { Count = service.GetCount() };
}

static object GetRecentActivity(HttpContext ctx)
{
    var service = ctx.RequestServices.GetRequiredService<ActivityService>();
    return service.GetRecent(10);
}

// ProductsController.cs - Clean controller actions
public class ProductsController : SwapController
{
    private readonly ProductService _service;
    
    public ProductsController(ProductService service) => _service = service;
    
    public IActionResult Create(Product product)
    {
        _service.Create(product);
        return SwapEvent(SwapEvents.Entity.Created("Product"));
    }
    
    public IActionResult Update(int id, Product product)
    {
        _service.Update(id, product);
        return SwapEvent(SwapEvents.Entity.Updated("Product"));
    }
    
    public IActionResult Delete(int id)
    {
        _service.Delete(id);
        return SwapEvent(SwapEvents.Entity.Deleted("Product"));
    }
}
```

## See Also

- [Type-Safe Events Guide](README_EVENTS.md) - Learn how to define strongly-typed events
- [Main README](../README.md) - Overview of all Swap.Htmx features

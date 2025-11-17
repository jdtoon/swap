# Event-Driven HTTP Responses

This guide explains how to use event chains to automatically coordinate UI updates when events are triggered.

## Overview

Instead of manually building responses in every controller action, you can configure **event chains** that define what should happen when specific events occur. This eliminates repetition and centralizes UI update logic.

## Quick Start

### 1. Configure Event Chains

In your `Program.cs`, define what should happen when events are triggered:

```csharp
// Define constants to eliminate magic strings
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

builder.Services.AddSwapHtmx(events =>
{
    // When a product is created, refresh the list and show a toast
    events.When(SwapEvents.Entity.Created("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx =>
          {
              var service = ctx.RequestServices.GetRequiredService<ProductService>();
              return service.GetAll();
          })
          .RefreshPartial(ProductElements.Count, ProductViews.Count, ctx =>
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
    targetId: ProductElements.List,         // ID of element to update (use constant!)
    viewName: ProductViews.List,            // Partial view to render (use constant!)
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
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx => GetProducts(ctx))
          .SuccessToast("Product created!");
    
    // Product updated
    events.When(SwapEvents.Entity.Updated("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx => GetProducts(ctx))
          .InfoToast("Product updated!");
    
    // Product deleted
    events.When(SwapEvents.Entity.Deleted("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, ctx => GetProducts(ctx))
          .RefreshPartial(ProductElements.Count, ProductViews.Count, ctx => GetCount(ctx))
          .WarningToast("Product deleted!");
});
```

## Model Factories

Model factories give you access to the current `HttpContext` so you can:
- Resolve services from DI
- Access user information
- Read request data

```csharp
.RefreshPartial(CartElements.Total, CartViews.Total, ctx =>
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
// Constants - define once, use everywhere (no magic strings!)
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

public static class ActivityViews
{
    public const string Recent = "_RecentActivity";
}

public static class ActivityElements
{
    public const string Recent = "recent-activity";
}

// Program.cs - Configure all product-related event chains
builder.Services.AddSwapHtmx(events =>
{
    events.When(SwapEvents.Entity.Created("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, GetProducts)
          .RefreshPartial(ProductElements.Count, ProductViews.Count, GetProductCount)
          .RefreshPartial(ActivityElements.Recent, ActivityViews.Recent, GetRecentActivity)
          .SuccessToast("Product created successfully!")
          .AlsoTrigger(SwapEvents.UI.RefreshPage);
    
    events.When(SwapEvents.Entity.Updated("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, GetProducts)
          .RefreshPartial(ActivityElements.Recent, ActivityViews.Recent, GetRecentActivity)
          .InfoToast("Product updated!");
    
    events.When(SwapEvents.Entity.Deleted("Product"))
          .RefreshPartial(ProductElements.List, ProductViews.List, GetProducts)
          .RefreshPartial(ProductElements.Count, ProductViews.Count, GetProductCount)
          .RefreshPartial(ActivityElements.Recent, ActivityViews.Recent, GetRecentActivity)
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

- [Type-Safe Events Guide](Events.md) - Learn how to define strongly-typed events
- [Main README](../README.md) - Overview of all Swap.Htmx features

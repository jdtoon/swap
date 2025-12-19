# Type-Safe Events Guide

Swap.Htmx uses `EventKey` for type-safe event handling, eliminating magic strings throughout your application.

**See also:**
- [Source Generators](SourceGenerators.md) (**Preferred**) - Automatically generate type-safe event keys from your constants.
- [Event Chains Guide](EventChains.md) - Learn how to configure automatic UI updates when events are triggered.
- [Typed Payloads](TypedPayloads.md) - Prefer stable DTO payloads over anonymous objects.
- [Event Naming & Realtime Routing](EventNamingAndRouting.md) - How dot-names relate to realtime broadcasting.
- [WebSockets & Realtime](WebSockets.md) - Learn how to broadcast events to connected clients.

## How HX-Trigger Headers Work

HTMX listens for the `HX-Trigger` response header to fire client-side events. Swap.Htmx automatically merges multiple triggers into a single JSON object:

```csharp
// In a Controller
return SwapResponse()
    .WithSuccessToast("Item saved!")
    .WithTrigger("itemSaved", item)
    .Build();

// In a Minimal API
return SwapResults.Response()
    .WithSuccessToast("Item saved!")
    .WithTrigger("itemSaved", item);
```

**Result:** Single `HX-Trigger` header with all events:
```json
{
  "showToast": {"type": "success", "message": "Item saved!"},
  "itemSaved": {"ProductId": 123, "Name": "Widget"},
  "listRefresh": null
}
```

This ensures toasts, custom events, and event chain payloads all work together seamlessly.

If an event payload is consumed by multiple places (client JS, multiple pages, realtime), prefer a small DTO payload type over anonymous objects. See [Typed Payloads](TypedPayloads.md).

## Type-Safe Event Keys

### 1. Define Your Events

Create static classes for each domain area in your application:

```csharp
// Events/ProductEvents.cs
using Swap.Htmx.Events;

public static class ProductEvents
{
    public static readonly EventKey Created = new("product.created");
    public static readonly EventKey Updated = new("product.updated");
    public static readonly EventKey Deleted = new("product.deleted");
    public static readonly EventKey PriceChanged = new("product.priceChanged");
    public static readonly EventKey OutOfStock = new("product.outOfStock");
}

// Events/CartEvents.cs
using Swap.Htmx.Events;

public static class CartEvents
{
    public static readonly EventKey ItemAdded = new("cart.itemAdded");
    public static readonly EventKey ItemRemoved = new("cart.itemRemoved");
    public static readonly EventKey Cleared = new("cart.cleared");
    public static readonly EventKey Updated = new("cart.updated");
}

// Events/OrderEvents.cs
using Swap.Htmx.Events;

public static class OrderEvents
{
    public static readonly EventKey Created = new("order.created");
    public static readonly EventKey Confirmed = new("order.confirmed");
    public static readonly EventKey Shipped = new("order.shipped");
    public static readonly EventKey Delivered = new("order.delivered");
    public static readonly EventKey Cancelled = new("order.cancelled");
}
```

### 2. Use in Controllers

```csharp
public static class ProductViews
{
    public const string Product = "_Product";
    public const string ProductAdded = "_ProductAdded";
    public const string Count = "_Count";
}

public static class ProductElements
{
    public const string Count = "product-count";
}

public static class CartElements
{
    public const string Badge = "cart-badge";
}

public static class CartViews
{
    public const string Badge = "_CartBadge";
}

public class ProductsController : SwapController
{
    public ActionResult Create(ProductInput input)
    {
        var product = _service.Create(input);
        
        return SwapResponse()
            .WithView(ProductViews.Product, product)
            .AlsoUpdate(ProductElements.Count, ProductViews.Count, GetCount())
            .WithSuccessToast("Product created!")
            .WithTrigger(ProductEvents.Created, new { product.Id });
            //          ^^^^^^^^^^^^^^^^^^^^^^
            //          Type-safe! IntelliSense works!
    }
    
    public ActionResult AddToCart(int productId)
    {
        _cart.Add(productId);
        
        return SwapResponse()
            .WithView(ProductViews.ProductAdded)
            .AlsoUpdate(CartElements.Badge, CartViews.Badge, _cart.Count)
            .WithSuccessToast("Added to cart!")
            .WithTrigger(CartEvents.ItemAdded, new { productId, count = _cart.Count });
            //          ^^^^^^^^^^^^^^^^^^^^^
            //          No magic strings!
    }
}

// OR using Composition (Standard Controller)
public class ProductsController : Controller
{
    public ActionResult Create(ProductInput input)
    {
        var product = _service.Create(input);
        
        // Use extension method
        return this.SwapResponse()
            .WithView(ProductViews.Product, product)
            .WithTrigger(ProductEvents.Created, new { product.Id })
            .Build();
    }
}
```

### 3. Use in Event Chains

```csharp
public static class ProductElements
{
    public const string List = "product-list";
    public const string Count = "product-count";
}

public static class CartElements
{
    public const string Dropdown = "cart-dropdown";
    public const string Badge = "cart-badge";
}

public static class SseRooms
{
    public const string Shopping = "shopping";
}

public static class CartSseEvents
{
    public const string Update = "cart-update";
}

// Program.cs
builder.Services.AddSwapHtmx(events =>
{
    events.When(ProductEvents.Created)
          .RefreshPartial(ProductElements.List, ctx => RenderProductList(ctx))
          .RefreshPartial(ProductElements.Count, ctx => RenderCount(ctx))
          .SuccessToast("Product created!");
    
    events.When(CartEvents.ItemAdded)
          .RefreshPartial(CartElements.Dropdown, ctx => RenderCart(ctx))
          .RefreshPartial(CartElements.Badge, ctx => RenderBadge(ctx))

     // Realtime: chain the domain event to an SSE broadcast key
     events.ChainToSse(CartEvents.ItemAdded, SseEvents.Room(SseRooms.Shopping, CartSseEvents.Update));
});
```

## Naming Conventions

Follow these patterns for consistency:

### Pattern: `{domain}.{action}`

- **Domain**: Lowercase entity or feature name (e.g., `product`, `cart`, `order`, `user`)
- **Action**: camelCase action verb (e.g., `created`, `updated`, `deleted`, `itemAdded`)

### Good Examples

```csharp
public static class ProductEvents
{
    public static readonly EventKey Created = new("product.created");
    public static readonly EventKey Updated = new("product.updated");
    public static readonly EventKey PriceChanged = new("product.priceChanged");
}

public static class NotificationEvents
{
    public static readonly EventKey Received = new("notification.received");
    public static readonly EventKey Read = new("notification.read");
    public static readonly EventKey Dismissed = new("notification.dismissed");
}
```

### Avoid

❌ Magic strings scattered in code:
```csharp
.WithTrigger("productCreated")  // Easy to typo
.WithTrigger("product-created") // Inconsistent format
.WithTrigger("PRODUCT_CREATED") // Different convention
```

✅ Type-safe event keys:
```csharp
.WithTrigger(ProductEvents.Created)  // Compile-time checked
```

## Advanced Patterns

### Nested Event Groups

For large applications, organize events hierarchically:

```csharp
public static class OrderEvents
{
    public static readonly EventKey Created = new("order.created");
    public static readonly EventKey Cancelled = new("order.cancelled");
    
    public static class Payment
    {
        public static readonly EventKey Started = new("order.payment.started");
        public static readonly EventKey Completed = new("order.payment.completed");
        public static readonly EventKey Failed = new("order.payment.failed");
    }
    
    public static class Shipping
    {
        public static readonly EventKey Prepared = new("order.shipping.prepared");
        public static readonly EventKey Shipped = new("order.shipping.shipped");
        public static readonly EventKey Delivered = new("order.shipping.delivered");
    }
}

// Usage:
.WithTrigger(OrderEvents.Payment.Completed)
.WithTrigger(OrderEvents.Shipping.Shipped)
```

### Parameterized Events

When you need dynamic event names, use factory methods:

```csharp
public static class UserEvents
{
    public static readonly EventKey LoggedIn = new("user.loggedIn");
    public static readonly EventKey LoggedOut = new("user.loggedOut");
    
    // Factory for role-specific events
    public static EventKey RoleChanged(string role) => new($"user.role.{role}");
    
    // Factory for permission events
    public static EventKey PermissionGranted(string permission) => new($"user.permission.{permission}.granted");
}

// Usage:
.WithTrigger(UserEvents.RoleChanged("admin"))
.WithTrigger(UserEvents.PermissionGranted("edit-products"))
```

## Built-in Event Catalogs

Swap.Htmx provides example events in `SwapEvents` that you can use or use as templates:

- **`SwapEvents.UI.*`** - UI interactions (refresh, modal, toast, etc.)
- **`SwapEvents.Entity.*`** - Generic CRUD operations
- **`SwapEvents.Auth.*`** - Authentication events
- **`SwapEvents.Form.*`** - Form validation events
- **`SwapEvents.Notification.*`** - User notification events

### Example Usage

```csharp
// Using built-in events
.WithTrigger(SwapEvents.UI.RefreshList)
.WithTrigger(SwapEvents.UI.CloseModal)
.WithTrigger(SwapEvents.Notification.Success)

// Mixing built-in and custom events
.WithTrigger(SwapEvents.UI.ShowSpinner)
.WithTrigger(ProductEvents.Created)
.WithTrigger(SwapEvents.UI.HideSpinner)
```

## Benefits of Type-Safe Events

✅ **IntelliSense support** - Discover available events while typing  
✅ **Compile-time checking** - Catch typos before runtime  
✅ **Refactoring safety** - Rename events across your codebase  
✅ **Consistency** - Enforce naming conventions  
✅ **Documentation** - Events are self-documenting  
✅ **Testability** - Easy to reference in tests  

## Migration from String-Based Events

### Before
```csharp
events.Emit("todo.created");
events.Emit("todoCreated");
events.Emit("TODO_CREATED");
// Which format is correct? Easy to make mistakes!
```

### After
```csharp
events.Emit(TodoEvents.Created);
// Always correct, always consistent
```

## Best Practices

1. **One file per domain** - Keep `ProductEvents`, `OrderEvents`, etc. in separate files
2. **Use readonly fields** - Prevents accidental modification
3. **Group related events** - Use nested classes for sub-domains
4. **Follow naming conventions** - Stick to `{domain}.{action}` pattern
5. **Avoid generic helpers** - Prefer `ProductEvents.Created` over `SwapEvents.Entity.Created("product")`
6. **Document complex events** - Add XML comments for events that need explanation

## Example: Complete E-Commerce Application

```csharp
// Events/ProductEvents.cs
public static class ProductEvents
{
    public static readonly EventKey Created = new("product.created");
    public static readonly EventKey Updated = new("product.updated");
    public static readonly EventKey Deleted = new("product.deleted");
    public static readonly EventKey Viewed = new("product.viewed");
}

// Events/CartEvents.cs
public static class CartEvents
{
    public static readonly EventKey ItemAdded = new("cart.itemAdded");
    public static readonly EventKey ItemRemoved = new("cart.itemRemoved");
    public static readonly EventKey QuantityUpdated = new("cart.quantityUpdated");
    public static readonly EventKey Cleared = new("cart.cleared");
}

// Events/CheckoutEvents.cs
public static class CheckoutEvents
{
    public static readonly EventKey Started = new("checkout.started");
    public static readonly EventKey AddressEntered = new("checkout.addressEntered");
    public static readonly EventKey PaymentSelected = new("checkout.paymentSelected");
    public static readonly EventKey Completed = new("checkout.completed");
}

// Events/OrderEvents.cs
public static class OrderEvents
{
    public static readonly EventKey Created = new("order.created");
    public static readonly EventKey Confirmed = new("order.confirmed");
    public static readonly EventKey Shipped = new("order.shipped");
    public static readonly EventKey Delivered = new("order.delivered");
}

// Controllers/ProductController.cs
public ActionResult AddToCart(int productId)
{
    var product = _products.Get(productId);
    _cart.Add(product);
    
    return SwapResponse()
        .WithView("_ProductAdded", product)
        .AlsoUpdate("cart-count", "_CartCount", _cart.Count)
        .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)
        .WithSuccessToast($"{product.Name} added to cart!")
        .WithTrigger(CartEvents.ItemAdded, new { 
            productId, 
            quantity = 1,
            cartTotal = _cart.Total 
        });
}
```

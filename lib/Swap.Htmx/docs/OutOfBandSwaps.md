# Out-of-Band Swaps Guide

Out-of-band (OOB) swaps let you update multiple parts of the page from a single HTMX request, even if those elements aren't the direct target of the request.

## What are Out-of-Band Swaps?

Normally, HTMX replaces the content of the `hx-target` element with the response. Out-of-band swaps let you also update **other elements** on the page at the same time.

### Example Scenario

When a user adds an item to their cart, you want to:
1. Show a "Product Added" message (main target)
2. Update the cart count in the header
3. Update the cart total
4. Show a success toast

All from one request!

## Basic Out-of-Band Swap

### Using SwapResponse Builder

The easiest way to add OOB swaps is with `SwapResponse()`:

```csharp
public class CartController : SwapController
{
    [HttpPost("/cart/add")]
    public IActionResult AddToCart([FromForm] int productId)
    {
        _cart.Add(productId);
        
        return SwapResponse()
            .WithView("_ProductAdded")                      // Main response
            .AlsoUpdate("cart-count", "_CartCount", _cart.ItemCount)  // OOB swap 1
            .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)      // OOB swap 2
            .WithSuccessToast("Added to cart!")                       // Toast
            .Build();
    }
}
```

### How It Works

1. `WithView("_ProductAdded")` - The main partial view returned to the `hx-target`
2. `AlsoUpdate("cart-count", ...)` - Generates HTML with `hx-swap-oob="true"` for the cart count
3. `AlsoUpdate("cart-total", ...)` - Generates HTML with `hx-swap-oob="true"` for the cart total
4. HTMX receives the response and updates all three elements

### The HTML Response

Swap.Htmx generates HTML like this:

```html
<!-- Main content (goes to hx-target) -->
<div class="product-added">
    <p>Product added to cart!</p>
</div>

<!-- OOB swap for cart count -->
<div id="cart-count" hx-swap-oob="true">
    <strong>5</strong> items
</div>

<!-- OOB swap for cart total -->
<div id="cart-total" hx-swap-oob="true">
    $99.99
</div>
```

## Swap Modes

Out-of-band swaps support different strategies for updating content:

```csharp
public enum SwapMode
{
    OuterHTML,      // Replace entire element (default)
    InnerHTML,      // Replace inner content only
    BeforeBegin,    // Insert before the element
    AfterBegin,     // Insert as first child
    BeforeEnd,      // Insert as last child
    AfterEnd,       // Insert after the element
    Delete,         // Remove the element
    None            // No swap
}
```

### OuterHTML (Default)

Replaces the entire element including its wrapper:

```csharp
.AlsoUpdate("cart-count", "_CartCount", 5, SwapMode.OuterHTML)
```

**Before:**
```html
<div id="cart-count">3 items</div>
```

**After:**
```html
<div id="cart-count">5 items</div>
```

### InnerHTML

Replaces only the content inside the element:

```csharp
.AlsoUpdate("cart-count", "_CartCount", 5, SwapMode.InnerHTML)
```

**Before:**
```html
<div id="cart-count" class="badge">3 items</div>
```

**After:**
```html
<div id="cart-count" class="badge">5 items</div>
```

The wrapper `<div>` with class `badge` is preserved!

### BeforeEnd

Appends content as the last child (useful for lists):

```csharp
.AlsoUpdate("todo-list", "_TodoItem", newTodo, SwapMode.BeforeEnd)
```

**Before:**
```html
<ul id="todo-list">
    <li>Task 1</li>
    <li>Task 2</li>
</ul>
```

**After:**
```html
<ul id="todo-list">
    <li>Task 1</li>
    <li>Task 2</li>
    <li>Task 3</li> <!-- New item appended -->
</ul>
```

### AfterBegin

Prepends content as the first child:

```csharp
.AlsoUpdate("notifications", "_Notification", notification, SwapMode.AfterBegin)
```

**Before:**
```html
<div id="notifications">
    <div>Old notification</div>
</div>
```

**After:**
```html
<div id="notifications">
    <div>New notification</div> <!-- New item prepended -->
    <div>Old notification</div>
</div>
```

### Delete

Removes the element from the page:

```csharp
.AlsoUpdate("task-123", null, null, SwapMode.Delete)
```

**Before:**
```html
<div id="task-list">
    <div id="task-123">Buy milk</div>
    <div id="task-124">Walk dog</div>
</div>
```

**After:**
```html
<div id="task-list">
    <div id="task-124">Walk dog</div>
</div>
```

## Real-World Examples

### Shopping Cart

```csharp
public class CartController : SwapController
{
    [HttpPost("/cart/add/{productId}")]
    public IActionResult Add(int productId)
    {
        var product = _products.Get(productId);
        _cart.Add(product);
        
        return SwapResponse()
            .WithView("_ProductAdded", product)
            .AlsoUpdate("cart-badge", "_CartBadge", _cart.ItemCount)
            .AlsoUpdate("cart-preview", "_CartPreview", _cart.Items)
            .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)
            .WithSuccessToast($"Added {product.Name} to cart")
            .Build();
    }
    
    [HttpDelete("/cart/remove/{productId}")]
    public IActionResult Remove(int productId)
    {
        var product = _products.Get(productId);
        _cart.Remove(productId);
        
        return SwapResponse()
            .WithView("_Empty")  // Or redirect
            .AlsoUpdate($"cart-item-{productId}", null, null, SwapMode.Delete)
            .AlsoUpdate("cart-badge", "_CartBadge", _cart.ItemCount)
            .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)
            .WithInfoToast($"Removed {product.Name} from cart")
            .Build();
    }
}
```

### Todo List with Count

```csharp
public class TodosController : SwapController
{
    [HttpPost("/todos")]
    public IActionResult Create([FromForm] string title)
    {
        var todo = _todos.Create(title);
        
        return SwapResponse()
            .WithView("_TodoItem", todo)
            .AlsoUpdate("todo-count", "_TodoCount", _todos.Count)
            .AlsoUpdate("completed-count", "_CompletedCount", _todos.CompletedCount)
            .WithSuccessToast("Todo created!")
            .Build();
    }
    
    [HttpPatch("/todos/{id}/toggle")]
    public IActionResult Toggle(int id)
    {
        var todo = _todos.Toggle(id);
        
        return SwapResponse()
            .WithView("_TodoItem", todo)
            .AlsoUpdate("completed-count", "_CompletedCount", _todos.CompletedCount)
            .Build();
    }
}
```

### Notification System

```csharp
public class NotificationsController : SwapController
{
    [HttpPost("/notifications/mark-read/{id}")]
    public IActionResult MarkRead(int id)
    {
        _notifications.MarkRead(id);
        
        var unreadCount = _notifications.UnreadCount();
        
        return SwapResponse()
            .WithView("_Empty")
            .AlsoUpdate($"notification-{id}", "_Notification", 
                _notifications.Get(id), SwapMode.OuterHTML)
            .AlsoUpdate("unread-badge", "_UnreadBadge", 
                unreadCount, SwapMode.InnerHTML)
            .Build();
    }
    
    [HttpDelete("/notifications/{id}")]
    public IActionResult Delete(int id)
    {
        _notifications.Delete(id);
        
        return SwapResponse()
            .AlsoUpdate($"notification-{id}", null, null, SwapMode.Delete)
            .AlsoUpdate("unread-badge", "_UnreadBadge", 
                _notifications.UnreadCount(), SwapMode.InnerHTML)
            .WithInfoToast("Notification deleted")
            .Build();
    }
}
```

### Dashboard with Live Stats

```csharp
public class DashboardController : SwapController
{
    [HttpPost("/tasks/create")]
    public IActionResult CreateTask([FromForm] TaskInput input)
    {
        var task = _tasks.Create(input);
        
        return SwapResponse()
            .WithView("_TaskCard", task)
            .AlsoUpdate("task-count", "_TaskCount", _tasks.Count)
            .AlsoUpdate("pending-count", "_PendingCount", _tasks.PendingCount)
            .AlsoUpdate("recent-activity", "_RecentActivity", 
                _activity.GetRecent(10), SwapMode.InnerHTML)
            .WithSuccessToast("Task created!")
            .Build();
    }
}
```

## Best Practices

### 1. Use Constants for Element IDs

Avoid magic strings by defining constants:

```csharp
public static class CartElements
{
    public const string Badge = "cart-badge";
    public const string Preview = "cart-preview";
    public const string Total = "cart-total";
    public static string Item(int id) => $"cart-item-{id}";
}

public static class CartViews
{
    public const string Badge = "_CartBadge";
    public const string Preview = "_CartPreview";
    public const string Total = "_CartTotal";
}

// Usage
.AlsoUpdate(CartElements.Badge, CartViews.Badge, _cart.ItemCount)
.AlsoUpdate(CartElements.Total, CartViews.Total, _cart.Total)
```

### 2. Keep Partials Clean

OOB partial views should not include layouts:

```cshtml
@* ✅ Good - clean partial *@
@model int
@{
    Layout = null;
}

<span class="badge">@Model</span>

@* ❌ Bad - includes layout *@
@model int
@{
    Layout = "_Layout";  <!-- Don't use layouts! -->
}
```

### 3. Match Element IDs Exactly

The element ID in HTML must match the target ID exactly:

```html
<!-- ✅ Good - IDs match -->
<div id="cart-count">3 items</div>

<!-- ❌ Bad - IDs don't match -->
<div id="cartCount">3 items</div>  <!-- camelCase vs kebab-case -->
```

### 4. Choose the Right Swap Mode

- **OuterHTML** - When you want to replace everything
- **InnerHTML** - When you want to keep the wrapper/attributes
- **BeforeEnd** - When appending to a list
- **AfterBegin** - When prepending to a list
- **Delete** - When removing items

### 5. Limit OOB Swaps Per Request

Too many OOB swaps can impact performance. If you're updating more than 5-6 elements, consider:
- Breaking the action into multiple requests
- Using Server-Sent Events for real-time updates
- Restructuring your UI

## Troubleshooting

### OOB Swaps Not Working

**Problem:** Out-of-band elements aren't updating.

**Solutions:**
1. Check element IDs match exactly (case-sensitive!)
2. Ensure target elements exist on the page
3. Verify partial views render without errors
4. Check browser console for HTMX errors
5. Use browser DevTools Network tab to inspect response HTML

### Element Not Found Warnings

**Problem:** Console shows "element with id X not found for oob swap".

**Solutions:**
1. Element might not exist on current page - this is OK if intentional
2. Check for typos in element IDs
3. Ensure element is in the DOM (not hidden by conditional rendering)

### Partial View Not Rendering

**Problem:** OOB swap target is empty or shows error.

**Solutions:**
1. Check partial view path is correct
2. Verify model type matches what view expects
3. Ensure partial view has `Layout = null`
4. Review server logs for rendering exceptions

### Wrong Content Swapped

**Problem:** Content swaps into wrong element.

**Solutions:**
1. Check for duplicate element IDs on page
2. Verify you're using correct swap mode (OuterHTML vs InnerHTML)
3. Ensure target element ID is unique

## Advanced Patterns

### Conditional OOB Swaps

Only add OOB swaps when needed:

```csharp
var builder = SwapResponse()
    .WithView("_TaskCard", task);

if (task.IsUrgent)
{
    builder.AlsoUpdate("urgent-tasks", "_UrgentTasks", _tasks.GetUrgent());
}

if (_tasks.IsOverCapacity())
{
    builder.WithWarningToast("Team is over capacity!");
}

return builder.Build();
```

### Dynamic Element IDs

Use helper methods for instance-specific IDs:

```csharp
public static class TaskElements
{
    public static string Card(int id) => $"task-card-{id}";
    public static string Status(int id) => $"task-status-{id}";
}

// Usage
.AlsoUpdate(TaskElements.Card(task.Id), "_TaskCard", task)
.AlsoUpdate(TaskElements.Status(task.Id), "_TaskStatus", task.Status)
```

### Chaining Multiple Updates

Build complex responses step by step:

```csharp
var response = SwapResponse()
    .WithView("_OrderConfirmation", order);

// Update all affected areas
response
    .AlsoUpdate("cart-badge", "_CartBadge", 0)
    .AlsoUpdate("order-history", "_OrderHistory", _orders.Recent(5), SwapMode.InnerHTML)
    .AlsoUpdate("inventory-alert", "_InventoryAlert", _inventory.LowStock());

// Add notifications
if (order.Total > 100)
{
    response.WithSuccessToast($"Order placed! You saved ${order.Discount}");
}
else
{
    response.WithSuccessToast("Order placed successfully!");
}

return response.Build();
```

## See Also

- **[Getting Started Guide](GettingStarted.md)** - Learn the basics
- **[SwapController Guide](SwapController.md)** - Controller features
- **[Event Chains Guide](EventChains.md)** - Declarative UI updates
- **[Server-Sent Events Guide](ServerSentEvents.md)** - Real-time updates

---

Out-of-band swaps are one of HTMX's most powerful features, and Swap.Htmx makes them easy to use with a clean, type-safe API!

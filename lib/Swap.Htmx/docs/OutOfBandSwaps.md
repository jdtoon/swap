# Out-of-Band Swaps

Update multiple page elements from a single request.

---

## The Problem

User adds item to cart. You need to:
1. Show "Added!" message (main response)
2. Update cart count in header
3. Update cart total
4. Show toast notification

**All from one HTTP request.**

---

## The Solution

```csharp
[HttpPost]
public IActionResult AddToCart(int productId)
{
    _cart.Add(productId);
    
    return SwapResponse()
        .WithView("_ProductAdded", product)                        // Main response
        .AlsoUpdate("cart-count", "_CartCount", _cart.Count)       // OOB: header count
        .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)       // OOB: sidebar total
        .WithSuccessToast("Added to cart!")                        // Toast
        .Build();
}
```

### HTML Response Generated

```html
<!-- Main content → hx-target -->
<div class="alert">Added to cart!</div>

<!-- OOB swaps → find elements by ID, replace -->
<span id="cart-count" hx-swap-oob="true">5</span>
<span id="cart-total" hx-swap-oob="true">$99.99</span>
```

HTMX receives this and updates all three elements automatically.

---

## AlsoUpdate Parameters

```csharp
.AlsoUpdate(
    "element-id",      // Target element ID (without #)
    "_PartialView",    // Partial view to render
    model,             // Model for the view
    SwapMode.InnerHtml // Optional: swap strategy
)
```

### Swap Modes

| Mode | What It Does |
|------|--------------|
| `InnerHtml` | Replace inner content (default) |
| `OuterHtml` | Replace entire element |
| `BeforeEnd` | Append inside element |
| `AfterEnd` | Insert after element |
| `Delete` | Remove element |

```csharp
// Append new item to list
.AlsoUpdate("todo-list", "_TodoItem", newTodo, SwapMode.BeforeEnd)

// Delete an element
.AlsoUpdate("notification-5", "", null, SwapMode.Delete)
```

---

## Decoupled Updates (Event Handlers)

For larger apps, decouple OOB logic from controllers:

```csharp
// Handler adds cart count update automatically
[SwapHandler(typeof(CartEvents), nameof(CartEvents.ItemAdded))]
public class CartCountHandler : ISwapEventHandler<CartPayload>
{
    public void Handle(SwapEventContext<CartPayload> ctx)
    {
        ctx.Response.AlsoUpdate("cart-count", "_CartCount", ctx.Payload.Count);
    }
}

// Controller just fires event
public IActionResult AddToCart(int productId)
{
    var result = _cart.Add(productId);
    return SwapEvent(CartEvents.ItemAdded, new CartPayload(result.Count))
        .WithView("_ProductAdded")
        .Build();
}
```

See [Event Chains](EventChains.md) for the full pattern.

---

## Common Patterns

### Update Count Badge

```csharp
.AlsoUpdate("notification-count", "_Badge", unreadCount)
```

### Refresh List After Add

```csharp
.AlsoUpdate("item-list", "_ItemList", allItems)
```

### Remove Deleted Item

```csharp
.AlsoUpdate($"item-{id}", "", null, SwapMode.Delete)
```

### Update Multiple Related Elements

```csharp
return SwapResponse()
    .WithView("_OrderConfirmation", order)
    .AlsoUpdate("cart-count", "_CartCount", 0)
    .AlsoUpdate("cart-total", "_CartTotal", 0)
    .AlsoUpdate("cart-items", "_EmptyCart", null)
    .Build();
```

---

## Best Practices

✅ **Use for related updates** — Cart count when adding items  
✅ **Keep responses focused** — 2-4 OOB swaps max  
✅ **Use event handlers** — Decouple in larger apps  

❌ **Don't do kitchen-sink responses** — 10+ unrelated OOB swaps  
❌ **Don't duplicate logic** — Use handlers instead  

---

## See Also

- [Patterns](Patterns.md) — More examples
- [Event Chains](EventChains.md) — Decoupled OOB with handlers
- [SwapResponse](../README.md) — Full builder API

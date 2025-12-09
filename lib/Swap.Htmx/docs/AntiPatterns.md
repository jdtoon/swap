# Anti-Patterns

Common mistakes and how to avoid them.

---

## Navigation

### ❌ Full Page Reloads

```html
<!-- Bad: full reload -->
<a href="/products">Products</a>

<!-- Good: partial swap -->
<swap-nav to="/products">Products</swap-nav>
```

### ❌ Using View() for HTMX Requests

```csharp
// Bad: always includes layout
return View(model);

// Good: auto-detects HTMX
return SwapView(model);
```

### ❌ Redirect After POST

```csharp
// Bad: HTMX follows redirect, loads full page in target
return Redirect("/products");

// Good: return partial or use HX-Redirect
return SwapResponse()
    .WithView("_Success")
    .WithSuccessToast("Created!")
    .Build();
```

---

## State

### ❌ Forgetting hx-include

```html
<!-- Bad: state not sent -->
<button hx-get="/filter">Filter</button>

<!-- Good: include state container -->
<button hx-get="/filter" hx-include="#filter-state">Filter</button>
```

### ❌ URL Params After hx-include (Order Matters!)

```html
<!-- Bad: hidden field value wins (comes first) -->
<button hx-get="/filter" hx-include="#state">
<!-- Request: ?Category=all (from hidden) - old value! -->

<!-- Good: URL params first, they win -->
<button hx-get="/filter?Category=electronics" hx-include="#state">
<!-- Request: ?Category=electronics&Category=all - first wins! -->
```

### ❌ Complex Objects in Hidden Fields

```csharp
// Bad: serialize whole objects
public List<Product> SelectedProducts { get; set; }

// Good: store IDs only
public string SelectedProductIds { get; set; }  // "1,2,3"
```

---

## Events

### ❌ Magic Strings

```csharp
// Bad: typos break everything
.WithTrigger("productCreated")
.WithTrigger("product-created")  // Different event!

// Good: type-safe keys
.WithTrigger(ProductEvents.Created)
```

### ❌ Circular Event Chains

```csharp
// Bad: infinite loop
events.When(EventA).Trigger(EventB);
events.When(EventB).Trigger(EventA);  // 💥
```

---

## OOB Swaps

### ❌ Target Doesn't Exist

```csharp
// Bad: element ID doesn't exist in DOM
.AlsoUpdate("cart-badge", "_Badge", count)
// Silently fails!

// Good: ensure target exists before updating
```

### ❌ Kitchen Sink Response

```csharp
// Bad: too many unrelated updates
return SwapResponse()
    .WithView("_Product")
    .AlsoUpdate("header", ...)
    .AlsoUpdate("sidebar", ...)
    .AlsoUpdate("footer", ...)
    .AlsoUpdate("breadcrumbs", ...)
    .AlsoUpdate("notifications", ...)
    // Controller knows too much about layout!

// Good: use event handlers for decoupling
return SwapEvent(ProductEvents.Created, payload)
    .WithView("_Product")
    .Build();
```

---

## Performance

### ❌ No Debouncing on Search

```html
<!-- Bad: request on every keystroke -->
<input hx-get="/search" hx-trigger="keyup">

<!-- Good: debounce -->
<input hx-get="/search" hx-trigger="keyup changed delay:300ms">
```

### ❌ No Loading Indicators

```html
<!-- Bad: no feedback -->
<button hx-post="/slow-action">Submit</button>

<!-- Good: show loading state -->
<button hx-post="/slow-action" hx-indicator="#spinner">Submit</button>
<span id="spinner" class="htmx-indicator">Loading...</span>
```

---

## Security

### ❌ Trusting Hidden Fields

```csharp
// Bad: trust client data
public IActionResult Update([FromSwapState] OrderState state)
{
    var order = _db.Orders.Find(state.OrderId);
    order.Price = state.Price;  // User could modify hidden field!
}

// Good: verify server-side
public IActionResult Update([FromSwapState] OrderState state)
{
    var order = _db.Orders.Find(state.OrderId);
    if (order.UserId != CurrentUser.Id) return Forbid();
    // Recalculate price, don't trust client
}
```

---

## HTML

### ❌ Duplicate IDs

```html
<!-- Bad: same ID twice -->
<div id="product-card">...</div>
<div id="product-card">...</div>  <!-- OOB swaps hit wrong one! -->

<!-- Good: unique IDs -->
<div id="product-card-1">...</div>
<div id="product-card-2">...</div>
```

### ❌ Form Inside Form

```html
<!-- Bad: invalid HTML, unpredictable behavior -->
<form hx-post="/outer">
    <form hx-post="/inner">  <!-- Nope! -->
    </form>
</form>
```

---

## Quick Checklist

- [ ] Using `SwapView()` not `View()`?
- [ ] Using `<swap-nav>` for navigation?
- [ ] `hx-include` pointing to state container?
- [ ] URL params BEFORE hx-include for overrides?
- [ ] Type-safe event keys?
- [ ] Debouncing search inputs?
- [ ] Loading indicators on slow actions?
- [ ] Validating/authorizing server-side?
- [ ] Unique element IDs?

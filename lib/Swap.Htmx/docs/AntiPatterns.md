# Anti-Patterns Guide

This guide documents common mistakes when building Swap.Htmx applications and how to avoid them. Learn from others' mistakes!

---

## Navigation & Routing

### ❌ Full Page Reloads for Navigation

```html
<!-- ANTI-PATTERN: Causes full page reload -->
<a href="/inventory">Inventory</a>
```

**Problem:** Loses all client-side state, rerenders entire page, poor UX.

```html
<!-- ✅ CORRECT: Partial swap with URL update -->
<a hx-get="/inventory"
   hx-target="#main-content"
   hx-push-url="true">
    Inventory
</a>
```

### ❌ Using View() Instead of SwapView()

```csharp
// ANTI-PATTERN: Always returns full layout
public IActionResult Index()
{
    return View(model);  // Includes _Layout.cshtml even for HTMX requests
}
```

**Problem:** HTMX requests get the full page HTML including layout, causing double headers/footers.

```csharp
// ✅ CORRECT: Automatically handles HTMX detection
public IActionResult Index()
{
    return SwapView(model);  // Partial for HTMX, full for browser
}
```

### ❌ Hardcoded Redirects After Actions

```csharp
// ANTI-PATTERN: Redirects break HTMX flow
[HttpPost]
public IActionResult Create(ItemModel model)
{
    _service.Create(model);
    return Redirect("/inventory");  // HTMX will follow this, loading full page
}
```

**Problem:** HTMX follows redirects, causing unexpected full page loads in the target div.

```csharp
// ✅ CORRECT: Return partial with event trigger
[HttpPost]
public IActionResult Create(ItemModel model)
{
    _service.Create(model);
    return SwapResponse()
        .WithTrigger(InventoryEvents.DataChanged)
        .WithSuccessToast("Item created!")
        .Build();
}

// Or use HX-Redirect header for intentional navigation
[HttpPost]
public IActionResult Create(ItemModel model)
{
    _service.Create(model);
    return SwapResponse()
        .WithRedirect("/inventory")  // Uses HX-Redirect header
        .Build();
}
```

---

## State Management

### ❌ Updating State in after-request

```html
<!-- ANTI-PATTERN: State updated too late -->
<button hx-get="/inventory/grid"
        hx-on::after-request="document.getElementById('currentTab').value = 'active'">
    Active
</button>
```

**Problem:** Other components listening for the `HX-Trigger` event read the state BEFORE `after-request` fires, getting stale values.

```html
<!-- ✅ CORRECT: State updated before request -->
<button hx-get="/inventory/grid"
        hx-on::before-request="document.getElementById('currentTab').value = 'active'">
    Active
</button>
```

### ❌ Forgetting hx-include

```html
<!-- ANTI-PATTERN: State not sent with request -->
<button hx-get="/inventory/grid" hx-target="#grid">
    Load Data
</button>

<!-- Hidden state container exists but isn't included -->
<div id="inventory-state">
    <input type="hidden" name="tab" value="active" />
</div>
```

**Problem:** Server doesn't receive current state, returns wrong data.

```html
<!-- ✅ CORRECT: Include state container -->
<button hx-get="/inventory/grid" 
        hx-target="#grid"
        hx-include="#inventory-state">
    Load Data
</button>
```

### ❌ Storing Complex Objects in Hidden Fields

```html
<!-- ANTI-PATTERN: Serialized object in hidden field -->
<input type="hidden" name="filter" value='{"categories":["A","B"],"priceRange":{"min":0,"max":100}}' />
```

**Problem:** Hard to update individual properties, easy to corrupt JSON, security risk (trusting client data).

```html
<!-- ✅ CORRECT: Flat hidden fields -->
<input type="hidden" name="category" value="A" />
<input type="hidden" name="category" value="B" />  <!-- Multiple values OK -->
<input type="hidden" name="priceMin" value="0" />
<input type="hidden" name="priceMax" value="100" />
```

---

## Events

### ❌ Circular Event Chains

```csharp
// ANTI-PATTERN: Infinite loop
public class BadConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        events.When(InventoryEvents.StateChanged)
              .TriggerEvent(InventoryEvents.StateChanged);  // Loops forever!
    }
}
```

**Problem:** Event triggers itself, creating infinite loop.

```csharp
// ✅ CORRECT: Distinct events for different purposes
public static class InventoryEvents
{
    public static readonly EventKey FilterChanged = new("inventory.filter.changed");
    public static readonly EventKey DataLoaded = new("inventory.data.loaded");
}

// FilterChanged triggers data load, DataLoaded does NOT trigger FilterChanged
```

### ❌ Magic String Event Names

```csharp
// ANTI-PATTERN: Typos won't be caught
return SwapResponse()
    .WithTrigger("inventory.chagned")  // Typo! No compile error
    .Build();
```

**Problem:** No compile-time checking, easy to make typos that silently fail.

```csharp
// ✅ CORRECT: Type-safe event keys
public static class InventoryEvents
{
    public static readonly EventKey Changed = new("inventory.changed");
}

return SwapResponse()
    .WithTrigger(InventoryEvents.Changed)  // Compile-time checked
    .Build();
```

### ❌ Triggering Events Without Handlers

```csharp
// ANTI-PATTERN: Event triggered but nothing listens
return SwapResponse()
    .WithTrigger(SomeObscureEvent)  // No components listen for this
    .WithSuccessToast("Done!")
    .Build();
```

**Problem:** Event fires but nothing happens, confusing debugging.

```csharp
// ✅ CORRECT: Ensure something listens
// Either in HTML:
// hx-trigger="some.obscure.event from:body"

// Or validate during startup (future feature)
options.ValidateEventHandlers = true;
```

---

## OOB Swaps

### ❌ OOB Target Doesn't Exist

```csharp
// ANTI-PATTERN: Targeting element that may not be on page
return SwapResponse()
    .WithView("_Grid", model)
    .AlsoUpdate("sidebar-stats", "_Stats", stats)  // Sidebar not on this page!
    .Build();
```

**Problem:** Silent failure - OOB content is ignored, no error shown.

```csharp
// ✅ CORRECT: Conditional OOB (future feature)
return SwapResponse()
    .WithView("_Grid", model)
    .AlsoUpdateIfExists("sidebar-stats", "_Stats", stats)
    .Build();

// Or check in controller
if (Request.Headers["HX-Current-URL"]?.Contains("/dashboard") == true)
{
    builder.AlsoUpdate("sidebar-stats", "_Stats", stats);
}
```

### ❌ Inconsistent Element IDs

```html
<!-- Page 1: Uses kebab-case -->
<div id="inventory-grid">...</div>

<!-- Page 2: Uses camelCase -->
<div id="inventoryGrid">...</div>
```

```csharp
// Controller doesn't know which to use
.AlsoUpdate("inventory-grid", "_Grid", model)  // Works on page 1, fails on page 2
```

**Problem:** OOB swaps work on some pages, fail on others.

```csharp
// ✅ CORRECT: Centralized element ID constants
public static class InventoryElements
{
    public const string Grid = "inventory-grid";
    public const string Pagination = "inventory-pagination";
    public const string Tabs = "inventory-tabs";
}

// Use everywhere
.AlsoUpdate(InventoryElements.Grid, "_Grid", model)
```

### ❌ Too Many OOB Swaps

```csharp
// ANTI-PATTERN: Kitchen sink response
return SwapResponse()
    .WithView("_ItemRow", item)
    .AlsoUpdate("header", "_Header", headerModel)
    .AlsoUpdate("sidebar", "_Sidebar", sidebarModel)
    .AlsoUpdate("footer", "_Footer", footerModel)
    .AlsoUpdate("breadcrumbs", "_Breadcrumbs", breadcrumbModel)
    .AlsoUpdate("notifications", "_Notifications", notificationModel)
    .AlsoUpdate("user-menu", "_UserMenu", userModel)
    .Build();
```

**Problem:** Coupling controller to entire page layout, large response payload, hard to maintain.

```csharp
// ✅ CORRECT: Trigger event, let handlers decide
return SwapResponse()
    .WithView("_ItemRow", item)
    .WithTrigger(ItemEvents.Updated, item)  // Handlers attach what they need
    .Build();
```

---

## Performance

### ❌ Large Payloads in Every Response

```csharp
// ANTI-PATTERN: Sending entire dataset
return SwapResponse()
    .WithView("_Grid", allItems)  // 10,000 items
    .Build();
```

**Problem:** Slow response, browser struggles to render.

```csharp
// ✅ CORRECT: Pagination
return SwapResponse()
    .WithView("_Grid", pagedItems)  // 20 items
    .AlsoUpdate("pagination", "_Pagination", paginationModel)
    .Build();
```

### ❌ No Loading Indicators

```html
<!-- ANTI-PATTERN: No feedback during load -->
<button hx-get="/slow-operation">
    Load Data
</button>
```

**Problem:** User doesn't know something is happening, may click multiple times.

```html
<!-- ✅ CORRECT: Loading indicator -->
<button hx-get="/slow-operation"
        hx-indicator="#loading-spinner"
        hx-disabled-elt="this">
    Load Data
</button>
<span id="loading-spinner" class="htmx-indicator">Loading...</span>
```

### ❌ No Debouncing on Search

```html
<!-- ANTI-PATTERN: Fires on every keystroke -->
<input type="search"
       hx-get="/search"
       hx-trigger="input">
```

**Problem:** Hammers server with requests, poor UX with flickering results.

```html
<!-- ✅ CORRECT: Debounced search -->
<input type="search"
       hx-get="/search"
       hx-trigger="input changed delay:300ms">
```

---

## Security

### ❌ Trusting Hidden Field Data

```csharp
// ANTI-PATTERN: Trusting client-provided ID
[HttpPost]
public IActionResult UpdatePrice([FromForm] int productId, [FromForm] decimal price)
{
    _service.UpdatePrice(productId, price);  // No authorization check!
    return Ok();
}
```

**Problem:** User can modify hidden fields to update any product.

```csharp
// ✅ CORRECT: Server-side authorization
[HttpPost]
[Authorize]
public IActionResult UpdatePrice([FromForm] int productId, [FromForm] decimal price)
{
    var product = _service.GetById(productId);
    if (product.OwnerId != User.GetUserId())
    {
        return Forbid();
    }
    
    _service.UpdatePrice(productId, price);
    return SwapResponse().WithSuccessToast("Price updated").Build();
}
```

### ❌ Sensitive Data in Event Payloads

```csharp
// ANTI-PATTERN: Exposing sensitive data
return SwapResponse()
    .WithTrigger(UserEvents.Updated, new { 
        Id = user.Id,
        Email = user.Email,
        PasswordHash = user.PasswordHash  // EXPOSED!
    })
    .Build();
```

**Problem:** Event payloads are visible in browser dev tools.

```csharp
// ✅ CORRECT: Minimal, safe payloads
return SwapResponse()
    .WithTrigger(UserEvents.Updated, new { Id = user.Id })
    .Build();
```

---

## HTML/DOM

### ❌ Duplicate IDs

```html
<!-- ANTI-PATTERN: Same ID used multiple times -->
@foreach (var item in items)
{
    <div id="item-row">  <!-- Same ID for every row! -->
        @item.Name
    </div>
}
```

**Problem:** OOB swaps target wrong element, JavaScript breaks.

```html
<!-- ✅ CORRECT: Unique IDs -->
@foreach (var item in items)
{
    <div id="item-row-@item.Id">
        @item.Name
    </div>
}
```

### ❌ Breaking HTMX with JavaScript Frameworks

```html
<!-- ANTI-PATTERN: React/Vue managing HTMX targets -->
<div id="app">
    <div id="htmx-target">  <!-- React will destroy this -->
        ...
    </div>
</div>
```

**Problem:** JavaScript framework re-renders DOM, losing HTMX targets and state.

```html
<!-- ✅ CORRECT: Separate concerns -->
<div id="spa-section">
    <!-- React/Vue managed content -->
</div>

<div id="htmx-section">
    <!-- HTMX managed content - framework doesn't touch this -->
</div>
```

### ❌ Form Inside Form

```html
<!-- ANTI-PATTERN: Nested forms are invalid HTML -->
<form id="outer-form">
    <form id="inner-form" hx-post="/submit">  <!-- Invalid! -->
        ...
    </form>
</form>
```

**Problem:** Browser behavior is undefined, HTMX may not work correctly.

```html
<!-- ✅ CORRECT: Use div with hx-post -->
<form id="outer-form">
    <div hx-post="/submit" hx-include="closest form">
        <!-- Acts like form without nesting -->
    </div>
</form>
```

---

## Testing

### ❌ Testing with Full Page Loads

```csharp
// ANTI-PATTERN: Not testing HTMX behavior
[Fact]
public async Task Grid_ReturnsView()
{
    var response = await _client.GetAsync("/inventory/grid");
    response.EnsureSuccessStatusCode();
}
```

**Problem:** Doesn't verify HTMX headers, OOB swaps, or triggers.

```csharp
// ✅ CORRECT: Use Swap.Testing
[Fact]
public async Task Grid_ReturnsPartialWithTrigger()
{
    var response = await _client.SwapGet("/inventory/grid");
    
    response
        .ShouldBePartial()  // Not full page
        .ShouldHaveTriggered(InventoryEvents.StateChanged)
        .ShouldHaveSwapped("#inventory-grid");
}
```

### ❌ Not Testing Event Handlers

```csharp
// ANTI-PATTERN: Handler never tested
public class StatsHandler : ISwapEventHandler<ItemCreatedEvent>
{
    public Task HandleAsync(ItemCreatedEvent e, SwapResponseBuilder b, CancellationToken ct)
    {
        b.AlsoUpdate("stats", "_Stats", ...);
        return Task.CompletedTask;
    }
}
```

**Problem:** Handler may have bugs, OOB swap may target wrong element.

```csharp
// ✅ CORRECT: Unit test handler
[Fact]
public async Task StatsHandler_AddsOobSwap()
{
    var handler = new StatsHandler(_statsService);
    var builder = new SwapResponseBuilder();
    
    await handler.HandleAsync(new ItemCreatedEvent(123), builder, default);
    
    builder.OobSwaps.Should().Contain(s => s.TargetId == "stats");
}
```

---

## Summary Checklist

Before deploying, verify:

- [ ] All navigation uses `hx-get` with `hx-push-url`, not `<a href>`
- [ ] All state updates happen in `hx-on::before-request`, not `after-request`
- [ ] All HTMX components include their state container via `hx-include`
- [ ] All event names use `EventKey` constants, not strings
- [ ] All element IDs are unique and use centralized constants
- [ ] All forms have proper loading indicators
- [ ] Search inputs have debouncing
- [ ] No sensitive data in event payloads
- [ ] Authorization is checked server-side, not trusted from hidden fields
- [ ] Tests verify HTMX behavior (partials, triggers, OOB swaps)

---

## Next Steps

- [Multi-Component Coordination](MultiComponentCoordination.md) - The right patterns
- [State Management](StateManagement.md) - Proper state handling
- [Events Guide](Events.md) - Type-safe event system

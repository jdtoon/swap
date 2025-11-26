# State Management Guide

Swap.Htmx applications manage state differently than traditional SPAs. This guide covers the patterns and best practices for handling state in HTMX-powered applications.

> **💡 New in v0.13:** For strongly-typed state management with automatic binding, see the **[SwapState Guide](SwapState.md)**.

---

## State Philosophy

In Swap.Htmx, state lives in the **HTML** rather than in JavaScript. This is a fundamental shift from SPA thinking:

| SPA Approach | Swap.Htmx Approach |
|--------------|-------------------|
| State in JavaScript memory | State in HTML (hidden fields, data attributes) |
| Components read from store | Components include state in requests |
| Client renders from state | Server renders from state |
| State synced via WebSocket | State synced via HTTP + events |

---

## Recommended: SwapState System

For most applications, we recommend using the **SwapState** system which provides:

- **Strongly-typed state classes** instead of loose hidden fields
- **Automatic model binding** via `[FromSwapState]`
- **Automatic OOB updates** via `.WithState()`
- **Change tracking** for debugging

```csharp
// Define state
public class InventoryState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
}

// Bind and update automatically
public IActionResult Grid([FromSwapState] InventoryState state)
{
    return this.SwapResponse()
        .WithView("_Grid", data)
        .WithState(state)  // Auto-updates hidden fields
        .Build();
}
```

```html
<swap-state state="Model.State" />
```

📖 **[Full SwapState Documentation →](SwapState.md)**

---

## Manual State Management

For simpler cases or legacy code, you can manage hidden fields manually.

### Where Should State Live?

Choose the right storage based on your requirements:

### Hidden Fields (Manual Approach)

**Best for:** Page-level state that multiple components share

```html
<div id="page-state" style="display: none;">
    <input type="hidden" name="tab" value="all" />
    <input type="hidden" name="page" value="1" />
    <input type="hidden" name="search" value="" />
    <input type="hidden" name="sortBy" value="name" />
</div>
```

**Pros:**
- Automatically included in requests via `hx-include`
- Survives DOM swaps (if you re-render the state container)
- Easy to debug (visible in DevTools)
- Works with standard model binding

**Cons:**
- Lost on full page reload (unless synced to URL)
- Requires manual updates

### URL Query Parameters

**Best for:** Shareable, bookmarkable state

```html
<a hx-get="/inventory?tab=active&page=2"
   hx-push-url="true"
   hx-target="#content">
    Active Items (Page 2)
</a>
```

**Pros:**
- Survives page refresh
- Shareable via link
- Browser back/forward works

**Cons:**
- Limited size (~2000 chars)
- Visible to users (security consideration)
- Requires URL parsing on server

### Data Attributes

**Best for:** Component-scoped state

```html
<div id="accordion-item-1" 
     data-expanded="false"
     hx-on:click="this.dataset.expanded = this.dataset.expanded === 'true' ? 'false' : 'true'">
    ...
</div>
```

**Pros:**
- Scoped to specific element
- Survives OOB swaps of other elements
- Good for UI-only state (collapse, hover, etc.)

**Cons:**
- Lost when element is swapped
- Not automatically included in requests

### Server Session

**Best for:** User-specific state that persists across pages

```csharp
// Store in session
HttpContext.Session.SetString("InventoryPreferences", JsonSerializer.Serialize(prefs));

// Retrieve
var prefs = JsonSerializer.Deserialize<UserPrefs>(
    HttpContext.Session.GetString("InventoryPreferences") ?? "{}"
);
```

**Pros:**
- Persists across page navigation
- Secure (not exposed to client)
- Can store complex objects

**Cons:**
- Server memory overhead
- Requires session infrastructure
- Doesn't survive server restart (without distributed session)

### TempData

**Best for:** One-time state (flash messages, redirect data)

```csharp
// Set before redirect
TempData["SuccessMessage"] = "Item created successfully!";

// Read on next request (auto-cleared)
var message = TempData["SuccessMessage"] as string;
```

---

## Building a State Container

### Basic Structure

```html
<!-- Views/Shared/_StateContainer.cshtml -->
@model PageState

<div id="@Model.ContainerId" style="display: none;" data-swap-state>
    @foreach (var field in Model.Fields)
    {
        <input type="hidden" 
               id="@field.Id" 
               name="@field.Name" 
               value="@field.Value" />
    }
</div>
```

### Typed State Model

```csharp
// Models/InventoryState.cs
public class InventoryState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
    
    // Computed properties
    public int Skip => (Page - 1) * PageSize;
}
```

### State Container Partial

```html
<!-- Views/Inventory/_InventoryState.cshtml -->
@model InventoryState

<div id="inventory-state" style="display: none;">
    <input type="hidden" id="inv-tab" name="tab" value="@Model.Tab" />
    <input type="hidden" id="inv-page" name="page" value="@Model.Page" />
    <input type="hidden" id="inv-pageSize" name="pageSize" value="@Model.PageSize" />
    <input type="hidden" id="inv-search" name="search" value="@Model.Search" />
    <input type="hidden" id="inv-sortBy" name="sortBy" value="@Model.SortBy" />
    <input type="hidden" id="inv-sortDesc" name="sortDesc" value="@Model.SortDesc.ToString().ToLower()" />
</div>
```

---

## Reading State on the Server

### ASP.NET Core Model Binding

State automatically binds to action parameters:

```csharp
// State binds from query string or form data
public IActionResult Grid(InventoryState state)
{
    // state.Tab, state.Page, etc. are populated
    var items = _service.GetItems(state);
    return SwapView("_Grid", items);
}
```

### Explicit Reading

```csharp
public IActionResult Grid()
{
    var state = new InventoryState
    {
        Tab = Request.Query["tab"].FirstOrDefault() ?? "all",
        Page = int.TryParse(Request.Query["page"], out var p) ? p : 1,
        Search = Request.Query["search"].FirstOrDefault()
    };
    
    // ...
}
```

---

## Writing State from the Client

### On User Interaction

Update hidden fields in `hx-on::before-request`:

```html
<button hx-get="/inventory/grid"
        hx-target="#inventory-grid"
        hx-include="#inventory-state"
        hx-on::before-request="
            document.getElementById('inv-tab').value = 'active';
            document.getElementById('inv-page').value = '1';
        ">
    Active Items
</button>
```

### Helper Function

For cleaner markup, use a helper:

```html
<script>
function setState(updates) {
    Object.entries(updates).forEach(([id, value]) => {
        const el = document.getElementById(id);
        if (el) el.value = value;
    });
}
</script>

<button hx-get="/inventory/grid"
        hx-on::before-request="setState({'inv-tab': 'active', 'inv-page': '1'})">
    Active Items
</button>
```

### From Server Response

The server can update state via OOB swap:

```csharp
return SwapResponse()
    .WithView("_Grid", model)
    .AlsoUpdate("inventory-state", "_InventoryState", newState)
    .Build();
```

---

## State Synchronization Patterns

### Pattern: URL ↔ Hidden Fields

Keep URL and hidden fields in sync for bookmarkable state:

```html
<!-- Update URL when state changes -->
<button hx-get="/inventory/grid"
        hx-push-url="/inventory?tab=active&page=1"
        hx-on::before-request="setState({'inv-tab': 'active', 'inv-page': '1'})">
    Active
</button>
```

```csharp
// On page load, initialize from URL
public IActionResult Index(InventoryState state)
{
    // state is populated from query string
    // Hidden fields will render with these values
    return View(new InventoryViewModel { State = state });
}
```

### Pattern: Server as Source of Truth

For complex state, let the server compute and return it:

```csharp
public IActionResult Grid(InventoryState state)
{
    // Server validates and potentially modifies state
    if (state.Page < 1) state.Page = 1;
    if (state.Page > totalPages) state.Page = totalPages;
    
    return SwapResponse()
        .WithView("_Grid", model)
        .AlsoUpdate("inventory-state", "_InventoryState", state) // Canonical state
        .Build();
}
```

### Pattern: Optimistic State

Update state immediately, reconcile if server disagrees:

```html
<button hx-post="/item/toggle"
        hx-on::before-request="this.dataset.pending = 'true'; optimisticToggle()"
        hx-on::after-request="this.dataset.pending = 'false'"
        hx-on::response-error="rollbackToggle()">
    Toggle
</button>
```

---

## Multiple State Containers

For complex pages with independent sections:

```html
<!-- Inventory section -->
<div id="inventory-state" style="display: none;">
    <input type="hidden" name="inv.tab" value="all" />
    <input type="hidden" name="inv.page" value="1" />
</div>

<!-- Orders section -->
<div id="orders-state" style="display: none;">
    <input type="hidden" name="ord.status" value="pending" />
    <input type="hidden" name="ord.page" value="1" />
</div>

<!-- Inventory grid includes only its state -->
<div hx-get="/inventory/grid" 
     hx-include="#inventory-state"
     hx-trigger="inventory.changed from:body">
</div>

<!-- Orders grid includes only its state -->
<div hx-get="/orders/grid" 
     hx-include="#orders-state"
     hx-trigger="orders.changed from:body">
</div>
```

---

## Debugging State

### Browser Console

```javascript
// Dump all state fields
function dumpState(containerId) {
    document.querySelectorAll(`#${containerId} input`).forEach(i => 
        console.log(`${i.name} = ${i.value}`)
    );
}

dumpState('inventory-state');
```

### Network Tab

Check that state is included in requests:
1. Open DevTools → Network
2. Trigger an action
3. Click the request
4. Check "Payload" or "Query String Parameters"

### Visual State Display (Development Only)

```html
@if (Environment.IsDevelopment())
{
    <details class="debug-state">
        <summary>State Debug</summary>
        <pre id="state-debug"></pre>
    </details>
    
    <script>
    function updateStateDebug() {
        const state = {};
        document.querySelectorAll('[data-swap-state] input').forEach(i => {
            state[i.name] = i.value;
        });
        document.getElementById('state-debug').textContent = 
            JSON.stringify(state, null, 2);
    }
    
    // Update on any htmx event
    document.body.addEventListener('htmx:afterSwap', updateStateDebug);
    updateStateDebug();
    </script>
}
```

---

## Common Mistakes

### ❌ Forgetting to Include State

```html
<!-- State won't be sent! -->
<button hx-get="/inventory/grid">Load</button>

<!-- ✅ Correct -->
<button hx-get="/inventory/grid" hx-include="#inventory-state">Load</button>
```

### ❌ Using after-request for State Updates

```html
<!-- Other components read stale state -->
<button hx-on::after-request="document.getElementById('tab').value = 'active'">

<!-- ✅ Correct: update before request -->
<button hx-on::before-request="document.getElementById('tab').value = 'active'">
```

### ❌ Not Re-rendering State Container

When the server modifies state, re-render the state container:

```csharp
// ❌ State container has stale values
return SwapResponse()
    .WithView("_Grid", model)
    .Build();

// ✅ State container updated with server's view
return SwapResponse()
    .WithView("_Grid", model)
    .AlsoUpdate("inventory-state", "_InventoryState", state)
    .Build();
```

### ❌ Conflicting IDs

```html
<!-- Two elements with same ID will cause problems -->
<input type="hidden" id="page" name="page" value="1" />
<input type="hidden" id="page" name="ordersPage" value="1" />

<!-- ✅ Use unique IDs -->
<input type="hidden" id="inv-page" name="page" value="1" />
<input type="hidden" id="ord-page" name="ordersPage" value="1" />
```

---

## Summary

| State Type | Storage | Include In Requests | Survives Refresh |
|------------|---------|---------------------|------------------|
| UI filters/pagination | Hidden fields | `hx-include` | ❌ (unless URL sync) |
| Bookmarkable state | URL params | Automatic | ✅ |
| Component state | Data attributes | Manual | ❌ |
| User preferences | Session | Server-side | ✅ |
| Flash messages | TempData | Server-side | One request |

**Golden Rule:** State in hidden fields, updated in `before-request`, included via `hx-include`, synchronized from server via OOB swaps.

---

## Next Steps

- [Multi-Component Coordination](MultiComponentCoordination.md) - Complete example with tabs, search, pagination
- [Events Guide](Events.md) - Triggering and listening to events
- [Out-of-Band Swaps](OutOfBandSwaps.md) - Updating multiple elements

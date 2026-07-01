# SwapState: First-Class State Management

SwapState provides a strongly-typed, automatic state management system for HTMX applications. It eliminates manual hidden field management while keeping state visible in the DOM.

---

## Quick Start

### 1. Define Your State

```csharp
using Swap.Htmx.State;

public class InventoryState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}
```

### 2. Add State to Your View

```html
@model InventoryViewModel

<!-- Place state container in your view -->
<swap-state state="Model.State" />

<!-- Reference it with hx-include -->
<button hx-get="/Inventory/Search"
        hx-target="#results"
        hx-include="#inventory-state">
    Search
</button>
```

The `<swap-state>` tag renders as:
```html
<div id="inventory-state" style="display: none;">
    <input type="hidden" name="Tab" value="all" />
    <input type="hidden" name="Page" value="1" />
    <input type="hidden" name="PageSize" value="10" />
    <input type="hidden" name="Search" value="" />
    <input type="hidden" name="SortBy" value="name" />
    <input type="hidden" name="SortDesc" value="false" />
</div>
```

### 3. Bind State in Controllers

```csharp
public IActionResult Search([FromSwapState] InventoryState state)
{
    var items = _service.GetItems(state.Tab, state.Page, state.Search);
    
    return this.SwapResponse()
        .WithView("_Results", items)
        .WithState(state)  // Auto-updates state container via OOB swap
        .Build();
}
```

---

## Core Concepts

### Container ID

Each `SwapState` class automatically generates a container ID from its class name:

| Class Name | Container ID |
|------------|--------------|
| `InventoryState` | `inventory-state` |
| `ProductSearchState` | `product-search-state` |
| `OrderFilterState` | `order-filter-state` |

You can override this:

```csharp
public class InventoryState : SwapState
{
    public override string ContainerId => "my-custom-id";
}
```

### Automatic OOB Updates

When you call `.WithState(state)`, the state container is automatically updated via an OOB (out-of-band) swap:

```html
<!-- This is appended to the response -->
<div id="inventory-state" hx-swap-oob="true" style="display: none;">
    <input type="hidden" name="Tab" value="electronics" />
    <input type="hidden" name="Page" value="1" />
    ...
</div>
```

This means state is always in sync without manual hidden field updates.

### When to Use OOB State Updates

**Pattern A: Swap Entire Content (no OOB needed)**

When the state container is INSIDE the swap target, `.WithState()` is optional - just include the state in your partial:

```html
<!-- _FilterContent.cshtml -->
<swap-state state="Model.State" />
<div class="product-grid">...</div>
```

```csharp
// State is inside the swap target - no OOB needed
return PartialView("_FilterContent", viewModel);
```

**Pattern B: OOB State Updates (use `.WithState()`)**

When the state container is OUTSIDE the swap target (e.g., swapping individual elements), use `.WithState()` to update it separately:

```html
<!-- State container outside cards -->
<swap-state state="Model.State" />

<!-- Individual cards get swapped -->
<div id="card-1">...</div>
<div id="card-2">...</div>
```

```csharp
// Swap just the card, update state separately via OOB
return this.SwapResponse()
    .WithView("_Card", cardModel)
    .WithState(state)  // OOB swaps the state container
    .Build();
```

### Change Tracking

SwapState tracks which properties have changed:

```csharp
var state = new InventoryState { Tab = "all" };
state.AcceptChanges();  // Mark current state as baseline

state.Tab = "electronics";
state.Page = 2;

Console.WriteLine(state.HasChanges);  // true
Console.WriteLine(state.ChangedProperties);  // ["Tab", "Page"]
```

---

## API Reference

### SwapState Base Class

| Property/Method | Description |
|-----------------|-------------|
| `ContainerId` | Auto-generated element ID (kebab-case of class name) |
| `Tampered` | `true` if a `Protected` value was missing, empty, or failed to decrypt during binding (fail-closed). Also set by `FromQueryString(...)` |
| `HasChanges` | True if any property modified since last `AcceptChanges()` |
| `ChangedProperties` | Set of modified property names |
| `AcceptChanges()` | Clears change tracking |
| `SuspendChangeTracking()` | Returns `IDisposable` for bulk updates without tracking |
| `GetStateValues()` | Returns dictionary of all state values |
| `SetStateValues(dict)` | Sets values from dictionary (used by model binder) |

### [FromSwapState] Attribute

Binds a `SwapState` parameter from form data:

```csharp
public IActionResult Action([FromSwapState] MyState state)
```

With prefix (for nested scenarios):

```csharp
public IActionResult Action([FromSwapState(Prefix = "filter")] MyState state)
// Binds from filter.Tab, filter.Page, etc.
```

### `<swap-state>` Tag Helper

```html
<!-- Basic usage -->
<swap-state state="Model.State" />

<!-- Custom ID -->
<swap-state state="Model.State" id="custom-id" />

<!-- With prefix -->
<swap-state state="Model.State" prefix="filter" />

<!-- Without container (just hidden inputs) -->
<swap-state state="Model.State" include-container="false" />
```

### Html.SwapStateContainer() Extension

Alternative to the tag helper:

```html
@Html.SwapStateContainer(Model.State)

@* With options *@
@Html.SwapStateContainer(Model.State, id: "custom-id", prefix: "filter")
```

### SwapResponseBuilder.WithState()

```csharp
return this.SwapResponse()
    .WithView("_Results", model)
    .WithState(state)  // Adds OOB swap for state container
    .Build();
```

---

## Common Patterns

### Tab Navigation

```csharp
public class TabState : SwapState
{
    public string Tab { get; set; } = "all";
}

[HttpPost]
public IActionResult ChangeTab([FromQuery] string tab, [FromSwapState] TabState state)
{
    // Use [FromQuery] to get tab from query string, not hidden fields
    var newState = new TabState { Tab = tab };
    
    return this.SwapResponse()
        .WithView("_TabContent", GetContent(tab))
        .WithState(newState)
        .Build();
}
```

```html
<swap-state state="Model.State" />

<div class="tabs">
    <button hx-post="/Items/ChangeTab?tab=all"
            hx-target="#content"
            hx-include="#tab-state"
            class="@(Model.State.Tab == "all" ? "active" : "")">
        All
    </button>
    <button hx-post="/Items/ChangeTab?tab=active"
            hx-target="#content"
            hx-include="#tab-state"
            class="@(Model.State.Tab == "active" ? "active" : "")">
        Active
    </button>
</div>
```

### Search + Pagination

```csharp
public class SearchState : SwapState
{
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public IActionResult Search([FromSwapState] SearchState state)
{
    state.Page = 1;  // Reset to first page on new search
    var results = _service.Search(state.Query, state.Page, state.PageSize);
    
    return this.SwapResponse()
        .WithView("_Results", results)
        .AlsoUpdate("pagination", "_Pagination", new { state.Page, TotalPages = results.TotalPages })
        .WithState(state)
        .Build();
}

public IActionResult Page([FromSwapState] SearchState state, int page)
{
    state.Page = page;
    var results = _service.Search(state.Query, state.Page, state.PageSize);
    
    return this.SwapResponse()
        .WithView("_Results", results)
        .AlsoUpdate("pagination", "_Pagination", new { state.Page, TotalPages = results.TotalPages })
        .WithState(state)
        .Build();
}
```

### Multiple State Containers

For complex pages with independent state:

```csharp
public class FilterState : SwapState
{
    public string Category { get; set; } = "all";
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

public class SortState : SwapState
{
    public string SortBy { get; set; } = "name";
    public bool Descending { get; set; } = false;
}
```

```html
<swap-state state="Model.FilterState" />
<swap-state state="Model.SortState" />

<!-- Filter controls include filter-state -->
<select hx-get="/Products/Filter"
        hx-target="#results"
        hx-include="#filter-state">
    ...
</select>

<!-- Sort controls include sort-state -->
<button hx-get="/Products/Sort?by=price"
        hx-target="#results"
        hx-include="#sort-state">
    Sort by Price
</button>
```

---

## Supported Property Types

SwapState supports these property types:

- `string`, `string?`
- `int`, `long`, `short`, `byte` (and nullable versions)
- `decimal`, `double`, `float` (and nullable versions)
- `bool`, `bool?`
- `DateTime`, `DateTimeOffset` (and nullable versions)
- `Guid`, `Guid?`
- Enum types

**Note:** Complex objects and collections are not supported. Keep state flat.

---

## Best Practices

### 1. URL Parameters Override Hidden Fields (FIRST Value Wins)

When using `hx-get` with URL parameters AND `hx-include` with hidden fields, **the URL parameter wins** because it comes first in the request.

```html
<!-- URL params come FIRST, hidden fields from hx-include come AFTER -->
<button hx-get="/Products/Filter?Category=electronics&Page=1"
        hx-target="#results"
        hx-include="#filter-state">
    Electronics
</button>
```

**Request URL becomes:** `/Products/Filter?Category=electronics&Page=1&Category=all&Page=3&...`

The model binder uses the **FIRST value** for each property. This is the key pattern for SwapState:

```csharp
// ✅ Correct - use hx-get with URL params to override specific fields
public IActionResult Filter([FromSwapState] FilterState state)
{
    // state.Category = "electronics" (from URL param - FIRST)
    // state.Page = 1 (from URL param - FIRST)
    // state.Search = "phone" (from hidden field - only source)
    
    var products = _service.Get(state);
    return PartialView("_Products", new ViewModel { State = state, Products = products });
}
```

### 2. Use [FromQuery] for Explicit Parameters

When you want to bind a parameter separately from the state:

```csharp
// Use [FromQuery] when the parameter shouldn't be part of SwapState
public IActionResult ChangeTab([FromQuery] string tab, [FromSwapState] State state)
```

### 3. Create New State for Major Changes

When state changes significantly, create a new instance:

```csharp
// ✅ Good - explicit new state
var newState = new SearchState 
{
    Query = query,
    Page = 1,  // Reset page
    PageSize = state.PageSize  // Keep page size
};

return this.SwapResponse()
    .WithView("_Results", results)
    .WithState(newState)
    .Build();
```

### 3. Keep State Flat

```csharp
// ❌ Avoid nested objects
public class BadState : SwapState
{
    public FilterOptions Filters { get; set; }  // Won't serialize properly
}

// ✅ Keep properties flat
public class GoodState : SwapState
{
    public string FilterCategory { get; set; }
    public decimal? FilterMinPrice { get; set; }
}
```

### 4. Use Meaningful Container IDs

The auto-generated ID is usually fine, but for clarity:

```csharp
public class ProductSearchState : SwapState
{
    // Container ID: "product-search-state" ✅ Clear and descriptive
}
```

---

## Migration from Manual Hidden Fields

### Before (Manual)

```html
<div id="state-container">
    <input type="hidden" name="Tab" value="@Model.Tab" />
    <input type="hidden" name="Page" value="@Model.Page" />
</div>
```

```csharp
public IActionResult Action(string tab, int page)
{
    // Manual parameter binding
}
```

### After (SwapState)

```html
<swap-state state="Model.State" />
```

```csharp
public IActionResult Action([FromSwapState] MyState state)
{
    return this.SwapResponse()
        .WithView("_View", model)
        .WithState(state)  // Auto-updates hidden fields
        .Build();
}
```

---

## Troubleshooting

### State Not Updating

**Problem:** Hidden fields don't update after response.

**Solution:** Make sure you're calling `.WithState(state)`:

```csharp
return this.SwapResponse()
    .WithView("_View", model)
    .WithState(state)  // Don't forget this!
    .Build();
```

### Wrong Value Bound

**Problem:** Parameter gets old value from hidden field instead of new value.

**Solution:** Use `hx-get` with URL parameters to override (FIRST value wins):

```html
<!-- URL param Category=electronics comes FIRST, wins over hidden field -->
<button hx-get="/Products/Filter?Category=electronics&Page=1"
        hx-target="#results"
        hx-include="#filter-state">
    Electronics
</button>
```

Or use `[FromQuery]` for separate binding:

```csharp
public IActionResult Action([FromQuery] string tab, [FromSwapState] State state)
```

### State Not Included in Request

**Problem:** State values are empty in controller.

**Solution:** Check `hx-include` targets the correct ID:

```html
<swap-state state="Model.State" />  <!-- ID: "my-state" -->

<button hx-get="/action"
        hx-include="#my-state">  <!-- Must match! -->
```

### Decimal/Float Formatting Issues

SwapState uses `InvariantCulture` for numeric serialization (always `.` as decimal separator). If you have culture-specific display, the state values will still work correctly.

---

## Secure State (Tamper-Proof)

By default, SwapState values are clear-text in hidden fields. This is fine for UI state (tab used, filters applied) but **unsafe** for business logic state (pricing, user IDs).

Enable **Tamper-Proof Mode** to encrypt state values using ASP.NET Core Data Protection.

### Encoding Protection

```csharp
public class PaymentState : SwapState
{
    // 1. Enable Protection
    public override bool Protected => true;
    
    // 2. (Optional) Sync encrypted state to URL
    public override bool UrlSync => true;

    // This property will be ENCRYPTED in the browser
    public decimal Price { get; set; }
    
    // This property will be ENCRYPTED
    public string OrderId { get; set; }
    
    // 3. Opt-out for user-editable fields (inputs)
    [SwapUnprotected]
    public string Comment { get; set; }
}
```

### Generating Links

When state is protected, you cannot concatenate strings manually. Use the helper:

```html
<!-- Correct: Uses encryption for protected properties -->
<a href="/Checkout?@Html.SwapStateQueryString(Model.State)">
    Refresh
</a>
```

### How It Works

1.  **Rendering**: The `<swap-state>` tag helper detects `Protected=true` and encrypts values using `IDataProtectionProvider`. This provider is **required** — `AddSwapHtmx()` registers it, and rendering protected state without a provider **throws** rather than emitting the values as plaintext.
2.  **Binding**: `[FromSwapState]` detects protected properties and automatically decrypts them.
3.  **Validation (fails closed)**: If a protected value is missing, empty, or fails to decrypt (tampered), binding **fails**: the binder adds a `ModelState` error and sets `SwapState.Tampered = true`. The value does **not** silently fall back to the type default (`0`, `Guid.Empty`) — check `ModelState.IsValid` (or `state.Tampered`) and reject the request. `SwapState.FromQueryString(...)` sets `Tampered` the same way for direct callers.
4.  **Scope**: Protection is scoped to the **State Container** and **Property Name**. You cannot copy an encrypted value from `OrderId` to `Price`.

### Handling Tampered State

```csharp
public IActionResult Checkout([FromSwapState] PaymentState state)
{
    if (!ModelState.IsValid || state.Tampered)
    {
        // A protected value was missing, cleared, or altered — reject it.
        return this.SwapResponse().WithErrorToast("Invalid request.").Build();
    }

    // state is trustworthy here
    return SwapView(state);
}
```

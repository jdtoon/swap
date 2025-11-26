# Multi-Component Coordination Guide

This guide covers one of the most powerful patterns in Swap.Htmx: coordinating multiple independent components on a page through events and shared state.

**Prerequisites:** You should be familiar with [Events](Events.md) and [Out-of-Band Swaps](OutOfBandSwaps.md) before reading this guide.

---

## The Challenge

Modern web pages often have multiple interactive components that need to work together:

- **Tabs** that filter data
- **Search** that filters data
- **Pagination** that pages through data
- **Data grid** that displays the filtered, paged results
- **Stats/summary** that reflects the current view

When a user clicks a tab, the search should reset, the pagination should go back to page 1, and the grid should reload with the new filter. This coordination is the "hard problem" that Swap.Htmx solves elegantly.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Page                                 │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              State Container (Hidden)                 │  │
│  │   tab="all"  |  page="1"  |  search=""               │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                 │
│         ┌─────────────────┼─────────────────┐              │
│         │                 │                 │              │
│         ▼                 ▼                 ▼              │
│  ┌────────────┐   ┌─────────────┐   ┌──────────────┐      │
│  │    Tabs    │   │   Search    │   │  Pagination  │      │
│  │            │   │             │   │              │      │
│  │ Writes to  │   │ Writes to   │   │ Writes to    │      │
│  │ state,     │   │ state,      │   │ state,       │      │
│  │ triggers   │   │ triggers    │   │ triggers     │      │
│  │ event      │   │ event       │   │ event        │      │
│  └─────┬──────┘   └──────┬──────┘   └──────┬───────┘      │
│        │                 │                  │              │
│        └─────────────────┼──────────────────┘              │
│                          │                                 │
│                          ▼                                 │
│              ┌───────────────────────┐                     │
│              │   StateChanged Event   │                    │
│              └───────────┬───────────┘                     │
│                          │                                 │
│                          ▼                                 │
│              ┌───────────────────────┐                     │
│              │      Data Grid        │                     │
│              │                       │                     │
│              │  Listens for event,   │                     │
│              │  reads state,         │                     │
│              │  reloads data         │                     │
│              └───────────────────────┘                     │
└─────────────────────────────────────────────────────────────┘
```

### Key Principles

1. **Shared State Container** - A hidden div with `<input type="hidden">` elements holds the current state
2. **Components Write State** - Each component updates the hidden fields it owns
3. **Events Signal Changes** - Components trigger events to notify others that state changed
4. **Components React** - Components listen for events and re-fetch data using the shared state
5. **`hx-include` Binds State** - Components include the state container in their requests

---

## Step-by-Step Implementation

Let's build a complete inventory management page with tabs, search, pagination, and a data grid.

### Step 1: Define Events

```csharp
// Events/InventoryEvents.cs
using Swap.Htmx.Events;

public static class InventoryEvents
{
    // UI state changed (tab, page, search, sort)
    public static readonly EventKey StateChanged = new("inventory.state.changed");
    
    // Data was modified (create, update, delete)
    public static readonly EventKey DataChanged = new("inventory.data.changed");
}
```

### Step 2: Create the State Container

The state container holds all shared UI state as hidden inputs:

```html
<!-- Views/Inventory/_StateContainer.cshtml -->
@model InventoryState

<div id="inventory-state" style="display: none;">
    <input type="hidden" id="currentTab" name="tab" value="@Model.Tab" />
    <input type="hidden" id="currentPage" name="page" value="@Model.Page" />
    <input type="hidden" id="searchTerm" name="search" value="@Model.Search" />
    <input type="hidden" id="sortBy" name="sortBy" value="@Model.SortBy" />
    <input type="hidden" id="sortDesc" name="sortDesc" value="@Model.SortDesc.ToString().ToLower()" />
</div>
```

```csharp
// Models/InventoryState.cs
public class InventoryState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}
```

### Step 3: Create the Main Page

```html
<!-- Views/Inventory/Index.cshtml -->
@model InventoryViewModel

<div class="inventory-page">
    <!-- State Container - ALL components include this -->
    <partial name="_StateContainer" model="Model.State" />
    
    <!-- Header with Search -->
    <div class="inventory-header">
        <h1>Inventory</h1>
        <div id="inventory-search">
            <partial name="_Search" model="Model.State" />
        </div>
    </div>
    
    <!-- Tabs -->
    <div id="inventory-tabs">
        <partial name="_Tabs" model="Model.State" />
    </div>
    
    <!-- Data Grid -->
    <div id="inventory-grid">
        <partial name="_Grid" model="Model" />
    </div>
    
    <!-- Pagination -->
    <div id="inventory-pagination">
        <partial name="_Pagination" model="Model" />
    </div>
</div>
```

### Step 4: Create the Tabs Component

The tabs component writes to the state container, then triggers an event:

```html
<!-- Views/Inventory/_Tabs.cshtml -->
@model InventoryState

<nav class="tabs">
    <button class="tab @(Model.Tab == "all" ? "active" : "")"
            hx-get="/inventory/grid"
            hx-target="#inventory-grid"
            hx-include="#inventory-state"
            hx-on::before-request="
                document.getElementById('currentTab').value = 'all';
                document.getElementById('currentPage').value = '1';
            ">
        All Items
    </button>
    
    <button class="tab @(Model.Tab == "active" ? "active" : "")"
            hx-get="/inventory/grid"
            hx-target="#inventory-grid"
            hx-include="#inventory-state"
            hx-on::before-request="
                document.getElementById('currentTab').value = 'active';
                document.getElementById('currentPage').value = '1';
            ">
        Active
    </button>
    
    <button class="tab @(Model.Tab == "archived" ? "active" : "")"
            hx-get="/inventory/grid"
            hx-target="#inventory-grid"
            hx-include="#inventory-state"
            hx-on::before-request="
                document.getElementById('currentTab').value = 'archived';
                document.getElementById('currentPage').value = '1';
            ">
        Archived
    </button>
</nav>
```

### Step 5: Create the Search Component

The search component listens for state changes AND triggers them:

```html
<!-- Views/Inventory/_Search.cshtml -->
@model InventoryState

<div class="search-container">
    <input type="search"
           id="search-input"
           placeholder="Search inventory..."
           value="@Model.Search"
           hx-get="/inventory/grid"
           hx-target="#inventory-grid"
           hx-include="#inventory-state"
           hx-trigger="input changed delay:300ms, @InventoryEvents.StateChanged.Name from:body"
           hx-on::before-request="
               document.getElementById('searchTerm').value = this.value;
               document.getElementById('currentPage').value = '1';
           "
           hx-indicator="#search-spinner" />
    
    <span id="search-spinner" class="htmx-indicator">
        <i class="spinner"></i>
    </span>
</div>
```

**Key Details:**
- `hx-trigger="input changed delay:300ms"` - Debounced typing
- `hx-trigger="..., inventory.state.changed from:body"` - Also reload when state changes elsewhere
- `hx-on::before-request` - Update state BEFORE the request (critical for timing!)

### Step 6: Create the Grid Component

The grid listens for state change events:

```html
<!-- Views/Inventory/_Grid.cshtml -->
@model InventoryViewModel

<table class="data-grid"
       hx-get="/inventory/grid"
       hx-target="#inventory-grid"
       hx-include="#inventory-state"
       hx-trigger="@InventoryEvents.DataChanged.Name from:body">
    <thead>
        <tr>
            <th hx-get="/inventory/grid"
                hx-target="#inventory-grid"
                hx-include="#inventory-state"
                hx-on::before-request="
                    var sortBy = document.getElementById('sortBy');
                    var sortDesc = document.getElementById('sortDesc');
                    if (sortBy.value === 'name') {
                        sortDesc.value = sortDesc.value === 'true' ? 'false' : 'true';
                    } else {
                        sortBy.value = 'name';
                        sortDesc.value = 'false';
                    }
                ">
                Name
                @if (Model.State.SortBy == "name")
                {
                    <span class="sort-indicator">@(Model.State.SortDesc ? "▼" : "▲")</span>
                }
            </th>
            <th>SKU</th>
            <th>Quantity</th>
            <th>Status</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Items)
        {
            <partial name="_GridRow" model="item" />
        }
    </tbody>
</table>

@if (!Model.Items.Any())
{
    <div class="empty-state">
        <p>No items found matching your criteria.</p>
    </div>
}
```

### Step 7: Create the Pagination Component

```html
<!-- Views/Inventory/_Pagination.cshtml -->
@model InventoryViewModel

<nav class="pagination"
     hx-trigger="@InventoryEvents.StateChanged.Name from:body"
     hx-get="/inventory/pagination"
     hx-target="#inventory-pagination"
     hx-include="#inventory-state">
    
    @if (Model.HasPreviousPage)
    {
        <button hx-get="/inventory/grid"
                hx-target="#inventory-grid"
                hx-include="#inventory-state"
                hx-on::before-request="
                    document.getElementById('currentPage').value = '@(Model.State.Page - 1)';
                ">
            Previous
        </button>
    }
    
    <span class="page-info">
        Page @Model.State.Page of @Model.TotalPages
    </span>
    
    @if (Model.HasNextPage)
    {
        <button hx-get="/inventory/grid"
                hx-target="#inventory-grid"
                hx-include="#inventory-state"
                hx-on::before-request="
                    document.getElementById('currentPage').value = '@(Model.State.Page + 1)';
                ">
            Next
        </button>
    }
</nav>
```

### Step 8: Create the Controller

```csharp
// Controllers/InventoryController.cs
public class InventoryController : SwapController
{
    private readonly IInventoryService _inventory;
    
    public InventoryController(IInventoryService inventory)
    {
        _inventory = inventory;
    }
    
    // Full page load
    public async Task<IActionResult> Index(InventoryState state)
    {
        var model = await BuildViewModel(state);
        return SwapView(model);
    }
    
    // Grid reload (partial)
    [HttpGet("inventory/grid")]
    public async Task<IActionResult> Grid(InventoryState state)
    {
        var model = await BuildViewModel(state);
        
        return SwapResponse()
            .WithView("_Grid", model)
            .AlsoUpdate("inventory-tabs", "_Tabs", model.State)
            .AlsoUpdate("inventory-pagination", "_Pagination", model)
            .AlsoUpdate("inventory-state", "_StateContainer", model.State)
            .WithTrigger(InventoryEvents.StateChanged)
            .Build();
    }
    
    // Create new item
    [HttpPost("inventory/create")]
    public async Task<IActionResult> Create(CreateInventoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return SwapResponse()
                .WithErrorToast("Please fix validation errors")
                .Build();
        }
        
        await _inventory.CreateAsync(request);
        
        return SwapResponse()
            .WithTrigger(InventoryEvents.DataChanged)  // Grid will reload
            .WithSuccessToast("Item created!")
            .Build();
    }
    
    // Delete item
    [HttpDelete("inventory/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _inventory.DeleteAsync(id);
        
        return SwapResponse()
            .WithTrigger(InventoryEvents.DataChanged)
            .WithSuccessToast("Item deleted")
            .Build();
    }
    
    private async Task<InventoryViewModel> BuildViewModel(InventoryState state)
    {
        var (items, totalCount) = await _inventory.GetPagedAsync(
            tab: state.Tab,
            search: state.Search,
            sortBy: state.SortBy,
            sortDesc: state.SortDesc,
            page: state.Page,
            pageSize: 20
        );
        
        return new InventoryViewModel
        {
            Items = items,
            State = state,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / 20.0),
            HasPreviousPage = state.Page > 1,
            HasNextPage = state.Page * 20 < totalCount
        };
    }
}
```

---

## Critical: Event Timing

> ⚠️ **This is the most common source of bugs when building multi-component UIs.**

### The Problem

When you update hidden fields in `hx-on::after-request`, other components that react to the `HX-Trigger` event will read **stale values**:

```html
<!-- ❌ WRONG: State updated AFTER request -->
<button hx-get="/inventory/grid"
        hx-on::after-request="document.getElementById('currentTab').value = 'active'">
    Active
</button>
```

**Timeline (WRONG):**
1. User clicks "Active" tab
2. HTTP request sent (tab value is still "all")
3. Server returns `HX-Trigger: inventory.state.changed`
4. Other components fire, read hidden field → get **"all"** (wrong!)
5. `after-request` fires → hidden field updated to "active" (too late)

### The Solution

Use `hx-on::before-request` to update state **before** the request:

```html
<!-- ✅ CORRECT: State updated BEFORE request -->
<button hx-get="/inventory/grid"
        hx-on::before-request="document.getElementById('currentTab').value = 'active'">
    Active
</button>
```

**Timeline (CORRECT):**
1. User clicks "Active" tab
2. `before-request` → hidden field updated to "active"
3. HTTP request sent (tab value is "active")
4. Server returns `HX-Trigger: inventory.state.changed`
5. Other components fire, read hidden field → get **"active"** (correct!)

### When to Use Each

| Event | Use For |
|-------|---------|
| `hx-on::before-request` | Updating state that other components will read |
| `hx-on::after-request` | UI cleanup, animations, focus management |
| `hx-on::after-swap` | Post-swap DOM manipulation |

---

## Event Flow Diagram

Here's what happens when a user clicks the "Active" tab:

```
User clicks "Active" tab
        │
        ▼
┌───────────────────────────────────────────┐
│ 1. hx-on::before-request fires            │
│    → currentTab = "active"                │
│    → currentPage = "1"                    │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│ 2. HTMX sends GET /inventory/grid         │
│    → Includes: tab=active, page=1         │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│ 3. Server returns:                        │
│    → Grid HTML (main response)            │
│    → Tabs HTML (OOB)                      │
│    → Pagination HTML (OOB)                │
│    → State container HTML (OOB)           │
│    → HX-Trigger: inventory.state.changed  │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│ 4. HTMX processes response:               │
│    → Swaps #inventory-grid (main)         │
│    → Swaps #inventory-tabs (OOB)          │
│    → Swaps #inventory-pagination (OOB)    │
│    → Swaps #inventory-state (OOB)         │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│ 5. inventory.state.changed event fires    │
│    → Search component sees event          │
│    → Reads state (tab=active, page=1)     │
│    → Updates its display if needed        │
└───────────────────────────────────────────┘
```

---

## State Management Strategies

Choose the right approach based on your needs:

| Approach | Best For | Example |
|----------|----------|---------|
| **Hidden Fields** | Page-level UI state | Tab, page, search, sort |
| **URL Query Params** | Shareable/bookmarkable state | Deep links to filtered views |
| **Data Attributes** | Component-scoped state | Collapse state, selection |
| **Server Session** | User-specific persistent state | Preferences, wizard progress |

### Syncing State to URL

For bookmarkable filtered views, sync state to the URL:

```html
<button hx-get="/inventory/grid"
        hx-push-url="/inventory?tab=active&page=1"
        hx-on::before-request="...">
    Active
</button>
```

Or use `hx-replace-url` to update without adding history:

```html
<button hx-get="/inventory/grid"
        hx-replace-url="/inventory?tab=active&page=1"
        ...>
```

---

## Common Patterns

### Pattern: Reset Page on Filter Change

When the user changes tabs or search, reset to page 1:

```javascript
// In before-request handler
document.getElementById('currentPage').value = '1';
```

### Pattern: Preserve Selection Across Reloads

Store selection in a hidden field, include it in requests:

```html
<input type="hidden" id="selectedIds" name="selectedIds" value="" />

<script>
function toggleSelection(id) {
    const input = document.getElementById('selectedIds');
    const ids = input.value ? input.value.split(',') : [];
    const index = ids.indexOf(id.toString());
    if (index > -1) {
        ids.splice(index, 1);
    } else {
        ids.push(id);
    }
    input.value = ids.join(',');
}
</script>
```

### Pattern: Debounced Search

Search triggers on typing with a delay:

```html
<input type="search"
       hx-get="/inventory/grid"
       hx-trigger="input changed delay:300ms"
       hx-target="#inventory-grid"
       hx-include="#inventory-state"
       hx-on::before-request="
           document.getElementById('searchTerm').value = this.value;
           document.getElementById('currentPage').value = '1';
       " />
```

### Pattern: Two-Way Event Listening

Components that both trigger AND listen to the same event:

```html
<!-- Search: triggers on typing, listens for external changes -->
<input hx-trigger="input changed delay:300ms, inventory.state.changed from:body"
       hx-get="/inventory/grid" ... />
```

---

## Debugging Tips

### 1. Inspect State Container

Open DevTools and check hidden field values:

```javascript
// In console
document.querySelectorAll('#inventory-state input').forEach(i => 
    console.log(i.name, '=', i.value)
);
```

### 2. Log Event Flow

Add temporary logging to track events:

```html
<div hx-on:inventory.state.changed="console.log('StateChanged received', event.detail)">
```

### 3. Check HX-Trigger Header

In Network tab, inspect response headers for `HX-Trigger`:

```
HX-Trigger: {"inventory.state.changed":null,"showToast":{"type":"success",...}}
```

### 4. Verify OOB Targets Exist

OOB swaps silently fail if target doesn't exist. Check for typos in IDs.

---

## Anti-Patterns to Avoid

### ❌ Updating State in after-request

```html
<!-- DON'T DO THIS -->
<button hx-on::after-request="document.getElementById('tab').value = 'active'">
```

### ❌ Circular Event Chains

```csharp
// DON'T DO THIS - infinite loop
events.When(InventoryEvents.StateChanged)
      .TriggerEvent(InventoryEvents.StateChanged);
```

### ❌ Including Too Much State

```html
<!-- DON'T: Including entire form when you only need filters -->
<div hx-include="form">

<!-- DO: Include only the state you need -->
<div hx-include="#inventory-state">
```

### ❌ Hardcoded Element IDs Everywhere

```csharp
// DON'T: Magic strings scattered in code
.AlsoUpdate("inventory-grid", "_Grid", model)

// DO: Centralized constants
public static class InventoryElements
{
    public const string Grid = "inventory-grid";
    public const string Tabs = "inventory-tabs";
}

.AlsoUpdate(InventoryElements.Grid, "_Grid", model)
```

---

## Summary

Multi-component coordination in Swap.Htmx follows a simple pattern:

1. **Define shared state** in hidden fields
2. **Components write** to state in `hx-on::before-request`
3. **Server triggers events** via `HX-Trigger`
4. **Components listen** for events via `hx-trigger="eventName from:body"`
5. **Components include state** via `hx-include="#state-container"`

This pattern scales to arbitrarily complex UIs while keeping each component independent and testable.

---

## Next Steps

- [Event Chains](EventChains.md) - Automate UI updates with declarative handlers
- [Out-of-Band Swaps](OutOfBandSwaps.md) - Deep dive into OOB mechanics
- [Realtime Updates](Realtime.md) - Push updates from server to clients

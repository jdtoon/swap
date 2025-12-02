# SwapStateDemo

Demonstrates state management patterns in Swap.Htmx using `<swap-hidden>` and `<swap-state>`.

## Features Demonstrated

### 1. `<swap-hidden>` Only
- Simple colocated state with controls
- Auto-formatting for dates, booleans, decimals
- Collection handling

### 2. `<swap-state>` Only  
- Full state container pattern
- OOB sync with server
- Change tracking

### 3. Both Together
- Complex pages with global state container + local hidden fields
- Best of both worlds approach

## Running

```bash
dotnet run
```

Then open http://localhost:5001

## Key Patterns

### swap-hidden: Simple Colocated State
```html
<div id="search-results">
    <swap-hidden name="page" value="@Model.Page" />
    <swap-hidden name="search" value="@Model.Search" />
    <button hx-get="/search" hx-include="closest div">Refresh</button>
</div>
```

### swap-state: Full State Container
```html
<swap-state state="Model.State" />
<!-- Renders as hidden div with all state properties -->
```

### Both Together: Global + Local State
```html
<!-- Global state container (synced via OOB) -->
<swap-state state="Model.GlobalState" />

<!-- Local colocated state per component -->
<div id="component-a">
    <swap-hidden name="localSort" value="@Model.SortA" />
    ...
</div>
```

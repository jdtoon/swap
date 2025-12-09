# SwapStateDemo

Demonstrates server-driven state management using `<swap-state>` and `[FromSwapState]`.

## What This Demo Shows

Three patterns for managing state with HTMX + Swap.Htmx:

### 1. Product Filter (Pagination + Filters)

A filterable, sortable, paginated product list where ALL state persists across interactions:
- Category filter (buttons)
- Search (text input with debounce)
- In stock filter (checkbox toggle)
- Price range (min/max)
- Sort by (dropdown)
- Sort direction (checkbox toggle)
- Pagination

**Pattern:** Swap entire content area. State container is inside swap target.

### 2. Multi-Step Wizard

A 5-step form wizard where data persists across all steps:
- Step 1: Personal info (first name, last name, email)
- Step 2: Address (street, city, postal code)
- Step 3: Preferences (contact method, newsletter, frequency)
- Step 4: Payment (method, card type)
- Step 5: Review all data

**Pattern:** Swap entire content area. Hidden fields + visible inputs merged.

### 3. OOB Dashboard (Out-of-Band Updates)

A dashboard with expandable cards and a detail panel:
- Click card header to expand/collapse (toggles that card only)
- Click "View Details" to show detail panel
- State tracks which cards are expanded, which is selected, click count

**Pattern:** Swap individual cards, use `.WithState(state)` for OOB update to state container.

## Running

```bash
cd demo/SwapStateDemo
dotnet run
```

Open http://localhost:5002

- Product Filter: http://localhost:5002/
- Wizard: http://localhost:5002/Home/Wizard
- OOB Dashboard: http://localhost:5002/Home/Dashboard

## Key Files

- `Models/Product.cs` - State classes (`ProductFilterState`, `WizardState`, `DashboardState`)
- `Controllers/HomeController.cs` - Filter, wizard, and dashboard actions
- `Views/Home/Index.cshtml` - Product filter main view
- `Views/Home/_FilterContent.cshtml` - Product filter partial (state + UI)
- `Views/Home/Wizard.cshtml` - Wizard main view
- `Views/Home/_WizardContent.cshtml` - Wizard partial (state + steps)
- `Views/Home/Dashboard.cshtml` - OOB dashboard main view
- `Views/Home/_DashboardCard.cshtml` - Individual card partial
- `Views/Home/_DashboardDetail.cshtml` - Detail panel partial

## Pattern 1: Swap Entire Content (Product Filter, Wizard)

State container is INSIDE the swap target. Everything updates together.

```html
<swap-state state="Model.State" />
<!-- All UI here -->

<button hx-get="/Filter" hx-target="#content" hx-include="#@state.ContainerId">
    Filter
</button>
```

Controller returns `PartialView()` - no OOB needed.

## Pattern 2: OOB State Updates (Dashboard)

State container is OUTSIDE the swap target. Use `.WithState()` to update it separately.

```html
<!-- State container outside cards -->
<swap-state state="Model.State" />

<!-- Individual cards get swapped -->
<div id="card-1">...</div>
<div id="card-2">...</div>
```

```csharp
// Controller uses SwapResponse with OOB state update
return this.SwapResponse()
    .WithView("_DashboardCard", model)  // Swaps #card-{id}
    .WithState(state)                    // OOB swaps #dashboard-state
    .Build();
```

## Checkbox Boolean Pattern

Unchecked checkboxes send nothing. To toggle a boolean properly:

```html
<!-- Pass the OPPOSITE value explicitly in the URL -->
<input type="checkbox" 
       @(state.InStockOnly ? "checked" : "")
       hx-get="/Filter?InStockOnly=@(!state.InStockOnly ? "true" : "false")"
       hx-include="#@state.ContainerId" />
```

## Wizard Include Pattern

For wizards, include BOTH the state container AND current step's form inputs:

```html
<button hx-post="/Wizard/Step?goToStep=2"
        hx-target="#wizard-content"
        hx-include="#@s.ContainerId, .step-content input, .step-content select">
    Next →
</button>
```

This sends:
- Hidden fields from `<swap-state>` (all wizard data)
- Visible inputs from current step (override hidden fields for current step)

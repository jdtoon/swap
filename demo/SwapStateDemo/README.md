# SwapStateDemo

Demonstrates server-driven state management using `<swap-state>` and `[FromSwapState]`.

## What This Demo Shows

Two patterns for managing state with HTMX + Swap.Htmx:

### 1. Product Filter (Pagination + Filters)

A filterable, sortable, paginated product list where ALL state persists across interactions:
- Category filter (buttons)
- Search (text input with debounce)
- In stock filter (checkbox toggle)
- Price range (min/max)
- Sort by (dropdown)
- Sort direction (checkbox toggle)
- Pagination

**Key insight:** Each interaction sends state via `hx-include`, server returns new HTML with updated state. No JavaScript state management.

### 2. Multi-Step Wizard

A 5-step form wizard where data persists across all steps:
- Step 1: Personal info (first name, last name, email)
- Step 2: Address (street, city, postal code)
- Step 3: Preferences (contact method, newsletter, frequency)
- Step 4: Payment (method, card type)
- Step 5: Review all data

**Key insight:** Hidden fields store ALL wizard data. Each step's form inputs override hidden fields for current step. Previous step data preserved in hidden fields.

## The Pattern

```html
<!-- 1. State container renders hidden fields -->
<swap-state state="Model.State" />

<!-- 2. Interactive elements include state container -->
<button hx-get="/Filter?Category=Electronics"
        hx-target="#content"
        hx-include="#product-filter-state">
    Electronics
</button>

<!-- 3. Form inputs with name attributes override hidden fields -->
<input type="text" 
       name="Search"
       value="@state.Search"
       hx-get="/Filter"
       hx-target="#content"
       hx-include="#product-filter-state"
       hx-trigger="keyup changed delay:300ms" />
```

```csharp
// 4. Controller binds state
public IActionResult Filter([FromSwapState] ProductFilterState state)
{
    // state is fully populated
    return PartialView("_ProductList", new ViewModel { State = state });
}
```

## Running

```bash
cd demo/SwapStateDemo
dotnet run
```

Open http://localhost:5002

- Product Filter: http://localhost:5002/
- Wizard: http://localhost:5002/Home/Wizard

## Key Files

- `Models/Product.cs` - State classes (`ProductFilterState`, `WizardState`)
- `Controllers/HomeController.cs` - Filter and wizard actions
- `Views/Home/Index.cshtml` - Product filter main view
- `Views/Home/_FilterContent.cshtml` - Product filter partial (state + UI)
- `Views/Home/Wizard.cshtml` - Wizard main view
- `Views/Home/_WizardContent.cshtml` - Wizard partial (state + steps)

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

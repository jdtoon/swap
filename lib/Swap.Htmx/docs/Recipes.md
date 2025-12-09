# Recipes

This guide contains patterns for common UI scenarios using Swap.Htmx primitives. Each recipe shows how to compose small partials with the UI orchestrator.

> **Philosophy:** Many small partials working together, coordinated by events, sharing state.

---

## Recipe Index

| Recipe | Complexity | Description |
|--------|------------|-------------|
| [Filterable List](#filterable-list) | Medium | Tabs + Search + Pagination + Grid |
| [Multi-Select Picker](#multi-select-picker) | Medium | Selecting multiple items with state sync |
| [Split-View Builder](#split-view-builder) | Medium | Config panel + Live preview |
| [Inline Edit](#inline-edit) | Low | Click-to-edit table cells |
| [Dependent Dropdowns](#dependent-dropdowns) | Low | Cascading selects |
| [Modal Forms](#modal-forms) | Medium | Create/Edit in dialog |
| [Infinite Scroll](#infinite-scroll) | Low | Load more on scroll |
| [Wizard / Multi-Step Form](#wizard-form) | High | Step-by-step with validation |

---

## Filterable List

A complete list view with tabs, search, pagination, and a data grid - all coordinated through state.

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Page                                                    │
│  ┌─────────────────────────────────────────────────┐   │
│  │ <swap-state state="Model.State" />              │   │
│  │ (hidden: Tab, Page, Search, SortBy, SortDesc)   │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ _Tabs partial (listens to state.changed)         │  │
│  │ [All] [Active] [Archived]                        │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ _Search partial                                   │  │
│  │ 🔍 [________________] (triggers state.changed)   │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ _Grid partial (listens to state.changed)         │  │
│  │ ┌─────────┬─────────┬─────────┐                  │  │
│  │ │ Name ▼  │ Status  │ Actions │                  │  │
│  │ ├─────────┼─────────┼─────────┤                  │  │
│  │ │ Item 1  │ Active  │ Edit    │                  │  │
│  │ │ Item 2  │ Active  │ Edit    │                  │  │
│  │ └─────────┴─────────┴─────────┘                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │ _Pagination partial (listens to state.changed)   │  │
│  │ < 1 2 [3] 4 5 >                                  │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### State Class

```csharp
public class ItemListState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
    
    // For URL bookmarking
    public override bool UrlSync => true;
}
```

### Events

```csharp
[SwapEventSource]
public static partial class ItemEvents
{
    public const string StateChanged = "items.state.changed";
    public const string Created = "items.created";
    public const string Updated = "items.updated";
    public const string Deleted = "items.deleted";
}
```

### Main View

```html
@model ItemListViewModel

<div id="item-list-page">
    <!-- State container (outside all swapped regions) -->
    <swap-state state="Model.State" />
    
    <!-- Tabs -->
    <div id="tabs"
         hx-get="/Items/Tabs"
         hx-trigger="@ItemEvents.StateChanged from:body"
         hx-include="#item-list-state">
        @await Html.PartialAsync("_Tabs", Model.State)
    </div>
    
    <!-- Search -->
    <div class="search-bar">
        <input type="text" 
               name="search"
               value="@Model.State.Search"
               placeholder="Search..."
               hx-get="/Items/Search"
               hx-trigger="keyup changed delay:300ms"
               hx-target="#grid"
               hx-include="#item-list-state" />
    </div>
    
    <!-- Grid -->
    <div id="grid"
         hx-get="/Items/Grid"
         hx-trigger="@ItemEvents.StateChanged from:body"
         hx-include="#item-list-state">
        @await Html.PartialAsync("_Grid", Model)
    </div>
    
    <!-- Pagination -->
    <div id="pagination"
         hx-get="/Items/Pagination"
         hx-trigger="@ItemEvents.StateChanged from:body"
         hx-include="#item-list-state">
        @await Html.PartialAsync("_Pagination", Model)
    </div>
</div>
```

### Controller

```csharp
public class ItemsController : Controller
{
    public IActionResult Index([FromSwapState] ItemListState? state)
    {
        state ??= new ItemListState();
        var data = _service.Query(state);
        return View(new ItemListViewModel { State = state, Items = data.Items, TotalCount = data.Total });
    }

    [HttpGet]
    public IActionResult Grid([FromSwapState] ItemListState state)
    {
        var data = _service.Query(state);
        return this.SwapResponse()
            .WithView("_Grid", new ItemListViewModel { State = state, Items = data.Items, TotalCount = data.Total })
            .WithState(state)
            .Build();
    }

    [HttpGet]
    public IActionResult Tabs([FromSwapState] ItemListState state)
    {
        return PartialView("_Tabs", state);
    }

    [HttpGet]
    public IActionResult Pagination([FromSwapState] ItemListState state)
    {
        var totalCount = _service.Count(state);
        return PartialView("_Pagination", new PaginationModel(state.Page, state.PageSize, totalCount));
    }

    [HttpGet]
    public IActionResult Search([FromSwapState] ItemListState state, string search)
    {
        state.Search = search;
        state.Page = 1;  // Reset to first page
        
        var data = _service.Query(state);
        return this.SwapResponse()
            .WithView("_Grid", new ItemListViewModel { State = state, Items = data.Items, TotalCount = data.Total })
            .WithState(state)
            .WithTrigger(ItemEvents.StateChanged)
            .Build();
    }

    [HttpPost]
    public IActionResult SetTab([FromSwapState] ItemListState state, string tab)
    {
        state.Tab = tab;
        state.Page = 1;
        
        return this.SwapResponse()
            .WithState(state)
            .WithTrigger(ItemEvents.StateChanged)
            .Build();
    }

    [HttpPost]
    public IActionResult SetPage([FromSwapState] ItemListState state, int page)
    {
        state.Page = page;
        
        return this.SwapResponse()
            .WithState(state)
            .WithTrigger(ItemEvents.StateChanged)
            .Build();
    }
}
```

### _Tabs Partial

```html
@model ItemListState

<nav class="tabs">
    @foreach (var tab in new[] { ("all", "All"), ("active", "Active"), ("archived", "Archived") })
    {
        <button hx-post="/Items/SetTab?tab=@tab.Item1"
                hx-include="#item-list-state"
                class="tab @(Model.Tab == tab.Item1 ? "active" : "")">
            @tab.Item2
        </button>
    }
</nav>
```

---

## Multi-Select Picker

Selecting multiple items from a list with state synchronization.

### The Pattern

```
┌────────────────────────────────────────┐
│ Select Rate Cards                      │
│ ┌────────────────────────────────────┐ │
│ │ ☑ Premium Package ($299)           │ │
│ │ ☐ Standard Package ($199)          │ │
│ │ ☑ Budget Package ($99)             │ │
│ │ ☐ Enterprise ($499)                │ │
│ └────────────────────────────────────┘ │
│                                        │
│ Selected: 2 items | Total: $398        │
│                                        │
│ [Clear] [Continue →]                   │
└────────────────────────────────────────┘
```

### State

```csharp
public class RateCardPickerState : SwapState
{
    // Comma-separated IDs
    public string SelectedIds { get; set; } = "";
    
    public List<int> GetSelectedIdList() => 
        string.IsNullOrEmpty(SelectedIds) 
            ? new List<int>() 
            : SelectedIds.Split(',').Select(int.Parse).ToList();
    
    public void SetSelectedIds(IEnumerable<int> ids) =>
        SelectedIds = string.Join(",", ids);
}
```

### View

```html
@model RateCardPickerViewModel

<div id="rate-card-picker">
    <swap-state state="Model.State" />
    
    <div id="rate-card-list">
        @foreach (var card in Model.RateCards)
        {
            var isSelected = Model.State.GetSelectedIdList().Contains(card.Id);
            <label class="picker-item @(isSelected ? "selected" : "")"
                   hx-post="/RateCards/Toggle?id=@card.Id"
                   hx-target="#rate-card-picker"
                   hx-include="#rate-card-picker-state">
                <input type="checkbox" @(isSelected ? "checked" : "") />
                <span class="name">@card.Name</span>
                <span class="price">@card.Price.ToString("C")</span>
            </label>
        }
    </div>
    
    <div id="selection-summary"
         hx-get="/RateCards/Summary"
         hx-trigger="selection.changed from:body"
         hx-include="#rate-card-picker-state">
        @await Html.PartialAsync("_SelectionSummary", Model)
    </div>
    
    <div class="actions">
        <button hx-post="/RateCards/ClearSelection"
                hx-include="#rate-card-picker-state">
            Clear
        </button>
        <button hx-post="/RateCards/Continue"
                hx-include="#rate-card-picker-state"
                @(Model.State.GetSelectedIdList().Count == 0 ? "disabled" : "")>
            Continue →
        </button>
    </div>
</div>
```

### Controller

```csharp
[HttpPost]
public IActionResult Toggle([FromSwapState] RateCardPickerState state, int id)
{
    var selected = state.GetSelectedIdList();
    
    if (selected.Contains(id))
        selected.Remove(id);
    else
        selected.Add(id);
    
    state.SetSelectedIds(selected);
    
    var cards = _service.GetAll();
    return this.SwapResponse()
        .WithView("_RateCardPicker", new RateCardPickerViewModel { State = state, RateCards = cards })
        .WithTrigger("selection.changed")
        .Build();
}

[HttpGet]
public IActionResult Summary([FromSwapState] RateCardPickerState state)
{
    var selectedCards = _service.GetByIds(state.GetSelectedIdList());
    return PartialView("_SelectionSummary", new SelectionSummaryModel
    {
        Count = selectedCards.Count,
        Total = selectedCards.Sum(c => c.Price)
    });
}
```

---

## Split-View Builder

A configuration panel alongside a live preview that updates as options change.

### The Pattern

```
┌─────────────────────────┬───────────────────────────────┐
│ Configuration           │ Preview                       │
│ ─────────────────────── │ ─────────────────────────────│
│                         │                               │
│ Currency: [USD ▼]       │   ┌───────────────────────┐   │
│                         │   │  QUOTE #QT-2025-001   │   │
│ Markup: [15%____]       │   │                       │   │
│                         │   │  Item 1    $1,149.99  │   │
│ ☑ Show images           │   │  Item 2      $299.99  │   │
│ ☑ Show descriptions     │   │  ─────────────────── │   │
│ ☐ Include tax           │   │  Subtotal  $1,449.98  │   │
│                         │   │  Markup      $217.50  │   │
│                         │   │  Total     $1,667.48  │   │
│                         │   └───────────────────────┘   │
│                         │                               │
└─────────────────────────┴───────────────────────────────┘
```

### State

```csharp
public class QuoteBuilderState : SwapState
{
    public string Currency { get; set; } = "USD";
    public decimal MarkupPercent { get; set; } = 15;
    public bool ShowImages { get; set; } = true;
    public bool ShowDescriptions { get; set; } = true;
    public bool IncludeTax { get; set; } = false;
}
```

### View

```html
@model QuoteBuilderViewModel

<div class="split-view">
    <swap-state state="Model.State" />
    
    <div class="config-panel">
        <h3>Configuration</h3>
        
        <div class="field">
            <label>Currency</label>
            <select name="currency"
                    hx-post="/Quote/UpdateConfig"
                    hx-target="#preview"
                    hx-include="#quote-builder-state">
                <option value="USD" selected="@(Model.State.Currency == "USD")">USD</option>
                <option value="EUR" selected="@(Model.State.Currency == "EUR")">EUR</option>
                <option value="GBP" selected="@(Model.State.Currency == "GBP")">GBP</option>
            </select>
        </div>
        
        <div class="field">
            <label>Markup %</label>
            <input type="number" 
                   name="markupPercent"
                   value="@Model.State.MarkupPercent"
                   hx-post="/Quote/UpdateConfig"
                   hx-target="#preview"
                   hx-trigger="change, keyup changed delay:500ms"
                   hx-include="#quote-builder-state" />
        </div>
        
        <div class="field">
            <label>
                <input type="checkbox" 
                       name="showImages"
                       value="true"
                       @(Model.State.ShowImages ? "checked" : "")
                       hx-post="/Quote/UpdateConfig"
                       hx-target="#preview"
                       hx-include="#quote-builder-state" />
                Show images
            </label>
        </div>
        
        <!-- More options... -->
    </div>
    
    <div id="preview"
         class="preview-panel"
         hx-get="/Quote/Preview"
         hx-trigger="load"
         hx-include="#quote-builder-state">
        <div class="loading">Loading preview...</div>
    </div>
</div>
```

### Controller

```csharp
[HttpPost]
public IActionResult UpdateConfig([FromSwapState] QuoteBuilderState state,
    string? currency, decimal? markupPercent, bool? showImages, bool? showDescriptions, bool? includeTax)
{
    // Update state with any provided values
    if (currency != null) state.Currency = currency;
    if (markupPercent != null) state.MarkupPercent = markupPercent.Value;
    if (showImages != null) state.ShowImages = showImages.Value;
    if (showDescriptions != null) state.ShowDescriptions = showDescriptions.Value;
    if (includeTax != null) state.IncludeTax = includeTax.Value;
    
    var preview = _service.GeneratePreview(state);
    
    return this.SwapResponse()
        .WithView("_QuotePreview", preview)
        .WithState(state)
        .Build();
}

[HttpGet]
public IActionResult Preview([FromSwapState] QuoteBuilderState state)
{
    var preview = _service.GeneratePreview(state);
    return PartialView("_QuotePreview", preview);
}
```

---

## Inline Edit

Click-to-edit pattern for table cells or fields.

### View

```html
<tr id="item-@Model.Id">
    <td>
        <span class="display-value"
              hx-get="/Items/@Model.Id/EditName"
              hx-target="closest td"
              hx-swap="innerHTML">
            @Model.Name
        </span>
    </td>
    <td>@Model.Category</td>
    <td>@Model.Price.ToString("C")</td>
</tr>
```

### Edit Partial (_EditName.cshtml)

```html
@model Item

<form hx-post="/Items/@Model.Id/UpdateName"
      hx-target="#item-@Model.Id"
      hx-swap="outerHTML">
    <input type="text" 
           name="name" 
           value="@Model.Name" 
           autofocus
           hx-on:keydown="if(event.key==='Escape') htmx.trigger(this.form, 'cancel')" />
    <button type="submit">✓</button>
    <button type="button"
            hx-get="/Items/@Model.Id/Row"
            hx-target="#item-@Model.Id"
            hx-swap="outerHTML">✗</button>
</form>
```

### Controller

```csharp
[HttpGet("{id}/EditName")]
public IActionResult EditName(int id)
{
    var item = _service.GetById(id);
    return PartialView("_EditName", item);
}

[HttpPost("{id}/UpdateName")]
public IActionResult UpdateName(int id, string name)
{
    var item = _service.UpdateName(id, name);
    return this.SwapResponse()
        .WithView("_ItemRow", item)
        .WithUpdatedToast("Item", name)
        .Build();
}

[HttpGet("{id}/Row")]
public IActionResult Row(int id)
{
    var item = _service.GetById(id);
    return PartialView("_ItemRow", item);
}
```

---

## Dependent Dropdowns

Cascading selects where the second dropdown depends on the first.

### View

```html
<div class="form-group">
    <label>Country</label>
    <select name="countryId"
            hx-get="/Location/Cities"
            hx-target="#city-select"
            hx-swap="innerHTML">
        <option value="">Select country...</option>
        @foreach (var country in Model.Countries)
        {
            <option value="@country.Id">@country.Name</option>
        }
    </select>
</div>

<div class="form-group">
    <label>City</label>
    <select name="cityId" id="city-select">
        <option value="">Select country first...</option>
    </select>
</div>
```

### Controller

```csharp
[HttpGet]
public IActionResult Cities(int countryId)
{
    if (countryId == 0)
    {
        return Content("<option value=''>Select country first...</option>", "text/html");
    }
    
    var cities = _service.GetCitiesByCountry(countryId);
    var options = cities.Select(c => $"<option value='{c.Id}'>{c.Name}</option>");
    
    return Content(
        "<option value=''>Select city...</option>" + string.Join("", options),
        "text/html"
    );
}
```

---

## Modal Forms

Create/Edit forms in a modal dialog.

### Trigger Button

```html
<button hx-get="/Items/CreateModal"
        hx-target="#modal-container"
        hx-swap="innerHTML">
    + New Item
</button>

<div id="modal-container"></div>
```

### Modal Partial (_CreateModal.cshtml)

```html
<div class="modal-backdrop" hx-on:click="htmx.trigger('#modal-close', 'click')"></div>
<div class="modal" role="dialog">
    <div class="modal-header">
        <h2>Create Item</h2>
        <button id="modal-close"
                hx-get="/Items/CloseModal"
                hx-target="#modal-container"
                hx-swap="innerHTML">×</button>
    </div>
    <div class="modal-body">
        <form hx-post="/Items/Create"
              hx-target="#item-list"
              hx-swap="afterbegin">
            <div class="form-group">
                <label asp-for="Name"></label>
                <input asp-for="Name" />
                <swap-validation for="Name" />
            </div>
            <!-- More fields... -->
            <button type="submit">Create</button>
        </form>
    </div>
</div>
```

### Controller

```csharp
[HttpGet]
public IActionResult CreateModal()
{
    return PartialView("_CreateModal", new CreateItemDto());
}

[HttpGet]
public IActionResult CloseModal()
{
    return Content("");  // Empty content clears the modal container
}

[HttpPost]
public IActionResult Create(CreateItemDto dto)
{
    if (!ModelState.IsValid)
    {
        return this.SwapValidationErrors(ModelState)
            .WithView("_CreateModal", dto)
            .Build();
    }
    
    var item = _service.Create(dto);
    
    return this.SwapResponse()
        .WithView("_ItemRow", item)
        .AlsoUpdate("modal-container", "_Empty", null)  // Close modal
        .WithCreatedToast("Item", item.Name)
        .Build();
}
```

---

## Infinite Scroll

Load more items as user scrolls.

### View

```html
<div id="item-list">
    @foreach (var item in Model.Items)
    {
        @await Html.PartialAsync("_ItemCard", item)
    }
    
    @if (Model.HasMore)
    {
        <div id="load-more"
             hx-get="/Items/LoadMore?page=@(Model.Page + 1)"
             hx-trigger="revealed"
             hx-swap="outerHTML">
            <div class="loading-spinner">Loading...</div>
        </div>
    }
</div>
```

### Controller

```csharp
[HttpGet]
public IActionResult LoadMore(int page)
{
    var (items, hasMore) = _service.GetPage(page, pageSize: 20);
    
    // Return items + next load trigger (or nothing if done)
    var html = new StringBuilder();
    
    foreach (var item in items)
    {
        html.Append(await RenderPartialAsync("_ItemCard", item));
    }
    
    if (hasMore)
    {
        html.Append($@"
            <div id='load-more'
                 hx-get='/Items/LoadMore?page={page + 1}'
                 hx-trigger='revealed'
                 hx-swap='outerHTML'>
                <div class='loading-spinner'>Loading...</div>
            </div>");
    }
    
    return Content(html.ToString(), "text/html");
}
```

---

## Wizard Form

Multi-step form with validation per step.

### State

```csharp
public class CheckoutWizardState : SwapState
{
    public int CurrentStep { get; set; } = 1;
    
    // Step 1: Shipping
    public string ShippingName { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public string ShippingCity { get; set; } = "";
    
    // Step 2: Payment
    public string CardNumber { get; set; } = "";
    public string CardExpiry { get; set; } = "";
    
    // Step 3: Review (no additional fields)
}
```

### View

```html
@model CheckoutWizardViewModel

<div id="checkout-wizard">
    <swap-state state="Model.State" />
    
    <!-- Progress indicator -->
    <div class="wizard-steps">
        <span class="step @(Model.State.CurrentStep >= 1 ? "completed" : "")">1. Shipping</span>
        <span class="step @(Model.State.CurrentStep >= 2 ? "completed" : "")">2. Payment</span>
        <span class="step @(Model.State.CurrentStep >= 3 ? "completed" : "")">3. Review</span>
    </div>
    
    <!-- Current step content -->
    <div id="wizard-content">
        @switch (Model.State.CurrentStep)
        {
            case 1:
                @await Html.PartialAsync("_ShippingStep", Model)
                break;
            case 2:
                @await Html.PartialAsync("_PaymentStep", Model)
                break;
            case 3:
                @await Html.PartialAsync("_ReviewStep", Model)
                break;
        }
    </div>
</div>
```

### Step Partial (_ShippingStep.cshtml)

```html
@model CheckoutWizardViewModel

<form hx-post="/Checkout/ValidateShipping"
      hx-target="#checkout-wizard"
      hx-swap="outerHTML">
    <h2>Shipping Information</h2>
    
    <div class="form-group">
        <label>Full Name</label>
        <input name="shippingName" value="@Model.State.ShippingName" />
        <swap-validation for="ShippingName" />
    </div>
    
    <div class="form-group">
        <label>Address</label>
        <input name="shippingAddress" value="@Model.State.ShippingAddress" />
        <swap-validation for="ShippingAddress" />
    </div>
    
    <div class="form-group">
        <label>City</label>
        <input name="shippingCity" value="@Model.State.ShippingCity" />
        <swap-validation for="ShippingCity" />
    </div>
    
    <div class="wizard-actions">
        <button type="submit">Continue to Payment →</button>
    </div>
</form>
```

### Controller

```csharp
[HttpPost]
public IActionResult ValidateShipping([FromSwapState] CheckoutWizardState state,
    string shippingName, string shippingAddress, string shippingCity)
{
    // Update state
    state.ShippingName = shippingName;
    state.ShippingAddress = shippingAddress;
    state.ShippingCity = shippingCity;
    
    // Validate
    if (string.IsNullOrEmpty(shippingName))
        ModelState.AddModelError("ShippingName", "Name is required");
    if (string.IsNullOrEmpty(shippingAddress))
        ModelState.AddModelError("ShippingAddress", "Address is required");
    if (string.IsNullOrEmpty(shippingCity))
        ModelState.AddModelError("ShippingCity", "City is required");
    
    if (!ModelState.IsValid)
    {
        return this.SwapValidationErrors(ModelState)
            .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state })
            .Build();
    }
    
    // Advance to next step
    state.CurrentStep = 2;
    
    return this.SwapResponse()
        .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state })
        .WithState(state)
        .Build();
}

[HttpPost]
public IActionResult GoBack([FromSwapState] CheckoutWizardState state)
{
    state.CurrentStep = Math.Max(1, state.CurrentStep - 1);
    
    return this.SwapResponse()
        .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state })
        .WithState(state)
        .Build();
}
```

---

## Key Principles

1. **State lives in `<swap-state>`** — One source of truth, outside swapped regions
2. **Small partials** — Each partial does one thing well
3. **Events coordinate** — Components listen to events, not each other
4. **`hx-include="#state-id"`** — HTMX requests include the state via standard hx-include
5. **Server renders** — No client-side templates, server decides what to show

---

## See Also

- [SwapState Guide](SwapState.md) — State management details
- [Multi-Component Coordination](MultiComponentCoordination.md) — Event-driven coordination
- [Form Validation](Validation.md) — Validation patterns

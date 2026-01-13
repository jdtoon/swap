Swap.Htmx — LLM Playbook (concise)

Use this as the “golden path” when generating Swap.Htmx code. Prefer correctness + consistency over cleverness.

============================================================
0) Non‑negotiables
============================================================

1. Controllers: inherit `SwapController`.
    - Realtime SSE endpoints: inherit `SwapRealtimeController` (from `Swap.Htmx.Realtime`).
2. Views: return `SwapView()` (not `View()`).
3. Multi-target updates: return `SwapResponse()` + `.AlsoUpdate(...)` + `.Build()`.
4. No magic strings:
     - Views: use generated `SwapViews.*` whenever possible.
     - Element IDs: use generated `SwapElements.*` wherever possible.
     - Events: use `EventKey` constants (not string literals).
5. State/paging/filtering: use `<swap-state>` + `[FromSwapState]`.
6. Decoupling: use event chains and/or `[SwapHandler]` handlers.


============================================================
1) Minimal Setup (Program.cs)
============================================================

Services:

```csharp
builder.Services.AddControllersWithViews();

builder.Services.AddSwapHtmx(events =>
{
        // 1) UI refresh chains (OOB swaps)
        // events.When(MyEvents.SomethingHappened)
        //       .RefreshPartial(SwapElements.SomeRegion, SwapViews.Foo._Bar, ctx => model)
        //       .SuccessToast("Done!");

        // 2) Optional: realtime broadcasting for that event
        // events.When(MyEvents.SomethingHappened)
        //       .Broadcast(); // SSE/WebSocket clients
})
.AddSseEventBridge(); // optional (only if you want realtime; requires Swap.Htmx.Realtime)
```

Pipeline:

```csharp
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();
app.UseSseEventBridge(); // optional, requires AddSseEventBridge() + Swap.Htmx.Realtime
app.MapControllers();
```


============================================================
2) Layout Requirements (_Layout.cshtml)
============================================================

Absolute minimum for HTMX + Swap client:

```html
<link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />
<script src="https://unpkg.com/htmx.org@2.0.8"></script>
<script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
```

If using SSE + htmx extension:

```html
<script src="https://unpkg.com/htmx-ext-sse@2.2.4/sse.js"></script>
```


============================================================
3) New Feature Recipe (CRUD screen)
============================================================

Do these in order.

Step A — Define keys (no strings)

```csharp
public static class DebtorEvents
{
        public static readonly EventKey Created = new("debtor.created");
        public static readonly EventKey Updated = new("debtor.updated");
        public static readonly EventKey Deleted = new("debtor.deleted");
}
```

Step B — Make stable element IDs

- Prefer generated `SwapElements.*`.
- If you must hand-write, do it once in a `*Elements` class.

Step C — Controller actions

```csharp
public sealed class DebtorsController : SwapController
{
        [HttpGet("/debtors")]
        public IActionResult Index([FromSwapState] DebtorsQuery query)
                => SwapView(model: /* view model */);

        [HttpPost("/debtors")]
        public async Task<IActionResult> Create(CreateDebtorInput input)
        {
                // persist...

                return SwapResponse()
                        .WithTrigger(DebtorEvents.Created, payload: new { /* optional */ })
                        .WithCreatedToast("Debtor")
                        .Build();
        }
}
```

Step D — SwapState (filter + pagination)

- Ensure the page renders `<swap-state>`.
- Ensure the listing request includes state: use `hx-include="swap-state"`.
- Bind state via `[FromSwapState]`.

Step E — Event chains (UI refresh)

```csharp
builder.Services.AddSwapHtmx(events =>
{
        events.When(DebtorEvents.Created)
                    .RefreshPartial(SwapElements.Debtors_List, SwapViews.Debtors._List, ctx => /* model */)
                    .RefreshPartial(SwapElements.Dashboard_Stats, SwapViews.Dashboard._Stats, ctx => /* model */)
                    .SuccessToast("Debtor created!");
});
```


============================================================
4) Event Handlers (server-side side-effects)
============================================================

Use a handler when you need cross-cutting behavior without bloating controllers.

```csharp
[SwapHandler(Priority = 100)]
public sealed class DebtorCreatedAuditHandler : ISwapEventHandler<DebtorCreatedEvent>
{
        public Task HandleAsync(DebtorCreatedEvent evt, SwapEventContext ctx, CancellationToken ct)
        {
                // log, enqueue work, etc.
                return Task.CompletedTask;
        }
}
```


============================================================
5) Realtime (SSE) Recipe
============================================================

Use realtime when other tabs/sessions should update automatically.

Step A — Enable services + middleware

- `AddSseEventBridge()` in DI
- `UseSseEventBridge()` in pipeline

Step B — Create an SSE endpoint (MVC or minimal API)

MVC:

```csharp
[HttpGet("/dashboard/stream")]
public IActionResult Stream()
{
        return ServerSentEvents(async (conn, ct) =>
        {
                conn.WithEvents(DashboardEvents.ActivityLogged.Name);
                await conn.KeepAlive(cancellationToken: ct);
        });
}

> `SwapRealtimeController` lives in the `Swap.Htmx.Realtime` package.
```

Step C — Connect from the view

```html
<div hx-ext="sse" sse-connect="/dashboard/stream">
    <div id="activity" sse-swap="activity.logged" hx-swap="afterbegin"></div>
</div>
```

Step D — Broadcast via event chains (recommended)

- Chain a domain/UI event to an internal SSE event key (`SseEvents.*`).
- Provide a chain config for the SSE event key that renders HTML.


============================================================
6) Debug Checklist (fast)
============================================================

- “Wrong HTML returned?”
    - Ensure action returns `SwapView()`.
- “Nothing updated?”
    - Confirm element IDs match (prefer `SwapElements.*`).
    - Confirm partial path matches (prefer `SwapViews.*`).
- “State not sticking?”
    - Ensure `<swap-state>` exists and requests `hx-include="swap-state"`.
- “Events not firing?”
    - Ensure `.WithTrigger(EventKey)` is present.
    - Ensure event chains are configured for that key.
- “Realtime not updating?”
    - Ensure `AddSseEventBridge()` + `UseSseEventBridge()` are present.
    - Ensure the page connects to the SSE endpoint and `sse-swap` matches the event name.

### Optional: See Generated Output

To inspect generated code (for debugging only):

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>obj\Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>

<!-- Exclude generated files from compilation (prevents duplicates) -->
<ItemGroup>
  <Compile Remove="obj\Generated\**\*.cs" />
</ItemGroup>
```

### 1. Event Source Generator (`[SwapEventSource]`)

Generates type-safe `EventKey` constants from string constants:

```csharp
// Input: Define events as string constants
[SwapEventSource]
public static partial class TaskEvents
{
    public const string TaskCompleted = "task.completed";
    public const string TaskCreated = "task.created";
    public const string UserLoggedIn = "user.loggedIn";
}

// Generated at build time:
public static partial class TaskEvents
{
    public static partial class Task
    {
        public static readonly EventKey Completed = new EventKey("task.completed");
        public static readonly EventKey Created = new EventKey("task.created");
    }
    public static partial class User
    {
        public static readonly EventKey LoggedIn = new EventKey("user.loggedIn");
    }
}

// Usage — type-safe, refactorable, no typos
return SwapEvent(TaskEvents.Task.Completed, payload).Build();
```

### 2. Auto-Generated View Constants (Zero Config)

The `AutoScanGenerator` creates `SwapViews` grouped by controller folder:

```csharp
// Auto-generated from Views/**/*.cshtml
// Grouped by controller folder, short names for all views
public static class SwapViews
{
    public static class Home
    {
        public const string Index = "Index";
    }
    public static class Products
    {
        public const string Index = "Index";
        public const string Details = "Details";
        public const string _Grid = "_Grid";           // Partials keep underscore
        public const string _Pagination = "_Pagination";
        public const string _ProductCard = "_ProductCard";
    }
    public static class Shared
    {
        public const string Error = "Error";
        public const string _Layout = "_Layout";
    }
}

// Usage — controller-relative, no magic strings
return SwapView(SwapViews.Products.Index, products);
builder.AlsoUpdate("product-grid", SwapViews.Products._Grid, products);
```

### 3. Modular Monolith Support

For modular apps, views are still grouped by controller folder within each module:

```
Modules/
  Billing/
    Views/
      Invoices/        → SwapViews.Invoices
        Index.cshtml
        _InvoiceList.cshtml
      Payments/        → SwapViews.Payments
        Index.cshtml
```

```csharp
// Each module project references Swap.Htmx
// Each module gets its own SwapViews class
builder.AlsoUpdate("invoice-list", SwapViews.Invoices._InvoiceList, model);
```

### 4. Auto-Generated Element ID Constants (Zero Config)

The generator scans `id="..."` attributes and filters out noise (numeric IDs, single chars, framework IDs):

```html
<!-- Views/Products/_Grid.cshtml -->
<div id="product-grid">...</div>
<div id="pagination">...</div>
<span id="product-count">...</span>
```

```csharp
// Auto-generated (filtered for meaningful IDs)
public static class SwapElements
{
    public const string ProductGrid = "product-grid";
    public const string Pagination = "pagination";
    public const string ProductCount = "product-count";
}

// Usage — no magic strings, IDE autocomplete
builder.AlsoUpdate(SwapElements.ProductGrid, SwapViews.Products._Grid, products);
builder.AlsoUpdate(SwapElements.ProductCount, SwapViews.Products._Count, count);
```

### Dynamic Element IDs

For elements with dynamic IDs (e.g., per-item cards), combine constants with interpolation:

```csharp
// Pattern: Static prefix + dynamic suffix
public static class ProductElements
{
    public const string CardPrefix = "product-card-";  // Used as prefix
    public static string Card(int id) => $"product-card-{id}";
}

// In handler — update specific product card
builder.AlsoUpdate(ProductElements.Card(productId), ProductViews._Card, product);

// In view
<div id="product-card-@Model.Id">...</div>
```

This maintains type safety for the prefix while allowing dynamic IDs.

### 5. Handler Validation Analyzer

Compile-time warnings for common mistakes:

| Code | Severity | Description |
|------|----------|-------------|
| `SWAP001` | Warning | Event triggered but no handler configured |
| `SWAP002` | Warning | Event key referenced but not defined |
| `SWAP003` | Warning | Circular event chain detected |
| `SWAP004` | Info | Duplicate handler for same event |

### Complete Example with Generators

```csharp
// Events/TaskEvents.cs
[SwapEventSource]
public static partial class TaskEvents
{
    public const string TaskCompleted = "task.completed";
}

// Handlers/TaskHandlers.cs
[SwapHandler]
public class StatsHandler : ISwapEventHandler<TaskPayload>
{
    public Task HandleAsync(TaskPayload payload, SwapResponseBuilder builder, CancellationToken ct)
    {
        // Use generated constants — controller-relative view names
        builder.AlsoUpdate(SwapElements.StatsPanel, SwapViews.Tasks._Stats, GetStats());
        return Task.CompletedTask;
    }
}

// Controllers/TasksController.cs
[HttpPost]
public IActionResult Complete(int id)
{
    var task = _service.Complete(id);
    
    // Type-safe event key
    return SwapEvent(TaskEvents.Task.Completed, new TaskPayload(task))
        .WithView(SwapViews.Tasks._Completed, task)
        .Build();
}
```

### Modular Template Setup

The `swap-modular` template has generators pre-configured:

```bash
dotnet new install Swap.Templates
dotnet new swap-modular -n MyApp
```

The template includes:
- Auto-scan enabled via `Swap.Htmx.targets` (zero config)
- Sample `[SwapEventSource]` usage in `Modules/Notes/Events/NotesEvents.cs`
- SwapViews grouped by controller folder within each module

---

## Pattern 1: SwapController

Base controller class that provides all Swap methods. **Every controller should inherit from this.**

```csharp
public class ProductsController : SwapController
{
    // Now you have access to:
    // - SwapView()
    // - SwapResponse()
    // - SwapEvent()
    // - ServerSentEvents()
}
```

Without inheritance (extension methods):

```csharp
public class ProductsController : Controller
{
    public IActionResult Index()
    {
        return this.SwapView(model);  // Extension method
    }
}
```

---

## Pattern 2: SwapView

`SwapView()` auto-detects request type. **Use this instead of `View()`.**

```csharp
public class ProductsController : SwapController
{
    public IActionResult Index()
    {
        return SwapView(model);
        // Browser navigation → View with layout
        // HTMX request → PartialView (no layout)
    }
}
```

---

## Pattern 3: SwapResponse (Multi-Target Updates)

Update multiple page elements in one response:

```csharp
[HttpPost]
public IActionResult AddToCart(int productId)
{
    _cart.Add(productId);
    
    return SwapResponse()
        .WithView("_ProductAdded", product)           // Main response → hx-target
        .AlsoUpdate("cart-count", "_Count", count)    // OOB swap
        .AlsoUpdate("cart-total", "_Total", total)    // OOB swap
        .WithSuccessToast("Added to cart!")           // Toast notification
        .WithTrigger("cartUpdated")                   // Client-side event
        .Build();
}
```

### AlsoUpdate Parameters

```csharp
.AlsoUpdate(
    "element-id",      // Target element ID (no #)
    "_PartialView",    // Partial view name
    model,             // Model for the view
    SwapMode.InnerHtml // Optional: swap strategy
)
```

### Swap Modes

| Mode | Effect |
|------|--------|
| `InnerHtml` | Replace inner content (default) |
| `OuterHtml` | Replace entire element |
| `BeforeEnd` | Append inside element |
| `AfterEnd` | Insert after element |
| `Delete` | Remove element |

---

## Pattern 4: SwapState

Server-side state via hidden HTML fields. No JavaScript state management. **Use when you need to preserve UI state across requests.**

### Define State

```csharp
public class FilterState : SwapState
{
    // OPTIONAL: Encrypt state in hidden fields and URLs
    public override bool Protected => true;
    
    // OPTIONAL: Sync state to URL query string
    public override bool UrlSync => true;

    // Use [SwapUnprotected] for fields user can edit (e.g. inputs)
    [SwapUnprotected]
    public string Category { get; set; } = "all";
    
    [SwapUnprotected]
    public int Page { get; set; } = 1;

    [SwapUnprotected]
    public string? Search { get; set; }
    
    public bool InStockOnly { get; set; } = false;
}
```

### Render State

```html
@model ProductViewModel

<!-- Renders hidden inputs in a container -->
<swap-state state="Model.State" />

<!-- Output: -->
<!-- <div id="filter-state" class="swap-state-container"> -->
<!--     <input type="hidden" name="Category" value="all" /> -->
<!--     <input type="hidden" name="Page" value="1" /> -->
<!--     ... -->
<!-- </div> -->
```

### Include State in Requests

```html
<button hx-get="/Products/Filter?Category=Electronics&Page=1"
        hx-target="#results"
        hx-include="#filter-state">
    Electronics
</button>
```

### Bind State in Controller

```csharp
[HttpGet]
public IActionResult Filter([FromSwapState] FilterState state)
{
    // state.Category = "Electronics" (from URL param)
    // state.Page = 1 (from URL param)
    // state.Search = "phone" (from hidden field)
    
    var products = _service.Get(state.Category, state.Search, state.Page);
    return PartialView("_ProductList", new ViewModel { State = state, Products = products });
}
```

### CRITICAL: URL Parameters Win (First Value)

When using `hx-get` with URL params AND `hx-include`:

```html
<button hx-get="/Filter?Category=Electronics&Page=1"
        hx-include="#filter-state">
```

Request becomes: `?Category=Electronics&Page=1&Category=all&Page=3&...`

**The model binder uses the FIRST value.** URL params come first, so they override hidden fields.

This is the key pattern:
- **Override specific fields:** Put them in URL parameters
- **Preserve other fields:** They come from hidden fields via hx-include
- **Clear a field:** Pass empty value in URL (`Search=`)

### Checkbox Pattern

Unchecked checkboxes send nothing. Toggle explicitly:

```html
<input type="checkbox" 
       @(state.InStockOnly ? "checked" : "")
       hx-get="/Filter?InStockOnly=@(!state.InStockOnly)"
       hx-include="#filter-state" />
```

### Generating Links for Secure State
If your state is `Protected` (encrypted), you cannot manually build query strings. Use the helper:

```html
<a href="/Products/Filter?@Html.SwapStateQueryString(Model.State)">
    Link
</a>
```

---

## Pattern 5: Event Handlers

Decouple UI updates from controllers. Controller fires event, handlers add OOB updates.

### Define Events (with Source Generator)

```csharp
// Use [SwapEventSource] for type-safe keys
[SwapEventSource]
public static partial class TaskEvents
{
    public const string TaskCreated = "task.created";
    public const string TaskCompleted = "task.completed";
    public const string TaskDeleted = "task.deleted";
}
// Generated: TaskEvents.Task.Created, TaskEvents.Task.Completed, TaskEvents.Task.Deleted
```

### Create Handlers

```csharp
[SwapHandler]
public class StatsHandler : ISwapEventHandler<TaskPayload>
{
    private readonly IStatsService _stats;
    public StatsHandler(IStatsService stats) => _stats = stats;
    
    public Task HandleAsync(TaskPayload payload, SwapResponseBuilder builder, CancellationToken ct)
    {
        var stats = _stats.GetStats();
        builder.AlsoUpdate("stats-panel", "_Stats", stats);
        return Task.CompletedTask;
    }
}

[SwapHandler]
public class ActivityHandler : ISwapEventHandler<TaskPayload>
{
    public Task HandleAsync(TaskPayload payload, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-feed", "_Activity", GetRecent());
        return Task.CompletedTask;
    }
}
```

### Fire Event from Controller

```csharp
[HttpPost]
public IActionResult Complete(int id)
{
    var task = _service.Complete(id);
    
    // Use generated type-safe key: TaskEvents.Task.Completed
    return SwapEvent(TaskEvents.Task.Completed, new TaskPayload(task.Id, task.Title))
        .WithView("_TaskCompleted", task)
        .WithSuccessToast("Task completed!")
        .Build();
}
// One event → both handlers run → one response with all OOB updates
```

---

## Pattern 6: SwapNavigation

SPA-style navigation without verbose HTMX. **Use `<swap-nav>` for internal links.**

```html
<!-- Instead of: -->
<a href="/products" hx-get="/products" hx-target="#main" hx-push-url="true">Products</a>

<!-- Use: -->
<swap-nav to="/products">Products</swap-nav>
```

Configure default target:

```csharp
builder.Services.AddSwapHtmx(options =>
{
    options.DefaultNavigationTarget = "#main-content";
});
```

---

## Complete Example: Product Filter

### State

```csharp
public class FilterState : SwapState
{
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}
```

### Controller

```csharp
public class ProductsController : SwapController
{
    public IActionResult Index()
    {
        var state = new FilterState();
        var products = GetProducts(state);
        return SwapView(new ProductViewModel { State = state, Products = products });
    }

    [HttpGet]
    public IActionResult Filter([FromSwapState] FilterState state)
    {
        var products = GetProducts(state);
        return PartialView("_FilterContent", new ProductViewModel { State = state, Products = products });
    }
}
```

### View (_FilterContent.cshtml)

```html
@model ProductViewModel
@{ var state = Model.State; }

<!-- State container -->
<swap-state state="Model.State" />

<!-- Category buttons -->
<div class="categories">
    @foreach (var cat in new[] { "all", "Electronics", "Furniture" })
    {
        <button class="@(state.Category == cat ? "active" : "")"
                hx-get="/Products/Filter?Category=@cat&Page=1"
                hx-target="#content"
                hx-include="#@state.ContainerId">
            @cat
        </button>
    }
</div>

<!-- Search -->
<input type="text" 
       name="Search"
       value="@state.Search"
       placeholder="Search..."
       hx-get="/Products/Filter?Page=1"
       hx-target="#content"
       hx-include="#@state.ContainerId"
       hx-trigger="keyup changed delay:300ms" />

<!-- Sort -->
<select hx-get="/Products/Filter?Page=1"
        hx-target="#content"
        hx-include="#@state.ContainerId">
    <option value="name" selected="@(state.SortBy == "name")">Name</option>
    <option value="price" selected="@(state.SortBy == "price")">Price</option>
</select>

<!-- Results -->
<div class="products">
    @foreach (var product in Model.Products)
    {
        <div class="product">@product.Name - @product.Price</div>
    }
</div>

<!-- Pagination -->
<div class="pagination">
    @for (int i = 1; i <= Model.TotalPages; i++)
    {
        <button hx-get="/Products/Filter?Page=@i"
                hx-target="#content"
                hx-include="#@state.ContainerId"
                class="@(state.Page == i ? "active" : "")">
            @i
        </button>
    }
</div>
```

---

## Complete Example: Multi-Step Wizard

### State

```csharp
public class WizardState : SwapState
{
    public int Step { get; set; } = 1;
    
    // Step 1
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    
    // Step 2
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    
    // Step 3
    public bool Newsletter { get; set; } = false;
}
```

### Controller

```csharp
public class WizardController : SwapController
{
    public IActionResult Index() => SwapView(new WizardViewModel());

    [HttpPost]
    public IActionResult Step([FromSwapState] WizardState state, int goToStep)
    {
        state.Step = goToStep;
        return PartialView("_WizardStep", new WizardViewModel { State = state });
    }
    
    [HttpPost]
    public IActionResult Submit([FromSwapState] WizardState state)
    {
        // All data available in state
        SaveRegistration(state);
        return PartialView("_Success");
    }
}
```

### View

```html
@model WizardViewModel
@{ var s = Model.State; }

<swap-state state="Model.State" />

@switch (s.Step)
{
    case 1:
        <input name="FirstName" value="@s.FirstName" />
        <input name="LastName" value="@s.LastName" />
        <input name="Email" value="@s.Email" />
        break;
    case 2:
        <input name="Street" value="@s.Street" />
        <input name="City" value="@s.City" />
        break;
    case 3:
        <p>Review: @s.FirstName @s.LastName, @s.Email</p>
        <p>Address: @s.Street, @s.City</p>
        break;
}

<div class="buttons">
    @if (s.Step > 1)
    {
        <button hx-post="/Wizard/Step?goToStep=@(s.Step - 1)"
                hx-target="#wizard"
                hx-include="#@s.ContainerId, form input">
            Back
        </button>
    }
    @if (s.Step < 3)
    {
        <button hx-post="/Wizard/Step?goToStep=@(s.Step + 1)"
                hx-target="#wizard"
                hx-include="#@s.ContainerId, form input">
            Next
        </button>
    }
    else
    {
        <button hx-post="/Wizard/Submit"
                hx-target="#wizard"
                hx-include="#@s.ContainerId">
            Submit
        </button>
    }
</div>
```

---

## API Quick Reference

### SwapController Methods

| Method | Purpose |
|--------|---------|
| `SwapView(model)` | Auto partial/full based on request |
| `SwapView("_View", model)` | Explicit view name |
| `SwapResponse()` | Start fluent builder |
| `SwapEvent(key, payload)` | Fire event, handlers add OOB |
| `ServerSentEvents(...)` | SSE endpoint |

### SwapResponse Builder

| Method | Purpose |
|--------|---------|
| `.WithView("_View", model)` | Main response content |
| `.AlsoUpdate("id", "_View", model)` | OOB swap |
| `.AlsoUpdate("id", "_View", model, SwapMode.X)` | OOB with swap mode |
| `.WithSuccessToast("msg")` | Success toast |
| `.WithErrorToast("msg")` | Error toast |
| `.WithTrigger("event")` | Client-side event |
| `.WithTrigger(EventKey)` | Type-safe event |
| `.WithRedirect("/path")` | HX-Redirect |
| `.WithState(state)` | OOB state container update |
| `.Build()` | Build IActionResult |

### SwapState

| Member | Purpose |
|--------|---------|
| `ContainerId` | Auto-generated element ID |
| `[FromSwapState]` | Bind state from request |
| `<swap-state state="x" />` | Render hidden fields |

---

## Rules

1. **Use `SwapView()` not `View()`** for HTMX-compatible responses
2. **URL params override hidden fields** — first value wins
3. **Always `hx-include` the state container** when using SwapState
4. **Debounce search inputs** — `hx-trigger="keyup changed delay:300ms"`
5. **Checkboxes need explicit toggle** — pass opposite value in URL
6. **Don't trust hidden fields** — validate server-side
7. **Keep state flat** — no nested objects in SwapState
8. **Use event handlers** for complex multi-component updates

---

## Don't Do This

```csharp
// WRONG: Returns full layout for HTMX requests
return View(model);

// WRONG: Redirect breaks HTMX
return Redirect("/products");

// WRONG: hx-include missing
<button hx-get="/filter">Filter</button>

// WRONG: Complex objects in state
public List<Product> Products { get; set; }  // Don't do this
```

---

## Do This

```csharp
// RIGHT: Auto-detects HTMX
return SwapView(model);

// RIGHT: Multi-target update
return SwapResponse()
    .WithView("_Main", main)
    .AlsoUpdate("count", "_Count", count)
    .Build();

// RIGHT: State with URL override
<button hx-get="/filter?Category=Electronics&Page=1"
        hx-include="#filter-state">

// RIGHT: Debounced search
<input hx-get="/search" 
       hx-trigger="keyup changed delay:300ms"
       hx-include="#state">

// RIGHT: Event-driven updates
return SwapEvent(Events.ItemAdded, payload)
    .WithView("_Item", item)
    .Build();
```

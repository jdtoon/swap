# Swap.Htmx# Swap.Htmx# Swap.Htmx



[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)

**Minimal HTMX framework for ASP.NET Core MVC** that provides automatic page/partial detection and a simple but powerful event system for decoupling domain logic from UI updates.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Core Philosophy



Swap.Htmx does three things exceptionally well:

**Minimal HTMX framework for ASP.NET Core MVC** that provides automatic page/partial detection and a simple but powerful event system for decoupling domain logic from UI updates.HTMX navigation framework for ASP.NET Core MVC applications. Provides a rigid, opinionated structure for building HTMX-powered applications with automatic page/partial detection, middleware enforcement, and extension methods.

1. **Automatic View Rendering**: `SwapView()` returns full page or partial based on request type

2. **Domain→UI Event Mapping**: Emit domain events, automatically trigger UI events via configured chains

3. **HTMX-Native**: No JavaScript required. HTMX handles event filtering and retargeting.

## Core Philosophy## Features

Everything else is just sensible defaults and extension methods. **Static typing enforced** - no raw strings for event names.



## Installation

Swap.Htmx does three things exceptionally well:- **SwapController Base Class**: Automatically handles page vs partial rendering based on HX-Request header

```bash

dotnet add package Swap.Htmx- **SwapView() Helper**: Single method that returns full page or partial view based on request type

```

1. **Automatic View Rendering**: `SwapView()` returns full page or partial based on request type- **Middleware Enforcement**: Catches and reports full page responses when partials are expected

## Quick Start

2. **Domain→UI Event Mapping**: Emit domain events (`product.created`), automatically trigger UI events (`ui.refreshList`)- **Extension Methods**: Fluent API for working with HTMX request/response headers

### 1. Register Services & Middleware

3. **HTMX-Native**: No JavaScript required. HTMX already handles event filtering and retargeting perfectly.- **Zero Configuration**: Works out of the box with sensible defaults

```csharp

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllersWithViews();Everything else is just sensible defaults and extension methods.## Installation



// Define app-specific event keys

public static class ProductEvents  

{## Installation```bash

    public static readonly EventKey Created = new("product.created");

    public static readonly EventKey Updated = new("product.updated");dotnet add package Swap.Htmx

    public static readonly EventKey Deleted = new("product.deleted");

}```bash```



public static class UiEventsdotnet add package Swap.Htmx

{

    public static readonly EventKey RefreshList = new("ui.refreshList");```## Quick Start

    public static readonly EventKey ShowToast = new("ui.toast.success");

}



builder.Services.AddSwapHtmx(events =>## Quick Start### 1. Register Services and Middleware

{

    // Map domain events to UI events (one-hop chains)

    events.Chain(ProductEvents.Created, UiEvents.RefreshList, UiEvents.ShowToast);

    events.Chain(ProductEvents.Deleted, UiEvents.RefreshList);### 1. Register Services & MiddlewareIn your `Program.cs`:

});



var app = builder.Build();

```csharp```csharp

app.UseRouting();

app.UseSwapHtmx();       // Event response middleware// Program.csvar builder = WebApplication.CreateBuilder(args);

app.UseSwapHtmxShell(); // Enforces partials for HTMX requests

app.MapControllers();var builder = WebApplication.CreateBuilder(args);



app.Run();// Add MVC and Swap.Htmx

```

builder.Services.AddControllersWithViews();builder.Services.AddControllersWithViews();

### 2. Use SwapController

builder.Services.AddSwapHtmx(events =>builder.Services.AddSwapHtmx(events =>

```csharp

using Swap.Htmx;{{

using Swap.Htmx.Events;

    // Map domain events to UI events (one-hop chains)    // Example chain: when a product is created, refresh any list listening

public class ProductsController : SwapController

{    events.Chain("product.created", "ui.refreshList", "ui.toast.success");    events.Chain(Swap.Htmx.Events.SwapEvents.Entity.Created("product"),

    private readonly AppDbContext _db;

    private readonly ISwapEventBus _events;    events.Chain("product.deleted", "ui.refreshList");                 Swap.Htmx.Events.SwapEvents.UI.RefreshList);



    public ProductsController(AppDbContext db, ISwapEventBus events)});});

    {

        _db = db;

        _events = events;

    }var app = builder.Build();var app = builder.Build();



    public async Task<IActionResult> Index()

    {

        var products = await _db.Products.ToListAsync();app.UseRouting();// Add middleware (after UseRouting, before MapControllers)

        return SwapView(products); // Auto-detects HTMX vs full page

    }app.UseSwapHtmx();  // Builds HX-Trigger headers from emitted eventsapp.UseRouting();



    [HttpPost]app.MapControllers();app.UseSwapHtmx();       // Event context + response header builder

    public async Task<IActionResult> Create(Product product)

    {app.Run();app.UseSwapHtmxShell(); // Enforces partial responses for HTMX requests

        _db.Products.Add(product);

        await _db.SaveChangesAsync();```



        // Emit domain event - framework handles UI updates via chainsapp.MapControllerRoute(

        await _events.EmitAsync(ProductEvents.Created, new { id = product.Id });

### 2. Use SwapController    name: "default",

        return SwapView("Details", product);

    }    pattern: "{controller=Home}/{action=Index}/{id?}");



    [HttpDelete]```csharp

    public async Task<IActionResult> Delete(int id)

    {public class ProductsController : SwapControllerapp.Run();

        var product = await _db.Products.FindAsync(id);

        _db.Products.Remove(product);{```

        await _db.SaveChangesAsync();

    private readonly AppDbContext _db;

        await _events.EmitAsync(ProductEvents.Deleted, new { id });

    private readonly ISwapEventBus _events;### 2. Update Controllers

        return Ok(); // HTMX handles DOM updates

    }

}

```    public ProductsController(AppDbContext db, ISwapEventBus events)Change your controllers to inherit from `SwapController` and use `SwapView()`:



### 3. Wire Up Views    {



```html        _db = db;```csharp

<!-- Views/Products/Index.cshtml -->

@model List<Product>        _events = events;using Microsoft.AspNetCore.Mvc;



<div id="product-list"     }using Swap.Htmx;

     hx-trigger="ui.refreshList from:body"

     hx-get="/Products/List"

     hx-swap="outerHTML">

    @foreach (var p in Model)    public async Task<IActionResult> Index()public class ArticlesController : SwapController

    {

        <div>@p.Name</div>    {{

    }

</div>        var products = await _db.Products.ToListAsync();    private readonly Swap.Htmx.Events.ISwapEventBus _events;



<button hx-post="/Products/Create"        return SwapView(products); // Auto-detects HTMX vs full page    private readonly AppDbContext _context;

        hx-target="#product-list"

        hx-swap="beforeend">    }

    Add Product

</button>    public ArticlesController(AppDbContext context, Swap.Htmx.Events.ISwapEventBus events)

```

    [HttpPost]    {

## Event System Deep Dive

    public async Task<IActionResult> Create(Product product)        _context = context;

### How It Works

    {        _events = events;

1. **Controller emits domain events**: `_events.Emit(ProductEvents.Created, payload)`

2. **Framework resolves chains**: `ProductEvents.Created` → `UiEvents.RefreshList`, `UiEvents.ShowToast`        _db.Products.Add(product);    }

3. **Middleware builds HX-Trigger header**: `{"product.created": {...}, "ui.refreshList": null, "ui.toast.success": null}`

4. **HTMX dispatches events client-side**: Components listening via `hx-trigger="ui.refreshList from:body"` react automatically        await _db.SaveChangesAsync();



### Why This Design?            public async Task<IActionResult> Index()



**No client-side subscription scanning**: HTMX already ignores unhandled events.        // Emit domain event - framework handles UI updates via chains    {



**No server-side filtering**: HTMX's event system handles this naturally.        await _events.EmitAsync("product.created", new { id = product.Id });        var articles = await _context.Articles.ToListAsync();



**Simple one-hop chains**: Predictable and sufficient. Emit domain events, map to UI events at startup, let HTMX handle the rest.                return SwapView(articles); // Automatically returns partial or full view



**Static typing enforced**: All event APIs require `EventKey` type. No raw strings allowed.        return SwapView("Details", product);    }



### Defining Your Own Events    }



Apps/modules define their own static event classes:    public async Task<IActionResult> Details(int id)



```csharp    [HttpDelete]    {

using Swap.Htmx.Events;

    public async Task<IActionResult> Delete(int id)        var article = await _context.Articles.FindAsync(id);

// In your app/module

public static class OrderEvents      {        if (article == null) return NotFound();

{

    public static readonly EventKey Created = new("order.created");        var product = await _db.Products.FindAsync(id);        

    public static readonly EventKey Completed = new("order.completed");

    public static readonly EventKey Cancelled = new("order.cancelled");        _db.Products.Remove(product);        return SwapView(article); // Works for any action

}

        await _db.SaveChangesAsync();    }

public static class UiEvents

{        

    public static class Orders

    {        await _events.EmitAsync("product.deleted", new { id });    [HttpPost]

        public static readonly EventKey Refresh = new("ui.orders.refresh");

    }            public async Task<IActionResult> Create(Article article)

    

    public static class Stats        return Ok(); // HTMX handles DOM updates via OOB or retarget    {

    {

        public static readonly EventKey Update = new("ui.stats.update");    }        if (!ModelState.IsValid)

    }

}}            return SwapView("Create", article);



// Register chains```

builder.Services.AddSwapHtmx(events =>

{        _context.Articles.Add(article);

    events.Chain(OrderEvents.Completed, 

                 UiEvents.Orders.Refresh,### 3. Wire Up Views        await _context.SaveChangesAsync();

                 UiEvents.Stats.Update);

});        await _events.EmitAsync(Swap.Htmx.Events.SwapEvents.Entity.Created("article"), new { id = article.Id });

```

```html        return SwapView("Details", article);

**Framework provides example patterns only** - `SwapEvents.Entity.Created("product")` is just a helper. Define your own events that match your domain.

<!-- Views/Products/Index.cshtml -->    }

### Event Payloads

@model List<Product>}

```csharp

// Emit with payload```

await _events.EmitAsync(ProductEvents.Created, new { 

    id = product.Id,<div id="product-list" 

    name = product.Name 

});     hx-trigger="ui.refreshList from:body"### 3. Update Views



// Chained events default to null payload     hx-get="/Products/List"

// If you need payload in chained events, emit them explicitly:

await _events.EmitAsync(ProductEvents.Created, new { id });     hx-swap="outerHTML">**Index.cshtml** (Shell with nested loading):

await _events.EmitAsync(UiEvents.ShowToast, new { message = "Product created!" });

```    @foreach (var p in Model)```html



## SwapView() Automatic Detection    {@model IEnumerable<Article>



```csharp        <div>@p.Name</div>

// Returns partial if HX-Request header present, full view otherwise

return SwapView(model);    }<div id="articles-container">



// Specify view name</div>    <h1>Articles</h1>

return SwapView("Details", model);

    

// Specify view name (null model)

return SwapView("EmptyState");<button hx-post="/Products/Create"     <!-- Nested loading: content loads via HTMX -->

```

        hx-target="#product-list"    <div hx-get="/Articles/List" 

**How it works:**

- Checks for `HX-Request` header        hx-swap="beforeend">         hx-trigger="load" 

- If present: Returns `PartialView()` (no layout)

- If absent: Returns `View()` (includes `_Layout.cshtml`)    Add Product         hx-target="#articles-list"



No configuration needed. Just works.</button>         hx-indicator="#loading">



## Extension Methods```        <div id="loading">Loading articles...</div>



### Request Detection    </div>



```csharp```javascript    

// Check if request is from HTMX

if (Request.IsHtmxRequest()) { }// wwwroot/js/swap-toast.js - Simple toast handler    <div id="articles-list"></div>



// Check if request is boosteddocument.body.addEventListener('ui.toast.success', (e) => {</div>

if (Request.IsHtmxBoosted()) { }

    showToast(e.detail.message || 'Success!', 'success');```

// Get HTMX headers

var currentUrl = Request.GetHtmxCurrentUrl();});

var target = Request.GetHtmxTarget();

var trigger = Request.GetHtmxTrigger();```**List.cshtml** (Partial content):

```

```html

### Response Headers

## Event System Deep Dive@model IEnumerable<Article>

```csharp

// Trigger client-side event (with EventKey)

Response.HxTrigger(UiEvents.RefreshList);

### How It Works@foreach (var article in Model)

// Trigger with payload

Response.HxTrigger(UiEvents.ShowToast, new { message = "Saved!", level = "success" });{



// Push URL to browser history1. **Controller emits domain events**: `_events.Emit("product.created", payload)`    <div class="article-card">

Response.HxPushUrl($"/products/{product.Id}");

2. **Framework resolves chains**: `product.created` → `ui.refreshList`, `ui.toast.success`        <h2>

// Client-side redirect

Response.HxRedirect("/login");3. **Middleware builds HX-Trigger header**: `{"product.created": {...}, "ui.refreshList": null, "ui.toast.success": null}`            <a href="/Articles/Details/@article.Id"



// Force full page refresh4. **HTMX dispatches events client-side**: Components listening via `hx-trigger="ui.refreshList from:body"` react automatically               hx-get="/Articles/Details/@article.Id"

Response.HxRefresh();

               hx-target="#main-content"

// Change target element

Response.HxRetarget("#notification-area");### Why This Design?               hx-push-url="true">



// Change swap strategy                @article.Title

Response.HxReswap("beforebegin");

```**No client-side subscription scanning**: Old approach scanned DOM for `hx-trigger` attributes and sent `X-Swap-Events` header. Premature optimization - HTMX already ignores unhandled events.            </a>



## Best Practices        </h2>



### 1. Define Static Event Keys**No server-side filtering**: Old approach filtered events against client subscriptions. Unnecessary - HTMX's event system handles this naturally.        <p>@article.Summary</p>



```csharp    </div>

// ✅ Good: Static, refactorable, type-safe

await _events.EmitAsync(ProductEvents.Created, payload);**Simple one-hop chains**: Old approach had three resolution modes (OneHop, Bidirectional, Transitive). Over-engineered. One-hop is predictable and sufficient.}



// ❌ Bad: String won't compile (APIs removed)```

await _events.EmitAsync("product.created", payload);

```**Result**: Emit domain events, map to UI events once at startup, let HTMX handle the rest. Clean separation of concerns.



### 2. Emit Domain Events, Not UI Events**_Layout.cshtml** (No hx-boost, explicit targets):



```csharp### Event Chains```html

// ✅ Good: Domain event

await _events.EmitAsync(ProductEvents.Created, new { id });<!DOCTYPE html>



// ❌ Bad: UI event (configure chain instead)```csharp<html lang="en">

await _events.EmitAsync(UiEvents.RefreshList);

```builder.Services.AddSwapHtmx(events =><head>



Configure chains at startup to map domain → UI. Keeps controllers clean.{    <meta charset="utf-8" />



### 3. Leverage HTMX's Native Features    // Single trigger → multiple UI events    <title>@ViewData["Title"] - My App</title>



```html    events.Chain("order.completed",    <script src="https://unpkg.com/htmx.org@1.9.10"></script>

<!-- ✅ Good: Let HTMX handle it -->

<div hx-trigger="ui.refreshList from:body"                 "ui.orders.refresh",</head>

     hx-get="/products/list"

     hx-swap="outerHTML">                 "ui.stats.update",<body>

</div>

                 "ui.toast.success");    <nav>

<!-- ❌ Bad: Manual JavaScript listeners -->

<script>        <a href="/" 

document.addEventListener('ui.refreshList', () => {

    fetch('/products/list').then(/*...*/);    // Multiple domain events → same UI refresh           hx-get="/" 

});

</script>    events.Chain("product.created", "ui.products.refresh");           hx-target="#main-content" 

```

    events.Chain("product.updated", "ui.products.refresh");           hx-push-url="true">Home</a>

### 4. Keep Chains Simple

    events.Chain("product.deleted", "ui.products.refresh");        <a href="/Articles" 

```csharp

// ✅ Good: One-hop, clear intent});           hx-get="/Articles" 

events.Chain(ProductEvents.Created, UiEvents.RefreshList, UiEvents.ShowToast);

```           hx-target="#main-content" 

// ❌ Bad: Confusing multi-domain chains

events.Chain(ProductEvents.Created, InventoryEvents.Check);           hx-push-url="true">Articles</a>

events.Chain(InventoryEvents.Check, WarehouseEvents.Notify);

```### Strongly-Typed Events (Recommended)    </nav>



## Module Event Sharing    



Modules can share events by referencing each other's assemblies:```csharp    <main id="main-content">



```csharp// Events/SwapEvents.cs (included in framework)        @RenderBody()

// In Products module

public static class ProductEvents  public static class SwapEvents    </main>

{

    public static readonly EventKey Created = new("products.created");{</body>

}

    public static class Entity</html>

// In Inventory module - reference Products assembly

using Products.Events;    {```



builder.Services.AddSwapHtmx(events =>        public static string Created(string entity) => $"{entity}.created";

{

    // Inventory module listens to product events        public static string Updated(string entity) => $"{entity}.updated";## How It Works

    events.Chain(ProductEvents.Created, InventoryEvents.CheckStock);

});        public static string Deleted(string entity) => $"{entity}.deleted";### Event System (Filtered + Chained)

```

    }

## Development Tools

Swap.Htmx includes a minimal event bus that:

In development mode, visit `/_swap/dev/events` to see:

- All configured event chains    public static class UI- Captures events you emit in controllers during a request

- Visual graph of chain relationships

- Event resolver (test what events fire for a given trigger)    {- Resolves configured chains (e.g., product.created → ui.refreshList)



```csharp        public const string RefreshList = "ui.refreshList";- Filters to active client subscriptions from `X-Swap-Events`

// Program.cs

if (app.Environment.IsDevelopment())        - Builds an `HX-Trigger` header automatically at response time

{

    app.MapSwapHtmxDevEndpoints();        public static class Toast

}

```        {Usage recap:



## Migration from v0.2.x            public const string Success = "ui.toast.success";- Register: `builder.Services.AddSwapHtmx(opts => opts.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList));`



**v0.3.0** simplified the event system and **enforced static typing**:            public const string Error = "ui.toast.error";- Middleware: `app.UseSwapHtmx();`



### Removed Features            public const string Warning = "ui.toast.warning";- Emit in controller: `await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id });`



- ❌ `ChainResolutionMode` enum (Bidirectional, Transitive)            public const string Info = "ui.toast.info";

- ❌ String-based `Emit(string)` and `Chain(string, ...)` APIs

- ❌ `SwapEventContextMiddleware` (client subscription filtering)        }Client side, ensure your components declare the events they listen to and send the `X-Swap-Events` header with active subscriptions (a small helper script can do this automatically; see docs). If the header is missing, no filtering occurs and all emitted+chained events are sent.

- ❌ `swap-events.js` (client-side subscription scanner)

    }

### What Changed

}#### Chain resolution modes

- **Chain resolution is now always one-hop**: Clearer, more predictable

- **No client/server filtering**: HTMX handles event filtering natively

- **EventKey required**: All event APIs now require static `EventKey` type

- **Simpler middleware stack**: Just `UseSwapHtmx()` for event system// UsageYou can control how chains expand at runtime via an enum on options. Default is safest.



### Migration Stepsevents.Chain(SwapEvents.Entity.Created("product"), 



1. **Define static EventKey fields**:             SwapEvents.UI.RefreshList,```csharp



```csharp             SwapEvents.UI.Toast.Success);builder.Services.AddSwapHtmx(opts =>

// Before (not allowed anymore)

opts.Chain("product.created", "ui.refreshList");```{

bus.Emit("product.created", payload);

    // Chains (prefer typed backend keys)

// After (required)

public static class ProductEvents  ### Event Payloads    opts.Chain(Swap.Htmx.Events.SwapEvents.Todo.Created,

{

    public static readonly EventKey Created = new("product.created");               Swap.Htmx.Events.SwapEvents.UI.Todo.RefreshList,

}

```csharp               Swap.Htmx.Events.SwapEvents.UI.Stats.Refresh);

public static class UiEvents

{// Emit with payload

    public static readonly EventKey RefreshList = new("ui.refreshList");

}await _events.EmitAsync("product.created", new {     // Resolution defaults to OneHop (immediate children only)



opts.Chain(ProductEvents.Created, UiEvents.RefreshList);    id = product.Id,     opts.ResolutionMode = Swap.Htmx.Events.ChainResolutionMode.OneHop; // default

bus.Emit(ProductEvents.Created, payload);

```    name = product.Name 



2. **Flatten multi-hop chains**:});    // Other strategies:



```csharp    // opts.ResolutionMode = ChainResolutionMode.Bidirectional; // reverse one-hop (Y emits X when X->Y configured)

// Before (relied on transitive resolution)

opts.Chain(EventA, EventB);// Chained events default to null payload    // opts.ResolutionMode = ChainResolutionMode.Transitive;    // expand breadth-first up to MaxTransitiveDepth

opts.Chain(EventB, EventC); // A would emit C transitively

// If you need payload in chained events, emit them explicitly:    // opts.MaxTransitiveDepth = 2; // depth limit when Transitive

// After (explicit one-hop)

opts.Chain(EventA, EventB, EventC); // Now explicitawait _events.EmitAsync("product.created", new { id });});

```

await _events.EmitAsync("ui.toast.success", new { message = "Product created!" });```

3. **Remove configuration**:

```

```csharp

// BeforeSemantics:

builder.Services.AddSwapHtmx(opts => {

    opts.ResolutionMode = ChainResolutionMode.Transitive;## SwapView() Automatic Detection- OneHop: A → {B,C} means emitting A includes B and C only.

    opts.MaxTransitiveDepth = 3;

});- Bidirectional: A → B means emitting A includes B, and emitting B also includes A (one hop each way).



// After (one-hop is the only mode)```csharp- Transitive: A → B → C expands along edges up to the configured depth (depth=1 equals OneHop).

builder.Services.AddSwapHtmx(opts => {

    // Just configure chains with EventKey// Returns partial if HX-Request header present, full view otherwise

});

```return SwapView(model);Guardrails: `Validate()` checks for invalid names and cycles at startup (Development), regardless of mode.



## Architecture



```// Specify view name### Strongly-typed backend event keys

framework/Swap.Htmx/

├── SwapController.cs               # Base controller with SwapView()return SwapView("Details", model);

├── SwapHtmxExtensions.cs           # Request/response extension methods

├── SwapHtmxServiceExtensions.cs   # DI registrationBackend code can (and should) use typed event keys via `EventKey` and the typed overloads for `Chain(...)`, `Emit(...)`, and `EmitAsync(...)`:

├── HxHeaders.cs                    # Header name constants

├── Events/// Specify view name (null model)

│   ├── EventKey.cs                 # Strongly-typed event key struct

│   ├── SwapEventBus.cs             # Core event bus (EventKey-only APIs)return SwapView("EmptyState");```csharp

│   ├── SwapEventBusOptions.cs      # Chain configuration (EventKey-only)

│   ├── SwapEvents.cs               # Example event patterns for apps```using Swap.Htmx.Events;

│   └── ISwapEventBus.cs            # Event bus interface

└── Middleware/

    ├── SwapEventResponseMiddleware.cs  # Builds HX-Trigger headers

    └── SwapHtmxShellMiddleware.cs      # Enforces partial responses**How it works:**builder.Services.AddSwapHtmx(events =>

```

- Checks for `HX-Request` header{

## License

- If present: Returns `PartialView()`    events.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);

MIT License - see [LICENSE](https://github.com/jdtoon/swap/blob/main/LICENSE) file for details.

- If absent: Returns `View()` (includes `_Layout.cshtml`)});

## Support



- 📖 [Documentation](https://github.com/jdtoon/swap/wiki)

- 🐛 [Issue Tracker](https://github.com/jdtoon/swap/issues)No configuration needed. Just works.public class ArticlesController : SwapController

- 💬 [Discussions](https://github.com/jdtoon/swap/discussions)

{

## Extension Methods    private readonly ISwapEventBus _events;

    public ArticlesController(ISwapEventBus events) => _events = events;

```csharp

// Request helpers    public async Task<IActionResult> Create(Article article)

if (Request.IsHtmxRequest()) { }    {

if (Request.IsHtmxBoosted()) { }        // ...save...

var currentUrl = Request.GetHtmxCurrentUrl();        await _events.EmitAsync(SwapEvents.Entity.Created("article"), new { id = article.Id });

var target = Request.GetHtmxTarget();        return SwapView("Details", article);

var trigger = Request.GetHtmxTrigger();    }

}

// Response headers (fluent)```

return View()

    .WithHxTrigger("eventName")There is also a Roslyn analyzer (`Swap.Htmx.Analyzers`) wired repo-wide that flags raw string usage in backend calls to `Chain`/`Emit` to help you avoid magic strings. Tests are suppressed by default. Note: HTML remains plain HTMX; you do not need to change attributes in markup.

    .WithHxTrigger("another", new { data = "payload" })

    .WithHxPushUrl("/new-url")

    .WithHxRetarget("#different-target")#### Client helper (swap-events.js)

    .WithHxReswap("innerHTML swap:500ms");

If you used the monolith template, include `/wwwroot/js/swap-events.js`. Otherwise, copy it from `templates/monolith/wwwroot/js/swap-events.js.template` into your app (rename to `swap-events.js`) and add it to your layout:

// Toast helpers

Response.ShowToast("Success!", ToastType.Success);```html

Response.ShowToast("Error occurred", ToastType.Error, ToastPosition.BottomRight);<script src="/js/swap-events.js"></script>

<script>

// Manual event triggering (if you need it)    // Opt-in to events you care about on this page

HtmxEvents.Trigger(Response, "custom.event", new { data = 123 });    SwapEvents.activate('ui.refreshList');

```    // Later: SwapEvents.deactivate('ui.refreshList');

    // Advanced: SwapEvents.set(['ui.refreshList', 'ui.showToast']);

## Development Tools    // Inspect current: SwapEvents.list()

    // Clear all: SwapEvents.clear()

In development mode, visit `/_swap/dev/events` to see:    // The script will automatically set X-Swap-Events on HTMX requests.

- All configured event chains    </script>

- Visual graph of chain relationships```

- Event resolver (test what events fire for a given trigger)



```csharp### Automatic Page/Partial Detection

// Program.cs

if (app.Environment.IsDevelopment())`SwapView()` checks for the `HX-Request` header:

{

    app.MapSwapHtmxDevEndpoints();- **HTMX Request** (header present): Returns `PartialView()` - no layout

}- **Normal Request** (initial load, refresh): Returns `View()` - with layout

```

This means:

## Architecture- First page load → Full page with layout

- Navigation via HTMX → Partial view swapped into target

```- Browser refresh → Full page with layout again

framework/Swap.Htmx/- No manual detection needed in every action

├── SwapController.cs           # Base controller with SwapView()

├── SwapHtmxExtensions.cs       # Request/response extension methods### Middleware Enforcement

├── SwapHtmxServiceExtensions.cs # DI registration

├── HxHeaders.cs                # Header name constants`SwapHtmxShellMiddleware` intercepts responses and checks:

├── HtmxEvents.cs               # Manual event helpers- If request has `HX-Request` header (excluding boosted requests)

├── SwapToastExtensions.cs      # Toast notification helpers- If response is full HTML page (contains `<!DOCTYPE>`, `<html>`, `<head>`)

├── Events/- If so, returns helpful error message instead

│   ├── SwapEventBus.cs         # Core event bus

│   ├── SwapEventBusOptions.cs  # Chain configurationThis catches common mistakes:

│   ├── SwapEvents.cs           # Strongly-typed event constants- Using `View()` instead of `SwapView()`

│   └── ISwapUiChainContributor.cs # Module chain registration- Error pages returning full layout for HTMX requests

├── Middleware/- Accidental layout rendering

│   ├── SwapEventResponseMiddleware.cs # Builds HX-Trigger headers

│   └── HxVaryHeaderMiddleware.cs      # Cache busting for HTMX### Navigation Pattern

├── Models/

│   ├── HxLocationOptions.cs    # HX-Location header model**Explicit HTMX Attributes** (not hx-boost):

│   └── HxReswapOptions.cs      # HX-Reswap header model```html

└── Dev/<a href="/Articles/Details/1"

    └── SwapDevEndpoints.cs     # Development UI   hx-get="/Articles/Details/1"

```   hx-target="#main-content"

   hx-push-url="true">

## Testing    View Article

</a>

```csharp```

// Use Swap.Testing package for HTMX-aware test clients

var client = new HtmxTestClient(factory);## Server-side events (registrars and transports)



var response = await client.GetAsync("/products");Swap.Htmx supports a simple in-process registrar for domain/server events out of the box, and optional distributed delivery via a transport abstraction. Pick one of the DI setups below.

Assert.True(response.IsHtmxRequest);

Assert.Contains("HX-Trigger", response.Headers);### Local/dev (in-memory registrar)

```

```csharp

## Best Practices// Keeps everything in-process for demos and local development

builder.Services.AddSwapServerEventChains();

### 1. Emit Domain Events, Not UI Events```



```csharp### Distributed (uses a transport + distributed registrar)

// ✅ Good: Domain event

await _events.EmitAsync("product.created", new { id });```csharp

// 1) Choose a transport

// ❌ Bad: UI eventbuilder.Services.AddInMemoryServerEventTransport(); // local multi-registrar simulation

await _events.EmitAsync("ui.refreshList");// or RabbitMQ

```builder.Services.AddRabbitMqServerEventTransport(opts =>

{

Configure chains at startup to map domain → UI. Keeps controllers clean.    opts.HostName = "localhost";          // or broker host

    opts.ExchangeName = "swap.events";    // topic exchange used for events

### 2. Use Strongly-Typed Event Names    // opts.UserName = "guest"; opts.Password = "guest"; // as needed

});

```csharp

// ✅ Good// 2) Use the distributed registrar (publishes/consumes via transport)

await _events.EmitAsync(SwapEvents.Entity.Created("product"), payload);builder.Services.AddSwapServerEventChainsDistributed();

```

// ❌ Bad (typos, no refactoring support)

await _events.EmitAsync("product.created", payload);Notes:

```- Your modules still only depend on `Swap.Modularity.Abstractions.IEventChainRegistrar` and call `Register`/`PublishAsync` the same way.

- The distributed registrar serializes payloads as JSON and includes a `ClrType` header to assist typed deserialization on the consumer side.

### 3. Leverage HTMX's Native Features- RabbitMQ transport uses a topic exchange (default `swap.events`) and a durable per-event-key queue by default.

- You can switch between in-memory and distributed by changing only DI wiring; no module code changes required.

```html

<!-- ✅ Good: Let HTMX handle it -->Why explicit over hx-boost:

<div hx-trigger="ui.refreshList from:body"- ✅ Clear intent - you see exactly what each link does

     hx-get="/products/list"- ✅ No conflicts between `hx-boost` and explicit `hx-target`

     hx-swap="outerHTML">- ✅ Per-link control over targets and behavior

</div>- ✅ Easier to debug and reason about



<!-- ❌ Bad: Manual JavaScript listeners -->### Nested Loading Pattern

<script>

document.addEventListener('ui.refreshList', () => {For progressive enhancement:

    fetch('/products/list').then(/*...*/);

});```html

</script><!-- Index page loads immediately -->

```<div id="articles-container">

    <h1>Articles</h1>

### 4. Keep Chains Simple    

    <!-- Content loads after page renders -->

```csharp    <div hx-get="/Articles/List" 

// ✅ Good: One-hop, clear intent         hx-trigger="load">

events.Chain("product.created", "ui.products.refresh", "ui.toast.success");        Loading...

    </div>

// ❌ Bad: Multi-hop, confusing</div>

events.Chain("product.created", "inventory.check");```

events.Chain("inventory.check", "warehouse.notify");

events.Chain("warehouse.notify", "ui.refresh"); // 3 hops!Benefits:

```- Fast initial page load

- Progressive content loading

## Modularity Integration- Better perceived performance

- Graceful degradation (content still loads without JS)

Swap.Htmx integrates with `Swap.Modularity` for modular event chain registration:

## Extension Methods

```csharp

public class ProductsModule : IModule### Request Detection

{

    public void ConfigureEventChains(IEventChainRegistrar chains)```csharp

    {// Check if request is from HTMX

        chains.Register(SwapEvents.Entity.Created("product"),if (Request.IsHtmxRequest())

                       SwapEvents.UI.RefreshList);{

    }    // Handle HTMX-specific logic

}}

```

// Check if request is boosted

See `Swap.Modularity` documentation for details.if (Request.IsHtmxBoosted())

{

## Migration from v0.2.x    // Handle boosted request

}

**v0.3.0** simplified the event system:

// Get HTMX headers

### Removed Featuresvar currentUrl = Request.GetHtmxCurrentUrl();

- ❌ `ChainResolutionMode` enum (Bidirectional, Transitive)var currentUri = Request.GetHtmxCurrentUrlUri();

- ❌ `ResolutionMode` property on `SwapEventBusOptions`var target = Request.GetHtmxTarget();

- ❌ `MaxTransitiveDepth` configurationvar trigger = Request.GetHtmxTrigger();

- ❌ `SwapEventContextMiddleware` (client subscription filtering)var triggerName = Request.GetHtmxTriggerName();

- ❌ `swap-events.js` (client-side subscription scanner)var promptValue = Request.GetHtmxPrompt();

- ❌ `X-Swap-Events` header (server-side filtering)

// Navigation via back/forward (history restore)

### What Changedif (Request.IsHtmxHistoryRestoreRequest())

- **Chain resolution is now always one-hop**: Clearer, more predictable{

- **No client/server filtering**: HTMX handles event filtering natively    // e.g., return cached fragment or fast path

- **Simpler middleware stack**: Just `UseSwapHtmx()` for event system}

```

### Migration Steps

### Response Headers

1. **Remove `ResolutionMode` configuration**:

```csharp```csharp

// Before// Trigger client-side event

builder.Services.AddSwapHtmx(opts => {Response.HxTrigger("itemCreated");

    opts.ResolutionMode = ChainResolutionMode.Transitive;

    opts.MaxTransitiveDepth = 3;// Trigger with JSON details

});Response.HxTriggerWithDetails("{\"showMessage\": {\"level\": \"info\"}}");



// After (one-hop is the only mode now)// Or typed trigger with details (auto-serializes to { "event": { ...details... } })

builder.Services.AddSwapHtmx(opts => {Response.HxTrigger("showMessage", new { level = "info", text = "Saved" });

    // Just configure chains

});// Push URL to browser history

```Response.HxPushUrl($"/articles/{article.Id}");



2. **Flatten multi-hop chains**:// Replace URL in history

```csharpResponse.HxReplaceUrl($"/articles/{article.Id}");

// Before (relied on transitive resolution)

opts.Chain("A", "B");// Client-side redirect

opts.Chain("B", "C"); // A would emit C via transitiveResponse.HxRedirect("/login");



// After (explicit one-hop)// Force full page refresh

opts.Chain("A", "B", "C"); // Now explicitResponse.HxRefresh();

```

// Change target element

3. **Remove `swap-events.js`**, replace with `swap-toast.js` if using toasts:Response.HxRetarget("#notification-area");

```html

<!-- Before -->// Change swap strategy

<script src="~/js/swap-events.js"></script>Response.HxReswap("beforebegin");



<!-- After -->// Or typed reswap options

<script src="~/js/swap-toast.js"></script>Response.HxReswap(new Swap.Htmx.Models.HxReswapOptions

```{

    Style = Swap.Htmx.Models.HxSwapStyle.innerHTML,

## License    Transition = true,

    SwapDelay = 50,

MIT License - see LICENSE file for details.    SettleDelay = 50

});

## Contributing

// HX-Location: string or typed options object

Issues and PRs welcome on GitHub: https://github.com/jdtoon/swapResponse.HxLocation("/inbox");

Response.HxLocation(new Swap.Htmx.Models.HxLocationOptions
{
    Path = "/inbox",
    Target = "#main",
    Select = "#main",
}.WithSwap(new Swap.Htmx.Models.HxReswapOptions { Style = Swap.Htmx.Models.HxSwapStyle.outerHTML }));

// Stop polling this endpoint
Response.HxStopPolling(); // returns HTTP 286

// If content differs for HTMX vs non-HTMX, set Vary header
Response.EnsureVaryHxRequest();

// Fire events at specific lifecycle moments
Response.HxTriggerAfterSwap("listRefreshed");
Response.HxTriggerAfterSwapWithDetails("{\"listRefreshed\": { \"count\": 42 }}");
Response.HxTriggerAfterSwap("listRefreshed", new { count = 42 });

Response.HxTriggerAfterSettle("toast");
Response.HxTriggerAfterSettleWithDetails("{\"toast\": { \"level\": \"success\" }}");
Response.HxTriggerAfterSettle("toast", new { level = "success" });
```

## Architecture Benefits

### Rigid Framework (Good Thing!)

Unlike copying code, using this package as a framework provides:

1. **Consistency**: All controllers work the same way
2. **Upgrades**: Bug fixes and improvements via package updates
3. **Best Practices**: Enforces correct HTMX patterns
4. **Team Alignment**: Everyone uses same approach
5. **Less Boilerplate**: No repeated HX-Request checks

### When to Use Embedded Code

Use `--embed-htmx` flag in Swap CLI if you need:
- Custom behavior beyond framework capabilities
- Full control over every detail
- No package dependencies
- Maximum flexibility

But for most apps, the package is better:
- Less code to maintain
- Automatic improvements
- Proven patterns
- Easier onboarding

## Common Patterns

### Form Submission

```csharp
[HttpPost]
public async Task<IActionResult> Create(Article article)
{
    if (!ModelState.IsValid)
    {
        return SwapView("Create", article); // Re-render form with errors
    }

    _context.Articles.Add(article);
    await _context.SaveChangesAsync();

    Response.HxTrigger("articleCreated"); // Notify client
    Response.HxPushUrl($"/articles/{article.Id}");
    
    return SwapView("Details", article);
}
```

### Delete with Confirmation

```csharp
[HttpDelete]
public async Task<IActionResult> Delete(int id)
{
    var article = await _context.Articles.FindAsync(id);
    if (article == null) return NotFound();

    _context.Articles.Remove(article);
    await _context.SaveChangesAsync();

    Response.HxTrigger("articleDeleted");
    Response.HxRedirect("/articles"); // Redirect after delete
    
    return Ok();
}
```

### Inline Editing

```csharp
public async Task<IActionResult> EditInline(int id)
{
    var article = await _context.Articles.FindAsync(id);
    return SwapView("_EditForm", article); // Return form partial
}

[HttpPut]
public async Task<IActionResult> UpdateInline(int id, Article article)
{
    if (!ModelState.IsValid)
        return SwapView("_EditForm", article);

    _context.Update(article);
    await _context.SaveChangesAsync();

    Response.HxTrigger("articleUpdated");
    return SwapView("_ArticleCard", article); // Return updated card
}
```

## Debugging

### Middleware Error

If you see "HTMX Shell Middleware Error", check:

1. Controller inherits from `SwapController`
2. Using `SwapView()` instead of `View()`
3. Partial views don't specify layout
4. Error handling returns partials for HTMX requests

### Full Page Returned

If HTMX requests get full pages:

1. Check `HX-Request` header in browser DevTools
2. Verify middleware is registered: `app.UseSwapHtmxShell()`
3. Ensure middleware comes after `UseRouting()`
4. Check controller inherits `SwapController`

### Content Not Swapping

If links don't work:

1. Verify HTMX script is loaded
2. Check `hx-target` selector is correct
3. Ensure target element exists in DOM
4. Check browser console for HTMX errors

## Migration from Manual Implementation

If you have existing code checking `HX-Request`:

**Before:**
```csharp
public async Task<IActionResult> Index()
{
    var articles = await _context.Articles.ToListAsync();
    
    if (Request.Headers.ContainsKey("HX-Request"))
        return PartialView(articles);
    else
        return View(articles);
}
```

**After:**
```csharp
public async Task<IActionResult> Index()
{
    var articles = await _context.Articles.ToListAsync();
    return SwapView(articles); // That's it!
}
```

Change:
1. Controller: `Controller` → `SwapController`
2. Return: `View()` / `PartialView()` → `SwapView()`
3. Remove: Manual `HX-Request` checks

## Contributing

Contributions are welcome! Please see the [main Swap repository](https://github.com/jdtoon/swap) for contribution guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/jdtoon/swap/blob/main/LICENSE) file for details.

## Support

- 📖 [Documentation](https://github.com/jdtoon/swap/wiki)
- 🐛 [Issue Tracker](https://github.com/jdtoon/swap/issues)
- 💬 [Discussions](https://github.com/jdtoon/swap/discussions)

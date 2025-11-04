# Swap.Htmx

[![NuGet](https://img.shields.io/nuget/v/Swap.Htmx.svg)](https://www.nuget.org/packages/Swap.Htmx/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

HTMX navigation framework for ASP.NET Core MVC applications. Provides a rigid, opinionated structure for building HTMX-powered applications with automatic page/partial detection, middleware enforcement, and extension methods.

## Features

- **SwapController Base Class**: Automatically handles page vs partial rendering based on HX-Request header
- **SwapView() Helper**: Single method that returns full page or partial view based on request type
- **Middleware Enforcement**: Catches and reports full page responses when partials are expected
- **Extension Methods**: Fluent API for working with HTMX request/response headers
- **Zero Configuration**: Works out of the box with sensible defaults

## Installation

```bash
dotnet add package Swap.Htmx
```

## Quick Start

### 1. Register Services and Middleware

In your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add MVC and Swap.Htmx
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(events =>
{
    // Example chain: when a product is created, refresh any list listening
    events.Chain(Swap.Htmx.Events.SwapEvents.Entity.Created("product"),
                 Swap.Htmx.Events.SwapEvents.UI.RefreshList);
});

var app = builder.Build();

// Add middleware (after UseRouting, before MapControllers)
app.UseRouting();
app.UseSwapHtmx();       // Event context + response header builder
app.UseSwapHtmxShell(); // Enforces partial responses for HTMX requests

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### 2. Update Controllers

Change your controllers to inherit from `SwapController` and use `SwapView()`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

public class ArticlesController : SwapController
{
    private readonly Swap.Htmx.Events.ISwapEventBus _events;
    private readonly AppDbContext _context;

    public ArticlesController(AppDbContext context, Swap.Htmx.Events.ISwapEventBus events)
    {
        _context = context;
        _events = events;
    }

    public async Task<IActionResult> Index()
    {
        var articles = await _context.Articles.ToListAsync();
        return SwapView(articles); // Automatically returns partial or full view
    }

    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null) return NotFound();
        
        return SwapView(article); // Works for any action
    }

    [HttpPost]
    public async Task<IActionResult> Create(Article article)
    {
        if (!ModelState.IsValid)
            return SwapView("Create", article);

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();
        await _events.EmitAsync(Swap.Htmx.Events.SwapEvents.Entity.Created("article"), new { id = article.Id });
        return SwapView("Details", article);
    }
}
```

### 3. Update Views

**Index.cshtml** (Shell with nested loading):
```html
@model IEnumerable<Article>

<div id="articles-container">
    <h1>Articles</h1>
    
    <!-- Nested loading: content loads via HTMX -->
    <div hx-get="/Articles/List" 
         hx-trigger="load" 
         hx-target="#articles-list"
         hx-indicator="#loading">
        <div id="loading">Loading articles...</div>
    </div>
    
    <div id="articles-list"></div>
</div>
```

**List.cshtml** (Partial content):
```html
@model IEnumerable<Article>

@foreach (var article in Model)
{
    <div class="article-card">
        <h2>
            <a href="/Articles/Details/@article.Id"
               hx-get="/Articles/Details/@article.Id"
               hx-target="#main-content"
               hx-push-url="true">
                @article.Title
            </a>
        </h2>
        <p>@article.Summary</p>
    </div>
}
```

**_Layout.cshtml** (No hx-boost, explicit targets):
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>@ViewData["Title"] - My App</title>
    <script src="https://unpkg.com/htmx.org@1.9.10"></script>
</head>
<body>
    <nav>
        <a href="/" 
           hx-get="/" 
           hx-target="#main-content" 
           hx-push-url="true">Home</a>
        <a href="/Articles" 
           hx-get="/Articles" 
           hx-target="#main-content" 
           hx-push-url="true">Articles</a>
    </nav>
    
    <main id="main-content">
        @RenderBody()
    </main>
</body>
</html>
```

## How It Works
### Event System (Filtered + Chained)

Swap.Htmx includes a minimal event bus that:
- Captures events you emit in controllers during a request
- Resolves configured chains (e.g., product.created → ui.refreshList)
- Filters to active client subscriptions from `X-Swap-Events`
- Builds an `HX-Trigger` header automatically at response time

Usage recap:
- Register: `builder.Services.AddSwapHtmx(opts => opts.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList));`
- Middleware: `app.UseSwapHtmx();`
- Emit in controller: `await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id });`

Client side, ensure your components declare the events they listen to and send the `X-Swap-Events` header with active subscriptions (a small helper script can do this automatically; see docs). If the header is missing, no filtering occurs and all emitted+chained events are sent.

#### Chain resolution modes

You can control how chains expand at runtime via an enum on options. Default is safest.

```csharp
builder.Services.AddSwapHtmx(opts =>
{
    // Chains (prefer typed backend keys)
    opts.Chain(Swap.Htmx.Events.SwapEvents.Todo.Created,
               Swap.Htmx.Events.SwapEvents.UI.Todo.RefreshList,
               Swap.Htmx.Events.SwapEvents.UI.Stats.Refresh);

    // Resolution defaults to OneHop (immediate children only)
    opts.ResolutionMode = Swap.Htmx.Events.ChainResolutionMode.OneHop; // default

    // Other strategies:
    // opts.ResolutionMode = ChainResolutionMode.Bidirectional; // reverse one-hop (Y emits X when X->Y configured)
    // opts.ResolutionMode = ChainResolutionMode.Transitive;    // expand breadth-first up to MaxTransitiveDepth
    // opts.MaxTransitiveDepth = 2; // depth limit when Transitive
});
```

Semantics:
- OneHop: A → {B,C} means emitting A includes B and C only.
- Bidirectional: A → B means emitting A includes B, and emitting B also includes A (one hop each way).
- Transitive: A → B → C expands along edges up to the configured depth (depth=1 equals OneHop).

Guardrails: `Validate()` checks for invalid names and cycles at startup (Development), regardless of mode.

### Strongly-typed backend event keys

Backend code can (and should) use typed event keys via `EventKey` and the typed overloads for `Chain(...)`, `Emit(...)`, and `EmitAsync(...)`:

```csharp
using Swap.Htmx.Events;

builder.Services.AddSwapHtmx(events =>
{
    events.Chain(SwapEvents.Entity.Created("product"), SwapEvents.UI.RefreshList);
});

public class ArticlesController : SwapController
{
    private readonly ISwapEventBus _events;
    public ArticlesController(ISwapEventBus events) => _events = events;

    public async Task<IActionResult> Create(Article article)
    {
        // ...save...
        await _events.EmitAsync(SwapEvents.Entity.Created("article"), new { id = article.Id });
        return SwapView("Details", article);
    }
}
```

There is also a Roslyn analyzer (`Swap.Htmx.Analyzers`) wired repo-wide that flags raw string usage in backend calls to `Chain`/`Emit` to help you avoid magic strings. Tests are suppressed by default. Note: HTML remains plain HTMX; you do not need to change attributes in markup.


#### Client helper (swap-events.js)

If you used the monolith template, include `/wwwroot/js/swap-events.js`. Otherwise, copy it from `templates/monolith/wwwroot/js/swap-events.js.template` into your app (rename to `swap-events.js`) and add it to your layout:

```html
<script src="/js/swap-events.js"></script>
<script>
    // Opt-in to events you care about on this page
    SwapEvents.activate('ui.refreshList');
    // Later: SwapEvents.deactivate('ui.refreshList');
    // Advanced: SwapEvents.set(['ui.refreshList', 'ui.showToast']);
    // Inspect current: SwapEvents.list()
    // Clear all: SwapEvents.clear()
    // The script will automatically set X-Swap-Events on HTMX requests.
    </script>
```


### Automatic Page/Partial Detection

`SwapView()` checks for the `HX-Request` header:

- **HTMX Request** (header present): Returns `PartialView()` - no layout
- **Normal Request** (initial load, refresh): Returns `View()` - with layout

This means:
- First page load → Full page with layout
- Navigation via HTMX → Partial view swapped into target
- Browser refresh → Full page with layout again
- No manual detection needed in every action

### Middleware Enforcement

`SwapHtmxShellMiddleware` intercepts responses and checks:
- If request has `HX-Request` header (excluding boosted requests)
- If response is full HTML page (contains `<!DOCTYPE>`, `<html>`, `<head>`)
- If so, returns helpful error message instead

This catches common mistakes:
- Using `View()` instead of `SwapView()`
- Error pages returning full layout for HTMX requests
- Accidental layout rendering

### Navigation Pattern

**Explicit HTMX Attributes** (not hx-boost):
```html
<a href="/Articles/Details/1"
   hx-get="/Articles/Details/1"
   hx-target="#main-content"
   hx-push-url="true">
    View Article
</a>
```

Why explicit over hx-boost:
- ✅ Clear intent - you see exactly what each link does
- ✅ No conflicts between `hx-boost` and explicit `hx-target`
- ✅ Per-link control over targets and behavior
- ✅ Easier to debug and reason about

### Nested Loading Pattern

For progressive enhancement:

```html
<!-- Index page loads immediately -->
<div id="articles-container">
    <h1>Articles</h1>
    
    <!-- Content loads after page renders -->
    <div hx-get="/Articles/List" 
         hx-trigger="load">
        Loading...
    </div>
</div>
```

Benefits:
- Fast initial page load
- Progressive content loading
- Better perceived performance
- Graceful degradation (content still loads without JS)

## Extension Methods

### Request Detection

```csharp
// Check if request is from HTMX
if (Request.IsHtmxRequest())
{
    // Handle HTMX-specific logic
}

// Check if request is boosted
if (Request.IsHtmxBoosted())
{
    // Handle boosted request
}

// Get HTMX headers
var currentUrl = Request.GetHtmxCurrentUrl();
var target = Request.GetHtmxTarget();
var trigger = Request.GetHtmxTrigger();
```

### Response Headers

```csharp
// Trigger client-side event
Response.HxTrigger("itemCreated");

// Trigger with JSON details
Response.HxTriggerWithDetails("{\"showMessage\": {\"level\": \"info\"}}");

// Push URL to browser history
Response.HxPushUrl($"/articles/{article.Id}");

// Replace URL in history
Response.HxReplaceUrl($"/articles/{article.Id}");

// Client-side redirect
Response.HxRedirect("/login");

// Force full page refresh
Response.HxRefresh();

// Change target element
Response.HxRetarget("#notification-area");

// Change swap strategy
Response.HxReswap("beforebegin");
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

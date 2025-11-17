# Swap

[![NuGet - Swap.Htmx](https://img.shields.io/nuget/v/Swap.Htmx.svg?label=Swap.Htmx)](https://www.nuget.org/packages/Swap.Htmx)
[![NuGet - Swap.Testing](https://img.shields.io/nuget/v/Swap.Testing.svg?label=Swap.Testing)](https://www.nuget.org/packages/Swap.Testing)
[![CI](https://github.com/jdtoon/swap/actions/workflows/ci-build.yml/badge.svg)](https://github.com/jdtoon/swap/actions/workflows/ci-build.yml)

HTMX + ASP.NET Core MVC, but ergonomic.

Swap is a small set of libraries that make it pleasant to build server‑rendered apps with HTMX and MVC:

- `Swap.Htmx` – runtime helpers: HTMX‑aware base controller, middleware, SSE primitives, event system, and extension methods for working with HX headers.
- `Swap.Testing` – testing helpers: HTMX‑aware integration test client and rich HTML/HTMX assertions.

You keep normal ASP.NET Core MVC. Swap gives you better defaults for HTMX requests and tests.

## Why Swap?

- **Stay server‑side** – HTML over the wire, no SPA framework required.
- **HTMX‑first MVC** – controllers and middleware that understand HX headers out of the box.
- **Events, not glue** – a small event system that turns server actions into `HX-Trigger` headers and optional SSE broadcasts.
- **Strong testing story** – integration tests that speak in terms of partials, HTMX attributes, and HX headers.

## Quick Start

**Install the package:**
```bash
dotnet add package Swap.Htmx
```

**Inherit from SwapController:**
```csharp
public class HomeController : SwapController
{
    public IActionResult Index() => SwapView();
}
```

**That's it!** Your controller now:
- Returns partials for HTMX requests, full pages otherwise
- Has access to `SwapResponse()` for multi-part updates
- Has access to `SwapEvent()` for event-driven UI updates
- Automatically handles session cookie persistence via `GetOrInitializeSessionId()`

**New in 0.5.0:**
- 🎯 Automatic session cookie persistence
- 📁 Configurable view search paths for cross-controller OOB swaps
- 🎨 Standalone toast CSS (zero dependencies)
- 📦 Event payload access in event chains
- 🔧 OOB instance ID helpers for lists

## Tiny example

`Program.cs` (minimal wiring):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add Swap.Htmx with optional configuration
builder.Services.AddSwapHtmx(options =>
{
    // Optional: Configure view search paths for cross-controller OOB swaps
    options.PartialViewSearchPaths.Add("Shared");
    
    // Optional: Configure event chains
    options.EventBus.When(new EventKey("todo.created"))
        .RefreshPartial("todo-count", "_TodoCount", ctx => GetTodoCount())
        .Toast("Todo created!", ToastType.Success);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx(); // Add Swap.Htmx middleware
app.MapControllers();

app.Run();
```

`TodosController.cs`:

```csharp
public class TodosController : SwapController
{
    private readonly ITodoService _service;
    
    public TodosController(ITodoService service)
    {
        _service = service;
    }

    // Tier 1: SwapView - Automatically handles HTMX vs full page
    [HttpGet("/todos")]
    public IActionResult Index()
    {
        var todos = _service.GetTodos();
        return SwapView(todos);
    }

    // Tier 2: SwapResponse - Coordinated multi-part updates
    [HttpPost("/todos")]
    public IActionResult Create(TodoInput input)
    {
        var todo = _service.Create(input);
        
        return SwapResponse()
            .AlsoUpdate("todo-count", "_TodoCount", _service.GetCount())
            .WithSuccessToast("Todo created!")
            .Build();
    }
    
    // Tier 3: SwapEvent - Event-driven updates (uses configured event chains)
    [HttpDelete("/todos/{id}")]
    public IActionResult Delete(int id)
    {
        _service.Delete(id);
        return SwapEvent(new EventKey("todo.deleted")).Build();
    }
}
```

`TodosTests.cs` (using `Swap.Testing`):

```csharp
public class TodosTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient _client;

    public TodosTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetTodos_ReturnsPartialWithItems()
    {
        var response = await _client.HtmxGetAsync("/todos");

        await response
            .AssertSuccess()
            .AssertPartialViewAsync()
            .AssertElementCountAsync(".todo-item", expectedCount: 5);
    }
    
    [Fact]
    public async Task CreateTodo_ReturnsOobSwapAndToast()
    {
        var response = await _client.HtmxPostAsync("/todos", new { Title = "New Todo" });
        
        await response
            .AssertSuccess()
            .AssertHasOobSwapAsync("todo-count")
            .AssertHasTriggerAsync("showToast");
    }
}
```

## Packages

- [`Swap.Htmx`](https://www.nuget.org/packages/Swap.Htmx) – HTMX‑friendly building blocks for ASP.NET Core MVC apps. [Docs](lib/Swap.Htmx/README.md)
- [`Swap.Testing`](https://www.nuget.org/packages/Swap.Testing) – fluent integration tests for HTMX endpoints. [Docs](lib/Swap.Testing/README.md)

## Demo Application

**[SwapShop](demo/SwapShop/README.md)** – A fully functional e-commerce demo showcasing all three tiers of the Swap.Htmx API:
- **Tier 1: SwapView** - Simple HTMX-aware view rendering
- **Tier 2: SwapResponse** - Coordinated multi-part updates with OOB swaps
- **Tier 3: SwapEvent** - Event-driven UI updates with configurable event chains

Features demonstrated:
- Shopping cart with session persistence
- Toast notifications (success, error, warning, info)
- Event chains for coordinated updates
- HTMX navigation with browser history support
- Form submissions with optimistic UI updates
- Debug logging with color-coded console output

Quick start:
```bash
cd demo/SwapShop/src
dotnet run
# Open http://localhost:5120
```

## Examples

- **[SwapShop](demo/SwapShop)** – Production-ready e-commerce demo with comprehensive event chain examples
- `lib/Swap.Testing/EXAMPLE_TESTS.cs` – Example test suite using the testing helpers



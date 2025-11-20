# Swap

[![NuGet - Swap.Htmx](https://img.shields.io/nuget/v/Swap.Htmx.svg?label=Swap.Htmx)](https://www.nuget.org/packages/Swap.Htmx)
[![NuGet - Swap.Testing](https://img.shields.io/nuget/v/Swap.Testing.svg?label=Swap.Testing)](https://www.nuget.org/packages/Swap.Testing)
[![CI](https://github.com/jdtoon/swap/actions/workflows/ci-build.yml/badge.svg)](https://github.com/jdtoon/swap/actions/workflows/ci-build.yml)

HTMX + ASP.NET Core MVC, but ergonomic.

**Reactive UI Orchestration for ASP.NET Core.**

Swap transforms standard MVC controllers into an **orchestration layer** for your user interface. It decouples **User Actions** (Events) from **UI Updates** (Reactions), giving you the interactivity of a SPA with the simplicity of server-side HTML.

It handles the "messy middle" of modern web apps—validation, partial updates, and real-time events—so you can focus on building features, not glue code.

- `Swap.Htmx` – runtime orchestration: HTMX‑aware controllers, decentralized event configuration, SSE bridge, and type-safe event system.
- `Swap.Testing` – testing helpers: HTMX‑aware integration test client and rich HTML/HTMX assertions.

## Why Swap?

- **Orchestrate, Don't Glue** – Decouple controller actions from view rendering using Event Chains.
- **Stay Server‑Side** – Build rich, reactive apps using standard Razor views and HTMX.
- **Type-Safe Events** – Coordinate partials, toasts, and triggers with a strongly-typed event system.
- **Minimal APIs** – First-class support for `IResult` endpoints with `SwapResults`.
- **Razor Pages** – Native support for `PageModel` with `this.SwapResponse()`.
- **Real-Time Ready** – Built-in Server-Sent Events (SSE) bridge with `ISseBackplane` support for web farms.

## Quick Start

**Install the package:**
```bash
dotnet add package Swap.Htmx
```

**Minimal API (New!):**
```csharp
app.MapPost("/subscribe", (string email) => 
    SwapResults.Response()
        .WithSuccessToast("Subscribed!")
        .WithView("_SuccessMessage", email));
```

**Razor Pages (New!):**
```csharp
public class CounterModel : PageModel
{
    public IActionResult OnGetIncrement() => 
        this.SwapResponse()
            .WithView("_Counter", this)
            .Build();
}
```

**Inherit from SwapController:**
```csharp
public class HomeController : SwapController
{
    public IActionResult Index() => SwapView();
}
```

**Or use Composition (Standard Controller):**
```csharp
public class HomeController : Controller
{
    // Use extension methods on 'this'
    public IActionResult Index() => this.SwapView();
}
```

**That's it!** Your controller now:
- Returns partials for HTMX requests, full pages otherwise
- Has access to `SwapResponse()` for multi-part updates
- Has access to `SwapEvent()` for event-driven UI updates
- Automatically handles session cookie persistence via `GetOrInitializeSessionId()`

## Features

- 🧩 **Composition Over Inheritance** - Use `Swap.Htmx` with standard Controllers via extension methods
- 🎯 **Automatic Session Persistence** - Handles cookie tracking for you
- 📁 **View Search Paths** - Share OOB partials across controllers easily
- 🎨 **Built-in Toasts** - Zero-dependency toast notifications included
- 📦 **Event Payload Access** - Pass data through event chains efficiently
- 🔧 **OOB Helpers** - Tools for managing dynamic lists and instance IDs

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
    
    // Optional: Register decentralized event configurations
    options.AddConfig<TodoEventConfig>();
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

## Demo Applications

### [SwapShop](demo/SwapShop/README.md) – E-commerce Foundation

## Demos

Explore the `demo/` folder to see Swap.Htmx in action:

### 1. [SwapShop](demo/SwapShop) (MVC)
A fully functional e-commerce demo showcasing the core features:
- **SwapView** & **SwapResponse** basics
- Coordinated multi-part updates (Cart + Badge + Total)
- Session persistence and Toast notifications

### 2. [TaskFlow](demo/TaskFlow) (Advanced MVC)
A collaborative task management app showcasing advanced patterns:
- **Server-Sent Events (SSE)** for real-time dashboards
- Complex event chains and payload access
- All swap modes (Delete, BeforeEnd, etc.)

### 3. [SwapChat](demo/SwapChat) (Distributed SSE)
**New in v0.9.0!** A real-time chat application demonstrating scalability:
- **Distributed SSE** using `ISseBackplane`
- Multi-server synchronization (simulated with file system)
- Room-based broadcasting

### 4. [SwapMinimal](demo/SwapMinimal) (Minimal APIs)
Demonstrates first-class support for Minimal APIs:
- `SwapResults` factory for `IResult` endpoints
- Functional endpoint organization

### 5. [SwapPages](demo/SwapPages) (Razor Pages)
Demonstrates native Razor Pages support:
- `PageModel` extensions (`this.SwapResponse()`)
- `OnGet`/`OnPost` handler integration

## Documentation

- [**Getting Started**](lib/Swap.Htmx/docs/GettingStarted.md)
- [**Events & Triggers**](lib/Swap.Htmx/docs/Events.md)
- [**Event Chains**](lib/Swap.Htmx/docs/EventChains.md)
- [**Out-of-Band Swaps**](lib/Swap.Htmx/docs/OutOfBandSwaps.md)
- [**Server-Sent Events**](lib/Swap.Htmx/docs/ServerSentEvents.md)



# Swap

[![NuGet - Swap.Htmx](https://img.shields.io/nuget/v/Swap.Htmx.svg?label=Swap.Htmx)](https://www.nuget.org/packages/Swap.Htmx)
[![NuGet - Swap.Templates](https://img.shields.io/nuget/v/Swap.Templates.svg?label=Swap.Templates)](https://www.nuget.org/packages/Swap.Templates)
[![NuGet - Swap.Testing](https://img.shields.io/nuget/v/Swap.Testing.svg?label=Swap.Testing)](https://www.nuget.org/packages/Swap.Testing)
[![CI](https://github.com/jdtoon/swap/actions/workflows/ci-build.yml/badge.svg)](https://github.com/jdtoon/swap/actions/workflows/ci-build.yml)

HTMX + ASP.NET Core MVC, but ergonomic.

**Reactive UI Orchestration for ASP.NET Core.**

Swap transforms standard MVC controllers into an **orchestration layer** for your user interface. It decouples **User Actions** (Events) from **UI Updates** (Reactions), giving you the interactivity of a SPA with the simplicity of server-side HTML.

It handles the "messy middle" of modern web apps—validation, partial updates, and real-time events—so you can focus on building features, not glue code.

- `Swap.Htmx` – runtime orchestration: HTMX‑aware controllers, decentralized event configuration, SSE bridge, and type-safe event system.
- `Swap.Testing` – testing helpers: HTMX‑aware integration test client and rich HTML/HTMX assertions.

## Why Swap?

- **Orchestrate, Don't Glue** – Decouple controller actions from view rendering using Event Chains and Distributed Handlers.
- **Stay Server‑Side** – Build rich, reactive apps using standard Razor views and HTMX.
- **Type-Safe Events** – Coordinate partials, toasts, and triggers with a strongly-typed event system and Source Generators.
- **Minimal APIs** – First-class support for `IResult` endpoints with `SwapResults`.
- **Razor Pages** – Native support for `PageModel` with `this.SwapResponse()`.
- **Real-Time Ready** – Built-in Server-Sent Events (SSE) and WebSocket bridge with `ISseBackplane` support for web farms and `CanJoinRoom` security hooks.

## Quick Start

**The easiest way to get started is using the project templates:**

```bash
# Install the templates
dotnet new install Swap.Templates

# Create a new project
dotnet new swap-mvc -n MyProject
```

**Or install the package manually:**
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
- Automatically handles user context resolution via `GetOrInitializeSessionId()` (defaults to Session)

## Features

- 🧩 **Composition Over Inheritance** - Use `Swap.Htmx` with standard Controllers via extension methods
- 🎯 **Pluggable User Context** - Abstracted user ID resolution (Session, Cookie, JWT, etc.)
- 📁 **View Search Paths** - Share OOB partials across controllers easily
- 🎨 **Built-in Toasts** - Zero-dependency toast notifications included
- 📦 **Event Payload Access** - Pass data through event chains efficiently
- 📊 **Observability** - Full OpenTelemetry tracing, metrics, and structured logging
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

## Demos

**Start here: [SwapLab](demo/SwapLab)** — The interactive pattern library showcasing all Swap.Htmx features with live demos, code samples, and explanations.

Other examples in `demo/`:

| Demo | Description |
|------|-------------|
| **[SwapLab](demo/SwapLab)** | 🎯 **Interactive pattern library** — Start here! Live demos of all patterns |
| [SwapShop](demo/SwapShop) | E-commerce demo (Cart, Badges, Toasts) |
| [SwapChat](demo/SwapChat) | Real-time chat with SSE and Rooms |
| [TaskFlow](demo/TaskFlow) | Collaborative task management with event chains |
| [SwapMinimal](demo/SwapMinimal) | Minimal APIs integration |
| [SwapPages](demo/SwapPages) | Razor Pages integration |
| [SwapWebSockets](demo/SwapWebSockets) | WebSocket integration |
| [SwapRedisDemo](demo/SwapRedisDemo) | Redis backplane for scaling SSE |

## Documentation

- [**Getting Started**](lib/Swap.Htmx/docs/GettingStarted.md)
- [**Events & Triggers**](lib/Swap.Htmx/docs/Events.md)
- [**Event Chains**](lib/Swap.Htmx/docs/EventChains.md)
- [**Realtime (WebSockets & SSE)**](lib/Swap.Htmx/docs/WebSockets.md)
- [**Server-Sent Events**](lib/Swap.Htmx/docs/ServerSentEvents.md)
- [**Out-of-Band Swaps**](lib/Swap.Htmx/docs/OutOfBandSwaps.md)
- [**Minimal APIs**](lib/Swap.Htmx/docs/MinimalApis.md)
- [**Razor Pages**](lib/Swap.Htmx/docs/RazorPages.md)
- [**Source Generators**](lib/Swap.Htmx/docs/SourceGenerators.md)
- [**User Context & Identity**](lib/Swap.Htmx/docs/UserContext.md)
- [**Debugging & Logging**](lib/Swap.Htmx/docs/DebuggingAndLogging.md)



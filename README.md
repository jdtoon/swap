# Swap

[![GitHub License](https://img.shields.io/github/license/jdtoon/swap)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![NuGet Swap.CLI](https://img.shields.io/nuget/v/Swap.CLI?label=Swap.CLI&logo=nuget)](https://www.nuget.org/packages/Swap.CLI)
[![NuGet Swap.Htmx](https://img.shields.io/nuget/v/Swap.Htmx?label=Swap.Htmx&logo=nuget)](https://www.nuget.org/packages/Swap.Htmx)
[![NuGet Swap.Modularity](https://img.shields.io/nuget/v/Swap.Modularity?label=Swap.Modularity&logo=nuget)](https://www.nuget.org/packages/Swap.Modularity)
[![NuGet Swap.Testing](https://img.shields.io/nuget/v/Swap.Testing?label=Swap.Testing&logo=nuget)](https://www.nuget.org/packages/Swap.Testing)

**Build modern, interactive ASP.NET Core apps with HTMX—fast, modular, and testable.**

Swap is an HTMX-first framework for ASP.NET Core. Three focused libraries plus production-ready templates help you build modular, server-rendered applications without JavaScript framework complexity.

---

## Core Libraries

### Swap.Htmx — HTMX Integration + Event System

HTMX-aware controllers, fluent headers, and declarative event chains for server-driven interactivity.

```csharp
// Automatic full page vs partial detection
public class ArticlesController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var articles = await _context.Articles.ToListAsync();
        return SwapView(articles);  // Full page or partial—handled automatically
    }
}

// Server-driven events for interactive UX
await _events.EmitAsync(SwapEvents.Entity.Created("article"), new { id = 42 });
Response.HxTrigger("articleRefreshed");
Response.ShowSuccessToast("Article created!");
```

**Features:**
- `SwapController` — auto-detect HTMX requests, return full page or partial automatically
- Fluent header API — clean methods for HX-Trigger, HX-Redirect, HX-Refresh, etc.
- Event chains — map domain events → UI events declaratively
- Server events (RabbitMQ) — optional distributed events for modular deployments
- Zero config — works out of the box

### Swap.Modularity — Lightweight Module System

Compose independent modules with automatic discovery and dependency ordering.

```csharp
// Define a module
public sealed class OrdersModule : IModule
{
    public string Name => "Orders";
    public IReadOnlyList<string> DependsOn => new[] { "Products" };

    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<OrderService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/orders", /* ... */);
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        registrar.Register("order.created", async (OrderCreated e) => {
            // Handle domain events
        });
    }
}

// Host discovers and wires everything
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwapModules(builder.Configuration);

var app = builder.Build();
app.MapSwapModuleEndpoints();
app.Run();
```

**Features:**
- `IModule` contract — name, dependencies, three hooks (services, endpoints, events)
- Automatic discovery — scans assemblies, validates dependencies, topological sort
- RCL support — auto-loads UI assemblies and event contributors
- Clear errors — missing dependencies and cycles fail fast with helpful messages

### Swap.Testing — HTMX-Aware Integration Testing

Fluent testing API for HTMX requests, partials, events, and DOM assertions.

```csharp
using Swap.Testing;

public class ArticleTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public ArticleTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task CreateArticle_WithHtmx_RefreshesListAndShowsToast()
    {
        // Act: Submit form as HTMX request
        var form = await _client.GetAsync("/articles/create");
        var response = await form.SubmitFormAsync(new { title = "Hello", content = "World" });

        // Assert: Verify partial, DOM, and events
        response.AssertSuccess();
        await response.AssertPartialViewAsync();
        await response.AssertHxTriggerAsync("articleCreated");
        await response.AssertElementContainsAsync("h2", "Hello");
    }
}
```

**Features:**
- Fluent assertions — chainable, readable test syntax
- HTMX headers — automatic HX-Request, verify HX-Trigger events
- Partial testing — assert full page vs partial response
- Form submission — follow redirects, submit with attributes, test validation
- Snapshot testing — scrubbers for GUIDs, timestamps, anti-forgery tokens

---

## Production Templates

Complete, runnable applications with HTMX, Tailwind CSS, EF Core, Docker, and tests.

### swap-monolith
Single-project app for rapid development. Perfect for MVPs and small teams.

```bash
swap new MyApp
```

Includes: ASP.NET Core MVC + Swap.Htmx + HTMX 2.x + Tailwind/DaisyUI + EF Core migrations + Docker Compose + integration tests + example CRUD.

### swap-layered
Multi-project clean architecture (Web, Application, Domain, Infrastructure).

```bash
swap new MyApp --template swap-layered
```

Includes: Clean layers + Swap.Htmx event system + provider-specific EF Core + session support + full test suite + example domain models.

### swap-modular-monolith ⭐ **Recommended**
Single deployable with module boundaries. Sweet spot between monolith and microservices.

```bash
swap new MyApp --template swap-modular-monolith
```

Includes: Host + independent modules (Contracts, Module, Web RCL) + per-module migrations + Swap.Modularity + Docker Compose (Postgres, RabbitMQ) + distributed events + production-ready structure.

---

## Event System

Declarative, server-driven UI reactions. Emit domain events; chains resolve them to HTMX triggers.

```csharp
// Define chains once during startup
builder.Services.AddSwapHtmx(events =>
{
    // When a product is created, also trigger these UI events
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});

// In your controller, just emit the domain event
public class ProductsController : SwapController
{
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), product);
        return SwapView(product);
    }
}

// Client receives HX-Trigger with all chained events
// HTMX markup handles them: hx-trigger="productRefreshed from:body"
```

**Resolution modes:** OneHop (default), Bidirectional, Transitive  
**Dev tools:** `/_swap/dev/events` (visual dashboard), `swap events list/validate/graph` (CLI)

---

## Quick Start

**Prerequisites:** .NET 9.0 SDK, Node.js (LTS), libman CLI

```bash
# Install
dotnet tool install -g Swap.CLI

# Create app
swap new MyApp
cd MyApp
dotnet watch
```

Runs npm install, builds CSS, applies migrations, starts at `https://localhost:5001`.

---

## Key Features

**SwapController** — one method returns full page or partial based on HX-Request header  
**Fluent headers** — `Response.HxTrigger()`, `HxRedirect()`, `ShowSuccessToast()`, etc.  
**Auto discovery** — define `IModule`, it's found and wired automatically  
**Testable events** — chains validated at startup, test without DOM/JavaScript

---

## Documentation

- **[EVENTS.md](docs/EVENTS.md)** — Event chains, resolution modes, RabbitMQ, dev tools
- **[TEMPLATES.md](docs/TEMPLATES.md)** — Template comparison, usage, migration paths
- **[PRODUCT.md](docs/PRODUCT.md)** — Architecture, design decisions, philosophy
- **[Wiki](https://jdtoon.github.io/swap/)** — Getting started, guides, examples

---

## License & Contributing

MIT License. Contributions welcome—see [CONTRIBUTING.md](CONTRIBUTING.md).

---

**Build fast, stay modular, ship quality.**

[📖 Docs](docs/) • [🐙 GitHub](https://github.com/jdtoon/swap) • [💬 Discussions](https://github.com/jdtoon/swap/discussions)

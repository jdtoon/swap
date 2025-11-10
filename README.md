# Swap

[![GitHub License](https://img.shields.io/github/license/jdtoon/swap)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![NuGet Swap.CLI](https://img.shields.io/nuget/v/Swap.CLI?label=Swap.CLI&logo=nuget)](https://www.nuget.org/packages/Swap.CLI)
[![NuGet Swap.Htmx](https://img.shields.io/nuget/v/Swap.Htmx?label=Swap.Htmx&logo=nuget)](https://www.nuget.org/packages/Swap.Htmx)
[![NuGet Swap.Modularity](https://img.shields.io/nuget/v/Swap.Modularity?label=Swap.Modularity&logo=nuget)](https://www.nuget.org/packages/Swap.Modularity)
[![NuGet Swap.Testing](https://img.shields.io/nuget/v/Swap.Testing?label=Swap.Testing&logo=nuget)](https://www.nuget.org/packages/Swap.Testing)

**Build modern, interactive ASP.NET Core apps with HTMX—fast, modular, and testable. No JavaScript frameworks required.**

Swap is a complete toolkit for building production-ready server-rendered web applications. It combines three focused libraries with battle-tested templates to let you build modular monoliths with HTMX in days, not months.

---

## 🎯 The Three Pillars

### 1. **Swap.Htmx** — HTMX Ergonomics + Event System

Make ASP.NET Core HTMX-native with minimal friction.

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

**What you get:**
- **SwapController**: Auto-detects HTMX requests and returns appropriate views
- **Fluent Header API**: Read/write HTMX headers with clean C# methods
- **Event Bus**: Emit domain events; resolve them to HTMX UI reactions declaratively
- **Server Events (RabbitMQ)**: Optional: distribute events across processes for modular deployments
- **Zero Configuration**: Works out of the box with sensible defaults

### 2. **Swap.Modularity** — Lightweight Module System

Compose modules deterministically with dependency ordering and automatic discovery.

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

**What you get:**
- **IModule Contract**: Name, dependencies, and three hooks (services, endpoints, events)
- **Automatic Discovery**: Scans loaded assemblies; validates dependencies; topological sort
- **RCL Support**: Auto-loads UI assemblies and discovers `ISwapUiChainContributor` instances
- **Clear Errors**: Missing dependencies, cycles, and ordering issues throw informative exceptions
- **Event Integration**: Seamlessly wires domain events to the HTMX event system

### 3. **Swap.Testing** — Fluent HTMX Testing

Integration testing for server-rendered HTMX apps, with first-class support for partials and event headers.

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

**What you get:**
- **Fluent API**: Chainable assertions for readable, maintainable tests
- **HTMX-Aware**: Built-in support for HX-Request headers and HTMX attributes
- **Partial View Testing**: Assert full vs partial responses
- **Event Assertions**: Verify HX-Trigger headers and event payloads
- **Form Submission**: Follow HX-Redirect, submit forms with attributes, test validation errors
- **Snapshot Testing**: Built-in scrubbers for GUIDs, timestamps, and anti-forgery tokens

---

## 🚀 Production-Ready Templates

Every template is a complete, runnable application with HTMX, Tailwind/DaisyUI, EF Core, Docker, and integration tests included.

### Monolith (swap-monolith)
Single deployable for rapid development. Perfect for MVPs and smaller projects.

```bash
swap new MyApp --template swap-monolith
```

**Includes:**
- ASP.NET Core MVC with Swap.Htmx event system wired
- HTMX 2.x + DaisyUI components + Tailwind CSS
- EF Core migrations (pick your database: SQLite, SQL Server, PostgreSQL)
- Docker Compose (Postgres, RabbitMQ optional)
- Integration tests with Swap.Testing
- Example CRUD controllers and forms

### Layered (swap-layered)
Multi-project architecture (Web, Application, Domain, Infrastructure) for scaling and team organization.

```bash
swap new MyApp --template swap-layered
```

**Includes:**
- Clean layering: Presentation → Application → Domain → Infrastructure
- Swap.Htmx event system with domain layer integration
- Provider-specific EF Core setup per project
- Session support and development endpoints
- Pre-built CRUD examples (Todos, Stats, Dynamic forms)
- Full integration test suite

### Modular Monolith (swap-modular-monolith) ⭐ **Recommended**
Single deployable with module boundaries and per-module ownership. The sweet spot between monolith and microservices.

```bash
swap new MyApp --template swap-modular-monolith
```

**Includes:**
- Host app + modules as independent NuGet packages
- **Per-module structure**: Contracts, Module (services/endpoints), Web RCL (UI)
- **Provider-specific migrations**: Each module owns its database layer (SqlServer/Postgres)
- Swap.Modularity for deterministic composition
- Docker Compose: Postgres, RabbitMQ for distributed event chains
- Example chains, migrations, and module authoring guides
- Production-ready patterns extracted from real apps

---

## 🔔 Event System: Server-Driven Interactivity

The Swap Event System is the secret sauce—it makes UX declarative and testable.

**How it works:**
1. Controllers emit domain events during request processing
2. Configured chains map domain events → UI events
3. Events resolve based on subscription filters and chain modes
4. All active events are merged into the `HX-Trigger` response header
5. Client handles `HX-Trigger` events (refresh, toast, redirect, etc.)

**Example: Product Created**

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

// Client receives HX-Trigger with all resolved events
// Your HTMX markup handles them: hx-trigger="productRefreshed from:body"
```

**Chain Resolution Modes:**
- `OneHop` (default): Only immediate chained events
- `Bidirectional`: Event → dependency → event (reverse one-hop)
- `Transitive`: BFS across the graph up to a configurable depth

**Development Dashboard** (in Development environment):
- `/_swap/dev/events` — Visual dashboard with Mermaid graph
- `/_swap/dev/events.json` — Export chains as JSON
- `/_swap/dev/explain.json?event=...` — Preview event resolution

**CLI Tools:**
```bash
swap events list -p .
swap events validate -p .
swap events graph -p . --format mermaid
```

---

## ⚡ Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later — [Download](https://dotnet.microsoft.com/download)
- **Node.js (LTS)** — For Tailwind CSS compilation
- **libman CLI** — For client libraries
  ```bash
  dotnet tool install -g Microsoft.Web.LibraryManager.Cli
  ```

### Installation

Install Swap CLI globally:

```bash
dotnet tool install -g Swap.CLI
```

Verify:
```bash
swap --version
```

### Create Your First App

#### 1. Monolith (Get Started Fast)
```bash
swap new MyApp
cd MyApp
dotnet watch
```

Automatically runs npm install, builds CSS, applies migrations, and starts on `https://localhost:5001`.

#### 2. Modular Monolith (Production Pattern)
```bash
swap new MyApp --template swap-modular-monolith
cd MyApp
docker-compose up -d  # Optional: Postgres + RabbitMQ
dotnet run
```

Open `https://localhost:5001` → Explore examples, review module structure, extend with your own.

#### 3. Add a New Module (Modular Monolith)
```bash
cd src/Modules
dotnet new classlib -n MyModule.Contracts
dotnet new classlib -n MyModule
dotnet new razorclasslib -n MyModule.Web

# Wire it in the host's Program.cs
builder.Services.AddSwapModules(builder.Configuration);
```

---

## 📚 Architecture Overview

```
Swap/
├── framework/
│   ├── Swap.Htmx/              ✅ HTMX ergonomics + event system
│   ├── Swap.Modularity/        ✅ Module discovery + composition
│   ├── Swap.Testing/           ✅ Integration testing fluent API
│   └── Swap.Htmx.Analyzers/   ✅ Roslyn analyzer (catches magic strings)
├── templates/
│   ├── swap-monolith/          ✅ Single project, HTMX-native
│   ├── swap-layered/           ✅ Multi-project, clean layers
│   └── swap-modular-monolith/  ✅ Modular, per-module ownership
└── tools/
    └── Swap.CLI/               ✅ Template instantiation + utilities
```

---

## 🎨 Key Concepts

### SwapController + SwapView
Eliminate boilerplate: one method decides whether to render full page or partial based on the `HX-Request` header.

### Fluent Header API
```csharp
Response.HxTrigger("eventName");
Response.HxRedirect("/products");
Response.HxRefresh();
Response.HxRetarget("#new-target");
Response.HxReswap("beforebegin");
Response.HxPushUrl("/products/123");
Response.ShowSuccessToast("Done!");
```

### Module Discovery
No manual wiring. Define `IModule`, place it in a loaded assembly, and it's discovered, validated, and ordered automatically.

### Testable Events
Every event chain is validated at startup; test event propagation without touching the DOM or JavaScript.

---

## 🔗 Framework Packages

| Package | Version | Purpose |
|---------|---------|---------|
| **Swap.Htmx** | 0.3.1 | HTMX integration, event system, middleware |
| **Swap.Modularity** | 0.3.1 | Module discovery, composition, dependency ordering |
| **Swap.Testing** | 0.3.1 | HTMX-aware integration testing |

All available on [NuGet](https://www.nuget.org/packages?q=owner:jdtoon).

---

## 📖 Documentation

- **[PRODUCT.md](docs/PRODUCT.md)** — What is Swap? The three pillars, key features, and why use it
- **[EVENTS.md](docs/EVENTS.md)** — Complete guide to UI and Server Events, chain resolution, and RabbitMQ
- **[TEMPLATES.md](docs/TEMPLATES.md)** — Template comparison, usage guide, and migration paths
- **[Wiki](https://jdtoon.github.io/swap/)** — Getting started guides, tutorials, and examples

---

## 🌟 Why Swap?

| Feature | Benefit |
|---------|---------|
| **HTMX-First** | Modern interactivity without JavaScript framework complexity |
| **Server-Driven Events** | Declarative UI reactions; testable; no magic strings |
| **Modular Templates** | Production-ready starting points for any team size |
| **Type-Safe** | Compile-time errors catch bugs early; full IntelliSense |
| **Testable** | Fluent assertions on HTMX behavior; no DOM parsing hacks |
| **Battle-Tested** | Patterns extracted from real production applications |
| **Opinionated** | Sensible defaults; clear guidelines; less decision fatigue |
| **Zero Hidden Magic** | Generated code is readable; you own and understand it |

---

## 🛠️ Common Tasks

### Use an Event in a Template
```html
<button hx-post="/articles/create" 
        hx-target="#list" 
        hx-trigger="articleCreated from:body">
    Create
</button>
```

### Test Event Propagation
```csharp
[Fact]
public async Task CreateProduct_EmitsCreatedChain()
{
    var response = await _client.PostAsync("/products", formData);
    
    response.AssertSuccess();
    await response.AssertHxTriggerAsync("productRefreshed", "toastSuccess");
}
```

### Add RabbitMQ for Distributed Events
```csharp
builder.Services.AddSwapServerEventChainsFromConfiguration(
    builder.Configuration,
    "Swap:ServerEvents"  // Reads RabbitMQ connection string
);
```

### Extend a Module
Each module in a modular monolith can be extended independently—add endpoints, services, UI without touching other modules.

---

## 🚀 Next Steps

1. **[Install Swap CLI](#installation)** and create an app
2. **Read [TEMPLATES.md](docs/TEMPLATES.md)** to choose the right template for your needs
3. **Check [EVENTS.md](docs/EVENTS.md)** to master the event system
4. **Write your first test** using `Swap.Testing`

---

## 📜 License

Swap is open source under the [MIT License](LICENSE).

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## 🔮 Roadmap

- **0.3.x** — Event system consolidation, documentation, and template polish
- **0.4.x** — Template refinement, comprehensive documentation, security hardening
- **Future** — Additional templates, WebSocket/SignalR support, validation framework

---

**Built for developers who love ASP.NET Core and want to move fast without sacrificing code quality or team scalability.**

[📖 Read the Docs](docs/) • [🐙 GitHub](https://github.com/jdtoon/swap) • [💬 Discussions](https://github.com/jdtoon/swap/discussions)

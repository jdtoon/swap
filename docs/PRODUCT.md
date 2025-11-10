# Swap Framework

**Build modern, interactive ASP.NET Core apps with HTMX—fast, modular, and testable. No JavaScript frameworks required.**

Swap is a complete toolkit for building production-ready server-rendered web applications. It combines three focused libraries with battle-tested templates to let you build modular monoliths with HTMX in days, not months.

---

## 🎯 What is Swap?

Swap is an opinionated framework for ASP.NET Core that makes HTMX a first-class citizen. It provides:

1. **Three Core Libraries** — Purpose-built packages that solve real problems
2. **Production-Ready Templates** — Complete, runnable applications with HTMX, Tailwind, Docker, and tests
3. **Developer Tools** — CLI for scaffolding, event validation, and code generation
4. **Event-Driven Architecture** — Declarative UI reactions without JavaScript complexity

**The Philosophy:**
- **HTMX-First**: Server-rendered interactivity without heavy JavaScript frameworks
- **Opinionated**: Clear patterns, sensible defaults, less decision fatigue
- **Modular**: Compose applications from independent, testable modules
- **Production-Ready**: Battle-tested patterns from real applications
- **Developer Experience**: Fast feedback loops, clear errors, comprehensive tooling

---

## 🧩 The Three Pillars

### 1. Swap.Htmx — HTMX Ergonomics + Event System

Make ASP.NET Core HTMX-native with minimal friction.

**Key Features:**
- **SwapController**: Auto-detects HTMX requests and returns appropriate views (full page vs partial)
- **Fluent Header API**: Read/write HTMX headers with clean, type-safe C# methods
- **Event Bus**: Emit domain events; resolve them to HTMX UI reactions declaratively
- **Server Events**: Optional RabbitMQ integration for distributed event chains
- **Toast Notifications**: Built-in support for success/error/warning toasts
- **Zero Configuration**: Works out of the box with sensible defaults

**Example:**
```csharp
public class ArticlesController : SwapController
{
    private readonly ISwapEventBus _events;

    public async Task<IActionResult> Create(CreateArticleDto dto)
    {
        var article = await _service.CreateAsync(dto);
        
        // Emit domain event
        await _events.EmitAsync(SwapEvents.Entity.Created("article"), article);
        
        // Fluent header API
        Response.ShowSuccessToast("Article created!");
        
        // Auto-detects full vs partial
        return SwapView(article);
    }
}
```

**Installation:**
```bash
dotnet add package Swap.Htmx
```

**Quick Start:**
```csharp
// Program.cs
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(
        SwapEvents.Entity.Created("article"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});

app.UseSwapHtmx();
```

---

### 2. Swap.Modularity — Lightweight Module System

Compose modules deterministically with dependency ordering and automatic discovery.

**Key Features:**
- **IModule Contract**: Simple interface for defining modules (services, endpoints, events)
- **Automatic Discovery**: Scans loaded assemblies, validates dependencies, topological sort
- **Dependency Resolution**: Declare module dependencies; framework enforces order
- **RCL Support**: Auto-loads Razor Class Libraries and UI chain contributors
- **Clear Errors**: Missing dependencies, cycles, and ordering issues throw informative exceptions
- **Event Integration**: Seamlessly wires domain events to the HTMX event system

**Example:**
```csharp
public sealed class OrdersModule : IModule
{
    public string Name => "Orders";
    public IReadOnlyList<string> DependsOn => new[] { "Products", "Customers" };

    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddDbContext<OrdersDbContext>(opts => 
            opts.UseNpgsql(config.GetConnectionString("Orders")));
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: "orders",
            pattern: "orders/{action=Index}",
            defaults: new { controller = "Orders" });
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        registrar.Register("order.created", async (OrderCreated evt) => {
            // Handle cross-module event
        });
    }
}
```

**Installation:**
```bash
dotnet add package Swap.Modularity
```

**Quick Start:**
```csharp
// Program.cs
builder.Services.AddSwapModules(builder.Configuration);

var app = builder.Build();
app.MapSwapModuleEndpoints();
app.Run();
```

---

### 3. Swap.Testing — Fluent HTMX Testing

Integration testing for server-rendered HTMX apps, with first-class support for partials and event headers.

**Key Features:**
- **Fluent API**: Chainable assertions for readable, maintainable tests
- **HTMX-Aware**: Built-in support for HX-Request headers and HTMX attributes
- **Partial View Testing**: Assert full vs partial responses
- **Event Assertions**: Verify HX-Trigger headers and event payloads
- **Form Submission**: Follow HX-Redirect, submit forms with attributes, test validation errors
- **Snapshot Testing**: Built-in scrubbers for GUIDs, timestamps, and anti-forgery tokens
- **WebApplicationFactory Integration**: Works with ASP.NET Core test host

**Example:**
```csharp
using Swap.Testing;

public class ArticleTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public ArticleTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task CreateArticle_WithHtmx_RefreshesListAndShowsToast()
    {
        // Arrange
        var form = await _client.GetAsync("/articles/create");
        
        // Act: Submit form as HTMX request
        var response = await form.SubmitFormAsync(new { 
            title = "Hello", 
            content = "World" 
        });

        // Assert: Verify partial, DOM, and events
        response.AssertSuccess();
        await response.AssertPartialViewAsync();
        await response.AssertHxTriggerAsync("articleCreated", "articleRefreshed");
        await response.AssertElementContainsAsync("h2", "Hello");
    }

    [Fact]
    public async Task ValidationError_ReturnsFormWithErrors()
    {
        var response = await _client.PostFormAsync("/articles/create", new { 
            title = "" // Invalid
        });

        response.AssertValidationError();
        await response.AssertElementContainsAsync(".error", "Title is required");
    }
}
```

**Installation:**
```bash
dotnet add package Swap.Testing
```

---

## 🔗 Framework Packages

| Package | Version | Purpose |
|---------|---------|---------|
| **Swap.Htmx** | 0.3.1 | HTMX integration, event system, middleware |
| **Swap.Modularity** | 0.3.1 | Module discovery, composition, dependency ordering |
| **Swap.Testing** | 0.3.1 | HTMX-aware integration testing |
| **Swap.Htmx.Analyzers** | 0.3.1 | Roslyn analyzer (catches magic strings) |

All available on [NuGet](https://www.nuget.org/packages?q=owner:jdtoon).

---

## 🚀 Production-Ready Templates

Swap includes three production-ready templates, each with HTMX, Tailwind/DaisyUI, EF Core, Docker, and integration tests.

- **[swap-monolith](TEMPLATES.md#monolith-swap-monolith)** — Single deployable for rapid development
- **[swap-layered](TEMPLATES.md#layered-swap-layered)** — Multi-project clean architecture
- **[swap-modular-monolith](TEMPLATES.md#modular-monolith-swap-modular-monolith)** — Modular with per-module ownership ⭐ **Recommended**

See **[TEMPLATES.md](TEMPLATES.md)** for detailed comparison and usage.

---

## 🌟 Why Swap?

### For Individual Developers
- **Move Fast**: Production-ready templates eliminate weeks of setup
- **Less JavaScript**: HTMX handles interactivity; focus on C# and Razor
- **Clear Patterns**: Opinionated structure means less decision fatigue
- **Great DX**: Hot reload, dev dashboard, CLI tools, instant feedback

### For Teams
- **Modular Architecture**: Independent modules with clear boundaries
- **Team Ownership**: Modules can be owned by different team members
- **Testable**: Comprehensive testing support; event chains are testable without UI
- **Scalable**: Start with monolith, grow to modules, extract to services later

### For Production
- **Battle-Tested**: Patterns extracted from real production applications
- **Type-Safe**: Compile-time errors catch bugs early; full IntelliSense
- **Observable**: Built-in dev dashboard, event tracing, clear errors
- **Deployable**: Docker support, cloud-ready, migration strategies included

---

## 🎨 Key Concepts

### SwapController + SwapView

Eliminate boilerplate by auto-detecting HTMX requests:

```csharp
public class ProductsController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.ToListAsync();
        
        // Returns full page on direct navigation
        // Returns partial on HTMX request
        return SwapView(products);
    }
}
```

### Fluent Header API

Read and write HTMX headers with clean C# methods:

```csharp
// Read HTMX request headers
if (Request.IsHtmx())
{
    var target = Request.HxTarget();
    var trigger = Request.HxTrigger();
}

// Write HTMX response headers
Response.HxTrigger("productCreated");
Response.HxRedirect("/products");
Response.HxRefresh();
Response.HxRetarget("#new-target");
Response.HxReswap("beforebegin");
Response.HxPushUrl("/products/123");

// Toast notifications
Response.ShowSuccessToast("Product saved!");
Response.ShowErrorToast("Something went wrong");
Response.ShowWarningToast("Please review");
```

### Event-Driven Architecture

Define event chains once during startup, emit events in controllers:

```csharp
// Startup: Define chains
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});

// Controller: Emit domain event
await _events.EmitAsync(SwapEvents.Entity.Created("product"), new { id = 42 });

// Client receives HX-Trigger: productRefreshed, toastSuccess
```

### Module Discovery

No manual wiring—define `IModule`, place it in a loaded assembly, and it's discovered automatically:

```csharp
// Define module
public class ProductsModule : IModule
{
    public string Name => "Products";
    public IReadOnlyList<string> DependsOn => Array.Empty<string>();
    
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IProductService, ProductService>();
    }
    
    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(/*...*/);
    }
    
    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        registrar.Register("product.created", async evt => {/*...*/});
    }
}

// Host discovers and wires everything
builder.Services.AddSwapModules(builder.Configuration);
app.MapSwapModuleEndpoints();
```

---

## 🛠️ Developer Tools

### Swap CLI

Install globally:
```bash
dotnet tool install -g Swap.CLI
```

**Commands:**
```bash
# Create new app from template
swap new MyApp --template swap-modular-monolith

# Event system tools
swap events list -p .
swap events validate -p .
swap events graph -p . --format mermaid

# Generate code (planned for v0.4.0)
swap generate controller Products
swap generate module Orders
```

### Development Dashboard

Available in Development environment:
- `/_swap/dev/events` — Visual dashboard with Mermaid graph
- `/_swap/dev/events.json` — Export chains as JSON
- `/_swap/dev/explain.json?event=...` — Preview event resolution

### Roslyn Analyzer

**Swap.Htmx.Analyzers** catches common mistakes:
- Magic strings in event names
- Unused event chains
- Invalid HTMX header usage

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
│   ├── swap-modular-monolith/  ✅ Modular, per-module ownership
│   └── generate/               📦 Component scaffolding (future)
└── tools/
    └── Swap.CLI/               ✅ Code generation + templates
```

---

## 🚀 Getting Started

### Quick Start (5 minutes)

1. **Install Swap CLI:**
   ```bash
   dotnet tool install -g Swap.CLI
   ```

2. **Create app:**
   ```bash
   swap new MyApp
   cd MyApp
   ```

3. **Run:**
   ```bash
   dotnet watch
   ```

4. **Open browser:**
   ```
   https://localhost:5001
   ```

### Next Steps

- **Read [TEMPLATES.md](TEMPLATES.md)** to choose the right template
- **Read [EVENTS.md](EVENTS.md)** to understand the event system
- **Explore [examples](../testApps/)** for real patterns
- **Check the [wiki](https://jdtoon.github.io/swap/)** for guides and best practices

---

## 🔮 Roadmap

- **0.3.x** — Event system consolidation, documentation, and template polish
- **0.4.0** — Template documentation, security audit, component scaffolding
- **0.5.0** — Authentication templates and built-in auth patterns
- **Future** — WebSocket/SignalR support, validation framework, form generation

---

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

---

## 📜 License

Swap is open source under the [MIT License](../LICENSE).

---

## 🔗 Links

- **[GitHub](https://github.com/jdtoon/swap)**
- **[Wiki](https://jdtoon.github.io/swap/)**
- **[NuGet Packages](https://www.nuget.org/packages?q=owner:jdtoon)**
- **[Discussions](https://github.com/jdtoon/swap/discussions)**

---

**Built for developers who love ASP.NET Core and want to move fast without sacrificing code quality or team scalability.**

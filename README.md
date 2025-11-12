# Swap

> **Build modern web apps with HTMX + ASP.NET Core** — Fast, modular, zero complexity

[![GitHub License](https://img.shields.io/github/license/jdtoon/swap)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![NuGet Swap.CLI](https://img.shields.io/nuget/v/Swap.CLI?label=Swap.CLI&logo=nuget)](https://www.nuget.org/packages/Swap.CLI)

**Swap** is an HTMX-first framework for ASP.NET Core. Get production-ready templates, server-driven events, and modular architecture—no JavaScript framework required.

---

## ⚡ Quick Start

```bash
# Install the CLI
dotnet tool install -g Swap.CLI

# Create your app
swap new MyApp

# Run it
cd MyApp/src
libman restore
dotnet run
```

Open **http://localhost:5000** — You're live! 🎉

---

## 🎯 Why Swap?

**Server-rendered UI** with **modern interactivity**—no build pipeline complexity, no massive JavaScript bundles, no state management hell.

- ✅ **Zero NPM** — LibMan handles HTMX & Bulma CSS
- ✅ **Event-driven** — Declarative server events → reactive UI
- ✅ **Testable** — HTMX-aware integration tests out of the box
- ✅ **Modular** — Scale from monolith → modules → microservices
- ✅ **Production-ready** — Docker, EF migrations, health checks included

---

## 📦 Choose Your Template

Swap includes **4 production-ready templates** for different needs:

### 1. swap-minimal ⚡ **Start Here**

Minimal HTMX starter. Perfect for learning or prototypes.

```bash
swap new MyApp --template swap-minimal
```

**What you get:** Single controller, toast notifications, Bulma CSS, HTMX 2.x, SwapController pattern.

**Best for:** Learning Swap, small projects, quick prototypes.

---

### 2. swap-monolith 🚀 **Default**

Full-featured monolith. Great for MVPs and small teams.

```bash
swap new MyApp
# or
swap new MyApp --template swap-monolith
```

**What you get:** Complete CRUD, EF Core, event chains, Docker Compose, integration tests, CLI generators.

**Best for:** MVPs, small-to-medium apps, rapid development.

---

### 3. swap-layered 🏗️ **Clean Architecture**

Multi-project clean architecture (Web, Application, Domain, Infrastructure).

```bash
swap new MyApp --template swap-layered
```

**What you get:** Clean separation of concerns, testable layers, repository pattern, domain events, full test suite.

**Best for:** Teams, long-term projects, enterprise apps, DDD.

---

### 4. swap-modular-monolith ⭐ **Recommended**

Modular monolith. Sweet spot between monolith and microservices.

```bash
swap new MyApp --template swap-modular-monolith
```

**What you get:** Independent modules (Contracts, Module, Web RCL), per-module migrations, Docker (Postgres/RabbitMQ), distributed events, production-ready structure.

**Best for:** Growing teams, complex domains, gradual migration to microservices.

---

## 🔥 Core Features

### SwapController — Automatic Page/Partial Detection

One method handles both full page and HTMX partial requests:

```csharp
public class ArticlesController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var articles = await _db.Articles.ToListAsync();
        return SwapView(articles); // Auto-detects full vs partial!
    }
}
```

### Server Events — Declarative UI Reactions

Decouple your UI with server-driven events:

```csharp
// Define chains once (Program.cs)
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(
        SwapEvents.Entity.Created("article"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});

// Controllers just emit domain events
await _events.EmitAsync(SwapEvents.Entity.Created("article"), new { id = 42 });

// UI listens independently (Views)
<div hx-get="/Articles/List"
     hx-trigger="load, ui.refreshList from:body">
</div>
```

**No tight coupling.** Add new UI reactions without changing controllers.

### Toast Notifications

Built-in user feedback:

```csharp
Response.ShowSuccessToast("Article created!");
Response.ShowErrorToast("Something went wrong");
Response.ShowWarningToast("Please review");
Response.ShowInfoToast("FYI...");
```

### HTMX-Aware Testing

Test your partials like a user would:

```csharp
public class ArticleTests : IClassFixture<HtmxTestFixture<Program>>
{
    [Fact]
    public async Task CreateArticle_RefreshesListAndShowsToast()
    {
        var response = await _client.HtmxPostAsync("/Articles/Create", form);
        
        response.AssertSuccess();
        await response.AssertPartialViewAsync();
        await response.AssertHxTriggerAsync("articleCreated");
        await response.AssertElementContainsAsync("h2", "Hello");
    }
}
```

---

## 📚 Learn More

| Resource | Description |
|----------|-------------|
| **[Documentation](https://jdtoon.github.io/swap/)** | Full guides, API reference, examples |
| **[EVENTS.md](docs/EVENTS.md)** | Event chains, resolution modes, dev tools |
| **[TEMPLATES.md](docs/TEMPLATES.md)** | Template comparison, migration paths |
| **[PRODUCT.md](docs/PRODUCT.md)** | Architecture, design decisions, philosophy |
| **[Wiki](https://jdtoon.github.io/swap/)** | Getting started, tutorials, best practices |

---

## 🎮 Live Demos

Explore **two production-quality demo applications** showcasing different architectural patterns:

### [ProjectHub](demo/projecthub/) — Modular Monolith ⭐

Modular architecture with SSE streaming, cross-module events, per-module migrations, and Docker stack.

```bash
cd demo/projecthub
docker compose up -d
dotnet run --project src/Web
# Open http://localhost:5073
```

### [TaskFlow](demo/taskflow/) — HTMX Patterns

Core HTMX patterns, event chains, and integration testing in a focused demo.

```bash
cd demo/taskflow/src
libman restore && dotnet run
# Open http://localhost:5000
```

**See [demo/README.md](demo/README.md) for detailed comparison.**

---

## 🛠️ What's Included

### Swap.Htmx

HTMX integration for ASP.NET Core:
- SwapController (automatic full/partial detection)
- Fluent header API (HX-Trigger, HX-Redirect, etc.)
- Event chains (domain → UI)
- Toast notifications
- Server events (RabbitMQ support)

### Swap.Modularity

Lightweight module system:
- Automatic discovery
- Dependency ordering
- RCL support (Razor Class Libraries)
- Event contributors
- Endpoint registration

### Swap.Testing

HTMX-aware integration testing:
- Fluent assertions
- HTMX header verification
- Partial vs full page testing
- Form submission helpers
- Snapshot testing with scrubbers

### Swap.CLI

Productivity tools:
- Project scaffolding (4 templates)
- Code generators (models, controllers, CRUD)
- Event chain validation
- Migration helpers

---

## 🎨 The Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | HTMX 2.x, Bulma CSS |
| **Backend** | ASP.NET Core 9 MVC |
| **Database** | EF Core 9 (SQLite/Postgres/SQL Server) |
| **Events** | Swap Event System |
| **Testing** | xUnit, Swap.Testing |
| **Container** | Docker + Docker Compose |

---

## 🚀 Next Steps

1. **Install the CLI:** `dotnet tool install -g Swap.CLI`
2. **Create a project:** `swap new MyApp --template swap-modular-monolith`
3. **Run it:** `cd MyApp/src/Web && libman restore && dotnet run`
4. **Explore:** Visit `/Demo` for event system playground
5. **Build:** Add your features with `swap g r Product --fields "Name:string Price:decimal"`

---

## 🤝 Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## 📄 License

MIT License. See [LICENSE](LICENSE) for details.

---

**Build fast, stay modular, ship quality.** 🎉

[📖 Docs](https://jdtoon.github.io/swap/) • [🐙 GitHub](https://github.com/jdtoon/swap) • [💬 Discussions](https://github.com/jdtoon/swap/discussions)

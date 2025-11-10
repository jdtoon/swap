# Swap CLI

[![NuGet: Swap.CLI](https://img.shields.io/nuget/v/Swap.CLI.svg?label=Swap.CLI&color=0078d4)](https://www.nuget.org/packages/Swap.CLI)

The Swap CLI creates ASP.NET Core + HTMX applications from templates and provides utilities for the Swap framework.

---

## 🎯 What Swap CLI Does

- 📦 **Create Projects** - Generate new apps from production-ready templates
- 🎨 **HTMX Shell** - Add middleware for HTMX request handling
- 📊 **Event System** - Analyze, validate, and visualize event chains

---

## ⚡ Quick Start

### Install

```bash
dotnet tool install --global Swap.CLI
swap --version
```

### Create Your First Project

```bash
swap new MyApp
cd MyApp
dotnet ef database update
dotnet run
```

Visit `https://localhost:5001` - Your HTMX-powered application is running! 🎉

---

## 📋 CLI Commands

### `swap new <name>`

Create a new ASP.NET Core + HTMX application.

```bash
swap new MyApp

# Choose template
swap new MyApp --template swap-modular-monolith

# Choose database
swap new MyApp --database sqlite      # SQLite (default)
swap new MyApp --database sqlserver   # SQL Server
swap new MyApp --database postgres    # PostgreSQL

# Use local NuGet feed (for framework development)
swap new MyApp --local-nuget
```

**Options:**
- `--template, -t` - Template to use (swap-monolith, swap-layered, swap-modular-monolith)
- `--database, -d` - Database provider (sqlite, sqlserver, postgres)
- `--output, -o` - Output directory (default: current directory)
- `--local-nuget` - Use local NuGet feed for framework packages
- `--skip-setup` - Skip post-creation setup (npm install, migrations)

**What it generates:**
- Complete ASP.NET Core MVC project
- Entity Framework Core with chosen database
- DaisyUI + Tailwind CSS configuration
- Swap.Htmx event system configured
- Sample TodoItem CRUD (monolith template)
- Ready to run immediately

---

### `swap generate htmx-shell`

Add HTMX shell middleware to your application.

```bash
# Add middleware
swap g htmx-shell

# Add middleware with global hx-boost
swap g htmx-shell --add-boost
```

**What it does:**
- Adds `SwapHtmxShellMiddleware` to catch full-page responses to HTMX requests
- Optionally adds `hx-boost="true"` to layout for progressive enhancement
- Helps prevent common HTMX mistakes during development

**Options:**
- `--add-boost` - Add hx-boost="true" to _Layout.cshtml
- `--force` - Overwrite existing middleware configuration

---

### `swap events`

Inspect and validate event chains in your application.

```bash
# List all event chains
swap events list -p .

# Validate event chains (check for cycles, undefined events)
swap events validate -p .

# Export event chain graph
swap events graph -p . --format mermaid
swap events graph -p . --format dot --output chains.dot

# Query running application
swap events from-server --url http://localhost:5000
```

**Commands:**
- `list` - Show all registered event chains
- `validate` - Check for undefined events and circular dependencies
- `graph` - Generate visual representation (Mermaid or DOT format)
- `from-server` - Query running application for live event chains

---

## 🏗️ Framework Architecture

Swap provides three core libraries:

### **Swap.Htmx** - HTMX Integration & Event System

- `SwapController` - Auto-detects HTMX vs full-page requests
- `SwapView()` - Returns partial or full view automatically
- Event bus with declarative chain resolution
- Fluent header API for HTMX headers
- Toast notifications built-in

### **Swap.Modularity** - Modular Monolith System

- `IModule` contract for defining modules
- Automatic discovery and dependency ordering
- Per-module endpoints, services, and event chains
- RCL (Razor Class Library) support

### **Swap.Testing** - HTMX Testing Framework

- `HtmxTestClient` for HTMX-aware requests
- DOM assertions with CSS selectors
- Header assertions (`HX-Trigger`, `HX-Redirect`, etc.)
- Snapshot testing with scrubbers

---

## 📦 Templates

### Monolith (swap-monolith)

**Single deployable for rapid development**

- 1 project
- Best for: MVPs, solo developers, prototypes
- Includes: Swap.Htmx

### Layered (swap-layered)

**Multi-project architecture for teams**

- 4 projects (Web, Application, Domain, Infrastructure)
- Best for: Teams of 3-5, enterprise apps
- Includes: Swap.Htmx

### Modular Monolith (swap-modular-monolith)

**Full modular architecture**

- Host + independent module projects
- Best for: Large teams (5+), complex domains
- Includes: Swap.Htmx, Swap.Modularity, Swap.Testing
- Features: Per-module databases, RabbitMQ event distribution

---

## 🧪 Swap.Testing (HTMX Testing Framework)

A fluent testing library purpose-built for HTMX applications.

**Key Features:**
- 🎯 **HTMX-Aware Client** - `HtmxGetAsync`, `HtmxPostAsync` with automatic HX-Request headers
- 🔍 **Rich Assertions** - `AssertPartialViewAsync`, `AssertHxGetAsync`, `AssertHxTriggered`
- 📸 **Snapshot Testing** - `AssertMatchesSnapshotAsync` with `UPDATE_SNAPSHOTS=true`
- ✅ **Validation Helpers** - `AssertHasValidationErrorsAsync`, `AssertFieldValidationErrorAsync`
- 🔄 **Form Helpers** - `SubmitFormAsync`, `FollowHxRedirectAsync`
- 🧹 **Snapshot Scrubbers** - Auto-replace GUIDs/timestamps/tokens for stable snapshots

**Quick Example:**
```csharp
public class PostControllerTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;
    public PostControllerTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task Create_Form_IsPartial_WithHtmxAttributes()
    {
        var resp = await _client.HtmxGetAsync("/posts/create");
        resp.AssertSuccess();
        await resp.AssertPartialViewAsync();
        await resp.AssertHxPostAsync("form", "/posts");
        await resp.AssertHxTargetAsync("form", "#post-list");
    }
}
```

**See also:**
- [Swap.Testing Framework Guide](../framework/Swap.Testing/README.md)
- [Testing Framework Wiki](https://jdtoon.github.io/swap/docs/features/testing-framework)

---

## 📊 Event System

The Swap event system enables declarative UI reactions to domain events.

**Configure in Program.cs:**
```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});

app.UseSwapHtmx();
```

**Emit events from controllers:**
```csharp
public class ProductsController : SwapController
{
    private readonly ISwapEventBus _events;

    public async Task<IActionResult> Create(ProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), product);
        Response.ShowSuccessToast("Product created!");
        return SwapView(product);
    }
}
```

**Listen in markup:**
```html
<div id="product-list" 
     hx-get="/products/list" 
     hx-trigger="load, productCreated from:body" 
     hx-swap="outerHTML">
</div>
```

---

## 🛠️ Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/jdtoon/swap.git
cd swap

# Pack framework and CLI locally
./scripts/pack-local.ps1    # Windows
./scripts/pack-local.sh     # Linux/Mac

# Install CLI from local feed
./scripts/reinstall-cli.ps1    # Windows
./scripts/reinstall-cli.sh     # Linux/Mac
```

### Project Structure

```
tools/Swap.CLI/
├── Commands/
│   ├── NewCommand.cs              # swap new
│   ├── EventsCommand.cs           # swap events
│   └── GenerateHtmxShellCommand.cs # swap generate htmx-shell
├── Infrastructure/
│   ├── TemplateEngine.cs
│   └── ProjectScanner.cs
└── Program.cs
```

---

## 📚 Documentation

- **[Getting Started](https://jdtoon.github.io/swap/)** - Complete setup guide
- **[CLI Reference](https://jdtoon.github.io/swap/docs/cli/overview)** - All commands and options
- **[Event System](https://jdtoon.github.io/swap/docs/features/event-system)** - Event system guide

---

## 🛠️ Advanced Usage

### Template Selection

Choose the template that matches your project needs:

```bash
# Monolith - Single project (default)
swap new MyApp --template swap-monolith --database sqlite
```

**What you get:**
- `AddSwapHtmx(...)` with event chain configuration
- Middleware: `UseSwapHtmxShell()` and `UseSwapHtmx()`
- `Events/SwapEventChains.cs` using `EventNames.*` constants
- Dev-only endpoints at `/_swap/dev/events` and `/_swap/dev/events.json`

```bash
# Layered - Multi-project solution
swap new MyApp --template layered --database sqlite
```

**What you get:**
- Solution with projects: Web, Application, Domain, Infrastructure
- Web registers `AddSwapHtmx(...)` and event middleware
- Event chains: `Web/Events/SwapEventChains.cs` mapping domain → UI events
- Post-create (automated unless `--skip-setup`): npm/libman/CSS in Web/, EF migrations

```bash
# Modular Monolith - Module-based architecture
swap new MyApp --template swap-modular-monolith
```

**What you get:**
- Host app + modules as independent assemblies
- Per-module structure: Contracts, Module (services/endpoints), Web RCL (UI)
- Provider-specific migrations: Each module owns its database layer
- Swap.Modularity for deterministic composition
- Docker Compose: Postgres, RabbitMQ for distributed events

### Events Inspection

Inspect your event chains from source or from a running app:

```bash
# Source scan (resolves EventNames constants)
swap events list -p .

# From running server (Development-only endpoint)
swap events from-server --url http://localhost:5000

# Validate names and cycles
swap events validate -p .

# Graph output (Mermaid or DOT)
swap events graph -p . --format mermaid
swap events graph -p . --format dot --output chains.dot
```

---

## 💬 Community

- **Documentation**: https://jdtoon.github.io/swap/
- **GitHub Issues**: https://github.com/jdtoon/swap/issues
- **GitHub Discussions**: https://github.com/jdtoon/swap/discussions

For questions or feedback, open an [issue](https://github.com/jdtoon/swap/issues)!

---

## 📜 License

Swap is open source under the [MIT License](../../LICENSE).

---

**Built for developers who want HTMX-first ASP.NET Core applications with minimal ceremony.**

# Swap Templates

**Production-ready starting points for modern ASP.NET Core + HTMX applications.**

Swap includes three battle-tested templates, each designed for different team sizes and architectural needs. Every template comes with HTMX, Tailwind CSS, DaisyUI, EF Core, Docker, and comprehensive integration tests.

---

## 🎯 Template Comparison

| Feature | Monolith | Layered | Modular Monolith |
|---------|----------|---------|------------------|
| **Projects** | 1 | 4 | Host + Modules |
| **Best For** | MVPs, Solo | Teams, Enterprise | Teams, Scalability |
| **Complexity** | ⭐ Low | ⭐⭐ Medium | ⭐⭐⭐ High |
| **Team Size** | 1-2 | 3-5 | 5+ |
| **Module Boundaries** | No | Logical | Physical |
| **Database Strategy** | Single DB | Single DB | Per-Module |
| **Event System** | UI Events | UI Events | UI + Server Events |
| **RabbitMQ** | Optional | Optional | Recommended |
| **Docker Compose** | ✅ | ✅ | ✅ |
| **Integration Tests** | ✅ | ✅ | ✅ |
| **Code Generation** | ✅ | ✅ | ✅ |

---

## 📦 Templates

### Monolith (swap-monolith)

**Single deployable for rapid development. Perfect for MVPs and smaller projects.**

#### When to Use

- **Solo developers** or small teams (1-2 people)
- **MVPs** that need to ship fast
- **Proof of concepts** or prototypes
- **Simple applications** with limited complexity
- **Learning HTMX** with Swap framework

#### Structure

```
MyApp/
├── MyApp.csproj
├── Program.cs
├── appsettings.json
├── docker-compose.yml
├── Controllers/
│   ├── HomeController.cs
│   ├── ProductsController.cs
│   └── OrdersController.cs
├── Models/
│   ├── Product.cs
│   └── Order.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── _Toast.cshtml
│   ├── Home/
│   ├── Products/
│   └── Orders/
├── wwwroot/
│   ├── css/
│   │   ├── input.css
│   │   └── output.css
│   └── js/
└── Tests/
    └── MyApp.Tests.csproj
```

#### Key Features

- **Single Project**: All code in one place—no layer boundaries
- **EF Core**: SQLite (dev), SQL Server/PostgreSQL (prod)
- **HTMX + DaisyUI**: Interactive UI without JavaScript frameworks
- **Swap.Htmx**: Event system, SwapController, fluent headers
- **Docker Ready**: Dockerfile and docker-compose.yml included
- **Integration Tests**: Swap.Testing with example test suite

#### Quick Start

```bash
# Create new app
swap new MyApp --template swap-monolith

# Navigate to project
cd MyApp

# Run (applies migrations, builds CSS, starts dev server)
dotnet watch
```

Open browser: `https://localhost:5001`

#### Example: Adding a Feature

**1. Create Model:**
```csharp
// Models/Article.cs
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**2. Add to DbContext:**
```csharp
// Data/ApplicationDbContext.cs
public DbSet<Article> Articles => Set<Article>();
```

**3. Create Migration:**
```bash
dotnet ef migrations add AddArticles
dotnet ef database update
```

**4. Create Controller:**
```csharp
// Controllers/ArticlesController.cs
public class ArticlesController : SwapController
{
    private readonly ApplicationDbContext _context;
    private readonly ISwapEventBus _events;

    public async Task<IActionResult> Index()
    {
        var articles = await _context.Articles.ToListAsync();
        return SwapView(articles);
    }

    public async Task<IActionResult> Create(Article article)
    {
        _context.Articles.Add(article);
        await _context.SaveChangesAsync();
        
        await _events.EmitAsync(SwapEvents.Entity.Created("article"), article);
        return SwapView(article);
    }
}
```

**5. Create Views:**
```razor
@* Views/Articles/Index.cshtml *@
@model IEnumerable<Article>

<div id="article-list" hx-trigger="refreshList from:body" hx-get="/articles/list">
    @foreach (var article in Model)
    {
        <div class="card">
            <h2>@article.Title</h2>
            <p>@article.Content</p>
        </div>
    }
</div>
```

**Documentation:**
- **[README.md](../templates/swap-monolith/README.md.template)** — Template overview and quickstart
- **[DEVELOPMENT.md](../templates/swap-monolith/docs/DEVELOPMENT.md.template)** — Development workflow and debugging
- **[DEPLOYMENT.md](../templates/swap-monolith/docs/DEPLOYMENT.md.template)** — Production deployment guide
- **[EVENTS.md](../templates/swap-monolith/docs/EVENTS.md.template)** — Event system deep dive

---

### Layered (swap-layered)

**Multi-project architecture (Web, Application, Domain, Infrastructure) for scaling and team organization.**

#### When to Use

- **Growing teams** (3-5 people) that need clear separation
- **Enterprise applications** with complex business logic
- **Long-term projects** that will scale over time
- **Clean Architecture** advocates
- **Team ownership** of specific layers

#### Structure

```
MyApp/
├── MyApp.sln
├── docker-compose.yml
├── src/
│   ├── MyApp.Web/              # Presentation Layer
│   │   ├── MyApp.Web.csproj
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   ├── Views/
│   │   └── wwwroot/
│   ├── MyApp.Application/      # Application Layer
│   │   ├── MyApp.Application.csproj
│   │   ├── Services/
│   │   ├── DTOs/
│   │   └── Interfaces/
│   ├── MyApp.Domain/           # Domain Layer
│   │   ├── MyApp.Domain.csproj
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Events/
│   └── MyApp.Infrastructure/   # Infrastructure Layer
│       ├── MyApp.Infrastructure.csproj
│       ├── Data/
│       ├── Repositories/
│       └── Migrations/
└── tests/
    └── MyApp.Tests/
```

#### Key Features

- **4-Layer Clean Architecture**: Web → Application → Domain → Infrastructure
- **Dependency Inversion**: Domain has no dependencies; Infrastructure references Domain
- **Provider-Specific EF Core**: Each project can use different database providers
- **Separation of Concerns**: Business logic in Application, entities in Domain, data in Infrastructure
- **Swap.Htmx Integration**: Event system wired across layers
- **Docker Multi-Stage Build**: Optimized for production deployment

#### Quick Start

```bash
# Create new app
swap new MyApp --template swap-layered

# Navigate to solution
cd MyApp

# Run
dotnet run --project src/MyApp.Web
```

Open browser: `https://localhost:5001`

#### Example: Adding a Feature

**1. Define Domain Entity:**
```csharp
// src/MyApp.Domain/Entities/Product.cs
namespace MyApp.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

**2. Create Application Service:**
```csharp
// src/MyApp.Application/Services/ProductService.cs
namespace MyApp.Application.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(CreateProductDto dto);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Product> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price
        };

        await _repository.AddAsync(product);
        return product;
    }
}
```

**3. Implement Repository:**
```csharp
// src/MyApp.Infrastructure/Repositories/ProductRepository.cs
namespace MyApp.Infrastructure.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task AddAsync(Product product);
}

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }
}
```

**4. Wire in Web Layer:**
```csharp
// src/MyApp.Web/Controllers/ProductsController.cs
namespace MyApp.Web.Controllers;

public class ProductsController : SwapController
{
    private readonly IProductService _service;
    private readonly ISwapEventBus _events;

    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        await _events.EmitAsync(SwapEvents.Entity.Created("product"), product);
        return SwapView(product);
    }
}
```

**Documentation:**
- **[README.md](../templates/swap-layered/README.md.template)** — Template overview and quickstart
- **[ARCHITECTURE.md](../templates/swap-layered/docs/ARCHITECTURE.md.template)** — Clean architecture deep dive
- **[DEVELOPMENT.md](../templates/swap-layered/docs/DEVELOPMENT.md.template)** — Cross-layer development workflow
- **[DEPLOYMENT.md](../templates/swap-layered/docs/DEPLOYMENT.md.template)** — Production deployment guide
- **[EVENTS.md](../templates/swap-layered/docs/EVENTS.md.template)** — Event flow in layered architecture

---

### Modular Monolith (swap-modular-monolith) ⭐ **Recommended**

**Single deployable with module boundaries and per-module ownership. The sweet spot between monolith and microservices.**

#### When to Use

- **Growing teams** (5+ people) that need ownership boundaries
- **Complex domains** with distinct business capabilities
- **Long-term scalability** without microservice complexity
- **Incremental extraction** path to microservices
- **Distributed events** for cross-module communication

#### Structure

```
MyApp/
├── MyApp.sln
├── docker-compose.yml
├── src/
│   ├── MyApp.Host/                      # Host Application
│   │   ├── MyApp.Host.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── wwwroot/
│   └── Modules/
│       ├── Products/
│       │   ├── Products.Contracts/      # Public contracts
│       │   │   ├── Products.Contracts.csproj
│       │   │   ├── IProductService.cs
│       │   │   └── Events/
│       │   ├── Products/                # Module implementation
│       │   │   ├── Products.csproj
│       │   │   ├── ProductsModule.cs
│       │   │   ├── Services/
│       │   │   └── Data/
│       │   └── Products.Web/            # Razor Class Library
│       │       ├── Products.Web.csproj
│       │       ├── Controllers/
│       │       ├── Views/
│       │       └── wwwroot/
│       └── Orders/
│           ├── Orders.Contracts/
│           ├── Orders/
│           └── Orders.Web/
└── tests/
    └── MyApp.Tests/
```

#### Key Features

- **Physical Module Boundaries**: Each module is a separate NuGet package
- **Per-Module Ownership**: Teams own entire vertical slices
- **Contracts-First**: Modules expose contracts; implementation is private
- **Per-Module Databases**: Each module owns its data (or shared if needed)
- **Swap.Modularity**: Automatic discovery, dependency validation, deterministic ordering
- **Server Events (RabbitMQ)**: Distributed event chains across modules
- **Razor Class Libraries**: Each module includes its own UI
- **Independent Migrations**: Each module manages its own database schema

#### Quick Start

```bash
# Create new app
swap new MyApp --template swap-modular-monolith

# Navigate to solution
cd MyApp

# Start dependencies (PostgreSQL, RabbitMQ)
docker-compose up -d

# Run host
dotnet run --project src/MyApp.Host
```

Open browser: `https://localhost:5001`

#### Example: Adding a Module

**1. Create Contracts Project:**
```bash
cd src/Modules
dotnet new classlib -n Inventory.Contracts
```

```csharp
// Inventory.Contracts/IInventoryService.cs
namespace Inventory.Contracts;

public interface IInventoryService
{
    Task<int> GetStockAsync(int productId);
    Task ReserveAsync(int productId, int quantity);
}

// Inventory.Contracts/Events/InventoryLow.cs
public record InventoryLow(int ProductId, int CurrentStock);
```

**2. Create Module Implementation:**
```bash
dotnet new classlib -n Inventory
dotnet add Inventory reference Inventory.Contracts
```

```csharp
// Inventory/InventoryModule.cs
using Swap.Modularity.Abstractions;

namespace Inventory;

public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";
    public IReadOnlyList<string> DependsOn => new[] { "Products" };

    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddDbContext<InventoryDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("Inventory")));
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // No direct endpoints—accessed via contracts
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // Listen for order.created from Orders module
        registrar.Register("order.created", async (OrderCreated evt) =>
        {
            var stock = await _inventoryService.GetStockAsync(evt.ProductId);
            
            if (stock < 10)
            {
                await _events.EmitAsync(new InventoryLow(evt.ProductId, stock));
            }
        });
    }
}
```

**3. Create Web RCL:**
```bash
dotnet new razorclasslib -n Inventory.Web
dotnet add Inventory.Web reference Inventory.Contracts
```

```csharp
// Inventory.Web/Controllers/InventoryController.cs
namespace Inventory.Web.Controllers;

public class InventoryController : SwapController
{
    private readonly IInventoryService _service;

    public async Task<IActionResult> Index()
    {
        var stock = await _service.GetStockAsync(productId: 1);
        return SwapView(stock);
    }
}
```

**4. Reference in Host:**
```xml
<!-- MyApp.Host.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Modules\Inventory\Inventory.csproj" />
  <ProjectReference Include="..\Modules\Inventory.Web\Inventory.Web.csproj" />
</ItemGroup>
```

**5. Add Connection String:**
```json
{
  "ConnectionStrings": {
    "Inventory": "Host=localhost;Database=MyApp_Inventory;Username=postgres;Password=postgres"
  }
}
```

**Documentation:**
- **[README.md](../templates/swap-modular-monolith/README.md.template)** — Template overview and quickstart
- **[Module authoring guides](../templates/swap-modular-monolith/src/Modules/README.md)** — How to create and structure modules
- **[Event chain examples](../templates/swap-modular-monolith/docs/EVENTS.md)** — Cross-module event patterns

---

## 🔀 Migration Paths

### Monolith → Layered

1. **Create layered solution**:
   ```bash
   swap new MyApp.Layered --template swap-layered
   ```

2. **Move models** to `Domain/Entities`

3. **Extract services** to `Application/Services`

4. **Move DbContext** to `Infrastructure/Data`

5. **Update controllers** to use application services

6. **Run tests** to verify everything still works

---

### Layered → Modular Monolith

1. **Create modular solution**:
   ```bash
   swap new MyApp.Modular --template swap-modular-monolith
   ```

2. **Identify module boundaries** (Products, Orders, etc.)

3. **Create module structure** (Contracts, Module, Web RCL)

4. **Move domain entities** to module projects

5. **Move controllers/views** to Web RCLs

6. **Define contracts** for inter-module communication

7. **Wire event chains** for cross-module interactions

8. **Test module isolation** and event propagation

---

### Modular Monolith → Microservices

1. **Extract module** to standalone ASP.NET Core app

2. **Replace in-process events** with RabbitMQ/HTTP

3. **Separate database** for the extracted module

4. **Update host** to call module via HTTP/gRPC

5. **Deploy independently**

6. **Repeat** for other modules as needed

---

## 🛠️ Common Tasks

### Add Event Chain

**All Templates:**
```csharp
// Program.cs
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(
        SwapEvents.Entity.Created("product"),
        SwapEvents.UI.RefreshList,
        SwapEvents.UI.ShowToast
    );
});
```

---

### Add RabbitMQ (Modular Monolith)

**1. Update appsettings.json:**
```json
{
  "Swap": {
    "ServerEvents": {
      "Enabled": true,
      "ConnectionString": "amqp://guest:guest@localhost:5672",
      "ExchangeName": "myapp.events",
      "QueuePrefix": "MyApp"
    }
  }
}
```

**2. Enable in Program.cs:**
```csharp
builder.Services.AddSwapServerEventChainsFromConfiguration(
    builder.Configuration,
    "Swap:ServerEvents"
);
```

**3. Start RabbitMQ:**
```bash
docker-compose up -d rabbitmq
```

---

### Add Database Provider

**Monolith/Layered:**
```bash
# SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**Update Program.cs:**
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);
```

**Modular Monolith (per module):**
```csharp
// ProductsModule.cs
services.AddDbContext<ProductsDbContext>(opts =>
    opts.UseNpgsql(config.GetConnectionString("Products"))
);
```

---

### Run Migrations

**Monolith:**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Layered:**
```bash
dotnet ef migrations add InitialCreate --project src/MyApp.Infrastructure --startup-project src/MyApp.Web
dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Web
```

**Modular Monolith (per module):**
```bash
# Products module
dotnet ef migrations add InitialCreate \
  --project src/Modules/Products/Products \
  --startup-project src/MyApp.Host \
  --context ProductsDbContext

dotnet ef database update \
  --project src/Modules/Products/Products \
  --startup-project src/MyApp.Host \
  --context ProductsDbContext
```

---

## 🧪 Testing

All templates include **Swap.Testing** with comprehensive test suites.

**Run Tests:**
```bash
dotnet test
```

**Example Test (all templates):**
```csharp
using Swap.Testing;

public class ProductTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public ProductTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task CreateProduct_EmitsEvents()
    {
        var response = await _client.PostFormAsync("/products/create", new { 
            name = "Widget",
            price = 19.99
        });

        response.AssertSuccess();
        await response.AssertHxTriggerAsync("productCreated", "refreshList");
    }
}
```

---

## 📚 Additional Resources

- **[PRODUCT.md](PRODUCT.md)** — Overview of Swap framework and the three pillars
- **[EVENTS.md](EVENTS.md)** — Complete event system guide
- **[Wiki](https://jdtoon.github.io/swap/)** — Getting started guides, tutorials, examples
- **[GitHub](https://github.com/jdtoon/swap)** — Source code, issues, discussions
- **[NuGet](https://www.nuget.org/packages?q=owner:jdtoon)** — Published packages

---

## 🎯 Choosing the Right Template

**Start with Monolith if:**
- You're a solo developer or small team (1-2 people)
- You're building an MVP or prototype
- You want to learn HTMX and Swap quickly
- You need to ship fast

**Use Layered if:**
- You have a growing team (3-5 people)
- You need clear separation of concerns
- You're building a complex application
- You want clean architecture patterns

**Use Modular Monolith if:**
- You have a larger team (5+ people)
- You need ownership boundaries
- You want scalability without microservice complexity
- You need cross-module event communication
- You plan to extract microservices later

**Still unsure?** Start with **Monolith**, migrate to **Layered** when team grows, then **Modular Monolith** when you need module boundaries.

---

**All templates are production-ready, battle-tested, and designed to grow with your team.**

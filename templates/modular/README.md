# NetMX Modular Monolith Template

**Module-Based Architecture** - Reusable modules with clear boundaries

## 💡 Best For

- ✅ Large applications (20+ entities)
- ✅ Multiple teams working independently
- ✅ Modules that need reusability (across projects)
- ✅ Applications requiring clear boundaries
- ✅ Teams wanting to package modules as NuGet

## 📦 What You Get

- **ASP.NET Core 9.0** - Latest .NET stack
- **SQLite Database** - Zero setup for development
- **Entity Framework Core** - Code-first migrations
- **HTMX-First UI** - Server-rendered HTML with interactivity
- **Bulma CSS** - Clean, modern styling
- **Docker Ready** - Dockerfile + docker-compose.yml included
- **NetMX Framework** - DDD patterns, HTMX helpers, events
- **Modular Architecture** - 4-layer modules with clear boundaries

## 🏗️ Structure

```
MyApp/
├── MyApp.sln
├── src/
│   └── MyApp.Web/                      # Host app (THIN)
│       ├── Data/
│       │   └── AppDbContext.cs         # Aggregates module DbSets
│       ├── wwwroot/
│       ├── Program.cs                  # Wires up modules
│       └── appsettings.json
├── modules/                             # ⭐ MODULES LIVE HERE
│   ├── Identity/                       # 4-layer module
│   │   ├── Identity.sln
│   │   ├── Identity.Core/              # Domain entities
│   │   ├── Identity.Contracts/         # DTOs, interfaces
│   │   ├── Identity.Application/       # Services
│   │   └── Identity.Web/               # Controllers, views
│   │       ├── Extensions/
│   │       │   └── IdentityServiceCollectionExtensions.cs
│   │       └── Events/
│   │           └── IdentityEventDefinitions.cs
│   ├── Catalog/
│   │   ├── Catalog.Core/
│   │   ├── Catalog.Contracts/
│   │   ├── Catalog.Application/
│   │   └── Catalog.Web/
│   └── Orders/
├── Dockerfile
├── docker-compose.yml
└── nuget.config
```

**Key Characteristic**: Modules are **separate projects** with 4-layer architecture, **reusable** across apps

## 🚀 Quick Start

### 1. Prerequisites

- .NET 9.0 SDK
- Docker Desktop (optional - for deployment)
- NetMX CLI: `dotnet tool install --global NetMX.CLI`

### 2. Create Your App

```bash
netmx new modular MyShop
cd MyShop
```

### 3. Add Pre-Built Modules

```bash
netmx add module Identity          # Copies Identity module
netmx add module Authorization     # Copies Authorization module
```

### 4. Create Custom Module

```bash
netmx create module Catalog        # Scaffolds NEW module
cd modules/Catalog/Catalog.Web
netmx generate feature Product     # Generates in module
```

**This creates** (in `Catalog` module):
- `Catalog.Core/Entities/Product.cs` - Domain entity
- `Catalog.Contracts/Dtos/ProductDto.cs` - DTOs
- `Catalog.Application/Services/ProductService.cs` - Business logic
- `Catalog.Web/Controllers/ProductController.cs` - HTMX controller
- `Catalog.Web/Views/Product/` - Views

### 5. Wire Up Module

```csharp
// src/MyApp.Web/Program.cs (CLI does this automatically)
using Catalog.Web.Extensions;

// Add services
builder.Services.AddCatalog();

// Add events (if using events)
// builder.Services.AddSingleton<IEventRegistry, EventRegistry>();
// EventRegistry.Instance.AddCatalogEvents();
```

### 6. Add Module DbSets

```csharp
// src/MyApp.Web/Data/AppDbContext.cs
using Catalog.Core.Entities;

public DbSet<Product> Products => Set<Product>();
```

### 7. Create & Apply Migration

```bash
cd src/MyApp.Web
dotnet ef migrations add AddCatalog
dotnet ef database update
```

### 8. Run Your App

```bash
dotnet run
```

Navigate to `http://localhost:5263/Product`

## 🐳 Docker Deployment

### Build & Run

```bash
# Build image
docker build -t myshop:latest .

# Run with docker-compose
docker-compose up
```

### Access

- App: `http://localhost:8080`
- Health: `http://localhost:8080/health`

## 📊 When to Choose This Template

**Choose Modular if:**
- ✅ Large application (20+ entities)
- ✅ Multiple teams
- ✅ Need module reusability
- ✅ Want to package modules as NuGet
- ✅ Clear boundaries required

**Downgrade to Vertical Slice if:**
- ⚠️ Simpler organization preferred
- ⚠️ Single team, no reusability needs

**Upgrade to Microservices if:**
- ⚠️ Need independent deployments
- ⚠️ Scaling requirements
- ⚠️ Distributed architecture

## 🧩 Module Benefits

**Advantages**:
- ✅ **Reusable** - Use same module in multiple apps
- ✅ **Packageable** - Distribute as NuGet packages
- ✅ **Boundaries** - True separation (separate projects)
- ✅ **Team Independence** - Teams own modules
- ✅ **Testable** - Module-level testing

**Trade-offs**:
- ⚠️ More complex than monolith
- ⚠️ More projects to manage
- ⚠️ Requires discipline

## 🎯 HTMX Showcase

Navigate to `/Demo` to see 8 interactive HTMX examples:

1. **Click to Edit** - Inline editing
2. **Delete with Confirmation** - Surgical DOM updates
3. **Infinite Scroll** - Auto-load content
4. **Search with Debounce** - Live search (500ms delay)
5. **Tab Switching** - Dynamic tabs
6. **Form Validation** - Server-side validation
7. **Out-of-Band Updates** - Multi-section updates
8. **Lazy Loading** - Load when visible

📖 **Learn more**: [HTMX Patterns Guide](../../docs/HTMX-PATTERNS.md)

## 🔧 CLI Commands

```bash
# Add existing module
netmx add module Identity

# Create new module
netmx create module Catalog

# Generate feature in module
cd modules/Catalog/Catalog.Web
netmx generate feature Product

# Database commands
netmx db migrate AddCatalog
netmx db update
netmx db rollback
netmx db status
```

## 🏗️ Module Structure (4 Layers)

Each module follows **clean architecture**:

1. **Core** - Domain entities, value objects (no dependencies)
2. **Contracts** - DTOs, service interfaces (depends on Core)
3. **Application** - Service implementations (depends on Contracts)
4. **Web** - Controllers, views, UI (depends on Application)

## 📚 Learn More

- [NetMX Documentation](../../docs/)
- [Quick Start Guide](../../docs/QUICK-START.md)
- [Modular Architecture](../../docs/MODULAR-ARCHITECTURE.md)
- [Module Creation Guide](../../docs/TERMINOLOGY.md#-module)

## 💰 Pricing

**$99 one-time purchase**

Includes:
- Modular monolith template
- Pre-built modules (Identity, Authorization, Audit)
- 1 year of template updates
- Community support

---

**Scales from monolith to microservices** - Modules can be extracted to services later

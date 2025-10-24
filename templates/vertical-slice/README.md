# NetMX Vertical Slice Template

**Feature-Organized Architecture** - Each feature is self-contained

## 💡 Best For

- ✅ Medium applications (10-20 entities)
- ✅ Teams wanting clear feature boundaries
- ✅ Vertical slice architecture fans
- ✅ Applications with distinct features
- ✅ Better organization than flat monolith

## 📦 What You Get

- **ASP.NET Core 9.0** - Latest .NET stack
- **SQLite Database** - Zero setup, file-based database
- **Entity Framework Core** - Code-first migrations
- **HTMX-First UI** - Server-rendered HTML with interactivity
- **Bulma CSS** - Clean, modern styling
- **Docker Ready** - Dockerfile + docker-compose.yml included
- **NetMX Framework** - DDD patterns, HTMX helpers, events
- **Vertical Slices** - Each feature in its own folder

## 🏗️ Structure

```
MyApp/
├── MyApp.sln
├── src/
│   └── MyApp.Web/                      # ALL code in ONE project
│       ├── Features/                   # ⭐ Feature-organized
│       │   ├── Products/
│       │   │   ├── Product.cs          # Entity
│       │   │   ├── ProductDto.cs       # DTOs
│       │   │   ├── ProductService.cs   # Service
│       │   │   ├── ProductController.cs # Controller
│       │   │   └── Views/
│       │   │       └── Product/        # Views
│       │   ├── Orders/
│       │   │   ├── Order.cs
│       │   │   ├── OrderDto.cs
│       │   │   ├── OrderService.cs
│       │   │   ├── OrderController.cs
│       │   │   └── Views/
│       │   └── Customers/
│       ├── Data/
│       │   └── AppDbContext.cs         # Single DbContext
│       ├── wwwroot/
│       ├── Program.cs
│       └── appsettings.json
├── Dockerfile
├── docker-compose.yml
└── nuget.config
```

**Key Characteristic**: `Features/Product/` contains ALL Product code (entity, service, controller, views)

## 🚀 Quick Start

### 1. Prerequisites

- .NET 9.0 SDK
- Docker Desktop (optional - for deployment)
- NetMX CLI: `dotnet tool install --global NetMX.CLI`

### 2. Create Your App

```bash
netmx new vertical MyShop
cd MyShop
```

### 3. Generate Features

```bash
cd src/MyShop.Web
netmx generate feature Product
```

**This creates** (`Features/Product/`):
- `Product.cs` - Entity with DDD patterns
- `ProductDto.cs` - Data transfer objects
- `ProductService.cs` + `IProductService.cs` - Business logic
- `ProductController.cs` - HTMX-enabled controller
- `Views/Product/` - Index, _List, _Form

### 4. Add to DbContext

```csharp
// Data/AppDbContext.cs
public DbSet<Product> Products => Set<Product>();
```

### 5. Create & Apply Migration

```bash
dotnet ef migrations add AddProduct
dotnet ef database update
```

### 6. Run Your App

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

**Choose Vertical Slice if:**
- ✅ Need better organization (10-20 entities)
- ✅ Want each feature self-contained
- ✅ Team prefers vertical slice architecture
- ✅ Still want single-project simplicity

**Downgrade to Simple Monolith if:**
- ⚠️ Only < 10 entities
- ⚠️ Flat structure is simpler

**Upgrade to Modular if:**
- ⚠️ Need reusable modules
- ⚠️ Multiple teams
- ⚠️ Want NuGet packaging

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
# Generate CRUD feature (creates Features/Customer/)
netmx generate feature Customer

# Generate with options
netmx generate feature Order --migrate

# Database commands
netmx db migrate AddOrders
netmx db update
netmx db rollback
netmx db status
```

## 🏗️ Vertical Slice Benefits

**Advantages**:
- ✅ Feature cohesion - all related code together
- ✅ Easy to find code (look in feature folder)
- ✅ Easier to delete features (rm -rf Features/X/)
- ✅ Better than flat for medium apps

**Trade-offs**:
- ⚠️ Not as reusable as modules
- ⚠️ Still in one project (not true boundaries)

## 📚 Learn More

- [NetMX Documentation](../../docs/)
- [Quick Start Guide](../../docs/QUICK-START.md)
- [HTMX Patterns](../../docs/HTMX-PATTERNS.md)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)

## 💰 Pricing

**$49 one-time purchase**

Includes:
- Template with vertical slice organization
- 1 year of template updates
- Community support

---

**Perfect balance** between Simple Monolith and Modular Monolith

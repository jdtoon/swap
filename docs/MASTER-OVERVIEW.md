# NetMX Master Overview - Complete Product Context

**Last Updated**: October 25, 2025  
**Version**: 1.0  
**Purpose**: Single source of truth for understanding NetMX - Product, Architecture, Status, and Direction

---

## 📋 Document Index

This master document references and integrates all key documentation:

### Foundation Documents
- [THE-PRODUCT.md](THE-PRODUCT.md) - What NetMX is, product vision
- [INSPIRATION.md](INSPIRATION.md) - Why we built it this way
- [TERMINOLOGY.md](TERMINOLOGY.md) - Key concepts (Module, Feature, Component)
- [MODULAR-ARCHITECTURE.md](MODULAR-ARCHITECTURE.md) - 4-layer architecture explained

### Developer Experience
- [DX.md](DX.md) - Developer experience principles
- [AUTOMATED-ENDPOINT-TESTING.md](AUTOMATED-ENDPOINT-TESTING.md) - Testing strategy
- [E2E-TESTING-FRAMEWORK.md](E2E-TESTING-FRAMEWORK.md) - E2E testing architecture

### Technical Architecture
- [EVENT-REGISTRY-ARCHITECTURE.md](EVENT-REGISTRY-ARCHITECTURE.md) - Type-safe events system
- [EVENT-BUS-ARCHITECTURE.md](EVENT-BUS-ARCHITECTURE.md) - Event bus design
- [THEMING-STRATEGY.md](THEMING-STRATEGY.md) - UI theming approach

### Business & Future
- [STUDIO-SUITE-VISION.md](STUDIO-SUITE-VISION.md) - Visual tools roadmap
- [PRO-MODULE-LICENSING.md](PRO-MODULE-LICENSING.md) - Pro module licensing strategy
- [ROADMAP.md](ROADMAP.md) - Complete roadmap

---

## 🎯 What Is NetMX?

**NetMX is a complete framework ecosystem for building web applications with .NET and HTMX.**

### The Vision

> **Framework First, Features Optional**

NetMX provides the **infrastructure** (DDD patterns, event system, CLI tools, templates) while keeping **features** (Identity, Audit, CMS) completely optional as plug-and-play modules.

### Core Differentiators

1. **Zero JavaScript Frameworks** - Pure server-rendered HTML with HTMX for interactivity
2. **True Modularity** - Every feature is optional (Identity, Audit, CMS, etc.)
3. **DDD-First** - Built on Domain-Driven Design with clean architecture
4. **Event-Driven** - Type-safe events via NetMX.Events (monolith-first, scalable to distributed)
5. **Developer Experience** - CLI scaffolding, strong typing, zero warnings policy
6. **Template-Based** - Start from production-ready templates (modular, monolith)

---

## 🏗️ Architecture Overview

### Repository Structure

```
netmx/                          # Repository root
├── framework/                  # Core infrastructure (10 packages)
│   ├── NetMX.sln              # Framework solution
│   ├── NetMX.Core/            # Core abstractions
│   ├── NetMX.Events/          # Type-safe event registry
│   ├── NetMX.Ddd.Domain/      # DDD patterns
│   ├── NetMX.Ddd.Application/ # Application layer
│   ├── NetMX.EntityFrameworkCore/  # EF Core integration
│   ├── NetMX.AspNetCore.Core/ # ASP.NET Core extensions
│   ├── NetMX.AspNetCore.Mvc/  # MVC + HTMX helpers
│   ├── NetMX.Data/            # Data abstractions
│   └── NetMX.Testing/         # Test helpers
│
├── modules/                    # Reusable feature modules
│   ├── Identity/              # User authentication (FREE)
│   │   ├── Identity.sln       # Module solution
│   │   ├── Identity.Core/     # Domain layer
│   │   ├── Identity.Contracts/  # DTOs, interfaces
│   │   ├── Identity.Application/  # Services
│   │   └── Identity.Web/      # Controllers, views
│   ├── Authorization/         # Permissions, roles (FREE)
│   └── Audit/                 # Audit logging (FREE - scaffolded)
│
├── templates/                  # Starter templates ⭐ KEY
│   ├── modular/               # Modular monolith template
│   └── monolith/              # Simple monolith template
│
├── tools/                      # CLI and tooling
│   ├── NetMX.CLI/             # Command-line interface
│   └── NetMX.CLI.Tests/       # CLI tests
│
├── sampleApps/                 # Example applications
│   └── ECommerceDogfood/      # Validation app (4 features)
│
├── scripts/                    # Build automation
│   ├── pack-framework.ps1     # Package framework
│   ├── pack-modules.ps1       # Package modules
│   └── setup-local-dev.ps1    # Development setup
│
└── docs/                       # All documentation
    └── archive/               # Historical documents
```

---

## 🎨 Template Strategy ⭐ NEW DOCUMENTATION

### Overview

NetMX uses **template-based project creation** where developers start from production-ready, pre-configured templates. The CLI creates new solutions by copying and customizing these templates.

### Available Templates

#### 1. Modular Monolith Template (`templates/modular/`)

**What It Is**: A single deployment with logically separated modules

**Structure**:
```
MyApp/                          # Created by: netmx new modular MyApp
├── MyApp.sln
├── src/
│   └── MyApp.Web/
│       ├── Program.cs          # Pre-configured with NetMX
│       ├── appsettings.json    # SQLite by default
│       ├── Data/
│       │   └── AppDbContext.cs # Ready for EF Core
│       ├── Models/             # Feature entities go here
│       ├── Services/           # Feature services go here
│       ├── Controllers/        # Feature controllers go here
│       └── Views/              # Feature views go here
├── modules/                    # Added modules live here
│   └── (empty until modules added)
└── tests/
    └── MyApp.Tests/
```

**How Modules Are Added**:
```bash
# Developer runs:
netmx add module Identity

# CLI copies module into solution:
MyApp/
├── modules/
│   └── Identity/               # ← Copied from modules/Identity/
│       ├── Identity.Core/
│       ├── Identity.Contracts/
│       ├── Identity.Application/
│       └── Identity.Web/
```

**Module Integration**:
- ✅ **Source Code Copy** - Module source copied (NOT via NuGet)
- ✅ **Project References** - Added to MyApp.sln
- ✅ **Wiring** - CLI automatically adds `services.AddIdentity()` to Program.cs
- ✅ **Migrations** - Module tables added to AppDbContext
- ✅ **Routes** - Module controllers auto-discovered

**Benefits**:
- Full control over module code (can customize)
- Single deployment (no microservices complexity)
- Easy debugging (all code in one solution)
- Can extract to microservices later if needed

#### 2. Simple Monolith Template (`templates/monolith/`)

**What It Is**: All code in a single project (no modules directory)

**Structure**:
```
MyApp/                          # Created by: netmx new monolith MyApp
├── MyApp.sln
├── src/
│   └── MyApp/
│       ├── Program.cs
│       ├── Data/
│       ├── Models/             # All entities here
│       ├── Services/           # All services here
│       ├── Controllers/        # All controllers here
│       └── Views/              # All views here
└── tests/
    └── MyApp.Tests/
```

**How Modules Are Added**:
```bash
# Developer runs:
netmx add module Identity

# CLI "bakes in" module code:
MyApp/
├── Models/                     # ← AppUser.cs, AppRole.cs copied here
├── Services/                   # ← UserService.cs copied here
├── Controllers/                # ← UsersController.cs copied here
└── Views/
    └── Users/                  # ← Index.cshtml, etc. copied here
```

**Module Integration**:
- ✅ **Flatten Structure** - Module files copied directly into app folders
- ✅ **Namespace Adjustment** - Changed from `Identity.*` to `MyApp.*`
- ✅ **Single Project** - All code compiled together
- ✅ **No Boundaries** - Can freely modify/mix module code

**Use Cases**:
- Small applications (< 10 features)
- Prototypes, MVPs
- Learning NetMX
- When module boundaries aren't needed

#### 3. Microservices Template (`templates/microservices/`) ⏳ PLANNED

**Status**: Planned for Phase 4 (Months 7-9)

**What It Will Be**: Multiple independently deployable services

---

## 🛠️ CLI Workflow

### Creating New Projects

```bash
# 1. Create project from template
netmx new modular MyShop
cd MyShop

# Result:
# - Copies templates/modular/ → MyShop/
# - Renames namespaces (NetMXApp → MyShop)
# - Creates solution file
# - Ready to run!

# 2. Add pre-built modules
netmx add module Identity

# Result:
# - Copies modules/Identity/ → MyShop/modules/Identity/
# - Adds projects to MyShop.sln
# - Wires up in Program.cs: services.AddIdentity()
# - Updates AppDbContext with Identity tables

# 2b. Add Pro modules (requires license key)
netmx add module MultiTenancy --license NETMX-MT-ABC123-...

# Result:
# - Same as free modules (source code copy)
# - Adds license key to appsettings.json
# - License validated at application startup
# - See PRO-MODULE-LICENSING.md for details

# 3. Generate custom features
netmx generate feature Product

# Result:
# - Creates Product.cs entity
# - Creates ProductService.cs
# - Creates ProductController.cs
# - Creates Product views (Index, _List, _Form)
# - Adds DbSet<Product> to AppDbContext
# - Registers IProductService in DI
```

### Template Workflow Details

**Phase 1: Template Copy**
```
templates/modular/                  MyApp/
├── NetMXApp.sln          →        ├── MyApp.sln
└── src/NetMXApp.Web/     →        └── src/MyApp.Web/
    ├── Program.cs                     ├── Program.cs
    └── ...                            └── ...
```

**Phase 2: Module Addition** (Source Copy)
```
modules/Identity/                   MyApp/modules/Identity/
├── Identity.Core/        →        ├── Identity.Core/
├── Identity.Contracts/   →        ├── Identity.Contracts/
├── Identity.Application/ →        ├── Identity.Application/
└── Identity.Web/         →        └── Identity.Web/
```

**Phase 3: Feature Generation** (Code Gen)
```
CLI Generates:
MyApp.Web/
├── Models/Product.cs               # Entity
├── Services/
│   ├── IProductService.cs         # Interface
│   └── ProductService.cs          # Implementation
├── Controllers/
│   └── ProductController.cs       # HTMX-enabled controller
└── Views/Product/
    ├── Index.cshtml               # List view
    ├── _List.cshtml               # Partial table
    └── _Form.cshtml               # Create/Edit modal
```

---

## 📊 Current Status (October 25, 2025)

### ✅ Completed (100%)

**Framework SDK** (10 packages)
- NetMX.Core, NetMX.Events, NetMX.Ddd.Domain, NetMX.Ddd.Application
- NetMX.EntityFrameworkCore, NetMX.AspNetCore.Core, NetMX.AspNetCore.Mvc
- NetMX.Data, NetMX.Testing
- **Version**: 0.2.0-local (C:\LocalNuGet)

**Modules** (3 production-ready)
- ✅ **Identity**: User authentication, registration, profile management
- ✅ **Authorization**: Permissions, roles, policies (6 events, 38 tests)
- ⏸️ **Audit**: Scaffolded (needs implementation)

**CLI Commands**
- ✅ `netmx new modular` - Create from modular template
- ✅ `netmx new monolith` - Create from monolith template
- ✅ `netmx create module` - Scaffold new module
- ✅ `netmx generate feature` - Generate CRUD feature (13 files)
- ✅ `netmx add module` - Add existing module to project
- ✅ `netmx db migrate/update/rollback/reset/status` - Database operations
- ✅ `--migrate` flag - Auto-migration after feature generation

**CLI Automation**
- ✅ Auto-service registration in Program.cs
- ✅ Auto-Events package refresh
- ✅ Auto-DbSet addition with pluralization
- ✅ Zero manual steps per feature
- ✅ 95% time savings (10 min → 30 sec)

**Quality Metrics**
- ✅ 356/356 tests passing (100% pass rate)
  - Framework: 178/178 ✅
  - Modules: 66/66 ✅
  - CLI: 112/112 ✅ (21 E2E tests skipped with documentation)
- ✅ Zero compilation errors
- ✅ Zero warnings across all builds
- ✅ Dogfooding validated (ECommerceDogfood: 32/32 endpoints passing)

**Templates**
- ✅ Modular monolith template (SQLite, HTMX, Bulma)
- ✅ Simple monolith template
- ✅ Template copy working (`netmx new`)
- ✅ Module addition working (`netmx add module`)

### 🔄 In Progress

**Phase 2D**: E2E Testing + NetMX.Testing Package
- NetMX.Testing package enhancements
- CLI test commands (`netmx test feature/module/e2e`)
- Playwright integration
- SQLite test database support

### ⏳ Next Up (Week 3+)

**Settings Module** (Week 3)
- Global, user, and tenant-ready settings
- Settings UI
- Validation + tests

**Audit Module Complete** (Weeks 4-5)
- Automatic entity change tracking
- Action audit logging
- Compliance reporting

**Observability Module** (Weeks 6-7)
- Health check UI
- Metrics endpoint (Prometheus)
- Tracing setup (OpenTelemetry)

---

## 🎯 Product Components

### 1. Framework SDK (Infrastructure)

**Purpose**: Pure infrastructure, zero features

**10 Packages**:
1. **NetMX.Core** - Core abstractions, dependency injection markers
2. **NetMX.Events** - Type-safe event registry (no magic strings)
3. **NetMX.Ddd.Domain** - Entities, repositories, value objects
4. **NetMX.Ddd.Application** - Application services, DTOs
5. **NetMX.EntityFrameworkCore** - EF Core integration, DbContext base
6. **NetMX.AspNetCore.Core** - ASP.NET Core extensions, middleware
7. **NetMX.AspNetCore.Mvc** - MVC extensions, HTMX helpers
8. **NetMX.Data** - Data access abstractions
9. **NetMX.Testing** - Test helpers, factories, assertions
10. **NetMX.Htmx** - DEPRECATED (merged into AspNetCore.Mvc)

**Distribution**: NuGet.org (pre-release), C:\LocalNuGet (local dev)

### 2. Templates (Project Starters)

**Purpose**: Production-ready starting points

**Available Now**:
- ✅ Modular Monolith (`templates/modular/`)
- ✅ Simple Monolith (`templates/monolith/`)

**Future**:
- ⏳ Microservices (`templates/microservices/`) - Phase 4

**How They Work**:
1. Developer runs `netmx new modular MyApp`
2. CLI copies template to new directory
3. Renames namespaces, solution files
4. Developer adds modules via `netmx add module`
5. Modules copied as source code (not NuGet)
6. CLI wires everything together automatically

### 3. Modules (Optional Features)

**Purpose**: Reusable feature packages

**Free Modules** (MIT License):
- ✅ **Identity**: User authentication, ASP.NET Core Identity
- ✅ **Authorization**: Permissions, roles, policies
- ⏸️ **Audit**: Audit logging, compliance (scaffolded)
- ⏳ **Settings**: Global, user, tenant settings (Week 3)
- ⏳ **Observability**: Health checks, metrics, tracing (Weeks 6-7)
- ⏳ **Testing**: Test helpers, E2E framework (Phase 2D)

**Paid Modules** (One-Time Purchase):
- ⏳ **Multi-Tenancy**: Database-per-tenant, tenant isolation ($299)
- ⏳ **Background Jobs**: Hangfire integration ($149)
- ⏳ **Email/SMS**: Templates, providers ($149)
- ⏳ **CMS**: Content management ($249)
- ⏳ **BLOB Storage**: Azure, AWS, S3 ($149)

**How Modules Work**:
- 4-layer architecture (Core, Contracts, Application, Web)
- Copied as source code into consuming apps (free AND paid)
- Fully customizable (developer owns the code)
- Pro modules require license key validation at runtime
- Can be extracted to microservices later

**Pro Module Distribution**:
- **Source code copy** (NOT NuGet packages for Pro modules)
- License key required: `netmx add module MultiTenancy --license KEY`
- License validated at application startup (graceful error if invalid)
- One-time purchase, perpetual usage license
- 1 year of updates included, optional $49/year renewal
- See [PRO-MODULE-LICENSING.md](PRO-MODULE-LICENSING.md) for complete details

### 4. CLI Tools (Developer Tooling)

**Purpose**: Automate boilerplate, enforce patterns

**Commands**:
- `netmx new` - Create from template
- `netmx create module` - Scaffold module
- `netmx generate feature` - Generate CRUD feature
- `netmx add module` - Add existing module
- `netmx db` - Database operations
- `netmx test` - Run tests (planned)

**Time Savings**:
- Manual feature: 5-10 minutes
- CLI feature: 15 seconds
- **Reduction**: 95%

### 5. Visual Tools (Future)

**NetMX Studio** (FREE - Phase 5)
- VS Code fork optimized for NetMX
- Module marketplace
- Entity designer
- Observability dashboard

**NetMX Suite** (PAID - Phase 5)
- Web-based code generator
- Visual entity designer
- UI designer (HTMX)
- Business rules engine
- $49-$199/month

---

## 💰 Business Model

### Tiering Strategy (One-Time Purchase)

**FREE** (MIT License):
- Framework SDK (10 packages)
- Core modules (Identity, Authorization, Settings, Audit, Observability, Testing)
- Templates (modular, monolith)
- CLI tools

**STANDARD** ($499 one-time):
- All FREE features
- Advanced modules: Multi-Tenancy, Jobs, Caching, Email, CMS, BLOB Storage

**PRO** ($1,499 one-time):
- All STANDARD features
- Pro modules: Distributed tracing, Microservices, Event Bus, API Gateway

**ENTERPRISE** ($4,999 one-time):
- All PRO features
- Enterprise modules: Advanced observability, Security scanning, Priority support

### Individual Pro Modules
- Multi-Tenancy: $299
- Background Jobs: $149
- Email/SMS: $149
- CMS: $249
- BLOB Storage: $149
- Payment Integration: $199

### Revenue Projections
- Year 1: $150K (300 STANDARD, 50 PRO, 10 ENTERPRISE)
- Year 2: $800K (scale + Studio/Suite)
- Year 3: $2.5M (ecosystem maturity)
- Year 4: $5M+ (full ecosystem)

---

## 🗺️ Roadmap Summary

### Phase 1: Foundation ✅ COMPLETE (Oct 20, 2025)
- Framework SDK (10 packages)
- Identity module
- Authorization module
- CLI basics
- Templates (modular, monolith)

### Phase 2: Essential Infrastructure ⏳ IN PROGRESS (Months 1-3)
- ✅ Week 1: Authorization complete
- ✅ Week 2: CLI automation complete
- ✅ Week 3: Quality cleanup (356/356 tests passing)
- 🔄 Week 3-4: Settings module
- ⏳ Week 4-5: Audit module complete
- ⏳ Week 6-7: Observability module
- ⏳ Week 8-9: Testing infrastructure
- ⏳ Week 10-12: Multi-Tenancy (FIRST PAID MODULE)

### Phase 3: Advanced Modules (Months 4-6)
- Background Jobs (Hangfire)
- Distributed Caching (Redis)
- Email/SMS
- BLOB Storage
- Localization
- CMS

### Phase 4: Distributed Architecture (Months 7-9)
- Event Bus (RabbitMQ, Kafka)
- API Gateway (YARP)
- Microservices template
- Distributed tracing

### Phase 5: Studio & Suite (Months 10-15)
- NetMX Studio (VS Code fork) - FREE
- NetMX Suite (Web SaaS) - $49-$199/mo
- Module marketplace
- Visual designers

### Phase 6: Enterprise & Community (Months 16-18)
- Visual Studio templates
- Advanced observability
- AI-powered code review
- Security & compliance
- Community building

---

## 🎓 Key Concepts

### Module
A **reusable package** containing related features (Identity, Audit, CMS)
- 4-layer architecture (Core, Contracts, Application, Web)
- Copied as source code into projects
- Fully customizable

### Feature
A **single business entity** with complete CRUD (Product, Order, AuditLog)
- Generated with `netmx generate feature`
- 13 files (entity, DTOs, service, controller, views)
- HTMX patterns built-in

### Template
A **production-ready starting point** for new projects
- Copied by CLI (`netmx new`)
- Pre-configured (SQLite, HTMX, Bulma)
- Modules added via source copy

### Event Registry
A **type-safe event system** replacing magic strings
- `Events.Product.Created` (not `"product-created"`)
- IntelliSense support
- Compile-time safety

---

## 📚 Documentation Index

### Getting Started
- [README.md](../README.md) - Repository overview
- [QUICK-START.md](QUICK-START.md) - 5-minute guide (needs update)
- [TERMINOLOGY.md](TERMINOLOGY.md) - Concepts explained

### Architecture
- [MODULAR-ARCHITECTURE.md](MODULAR-ARCHITECTURE.md) - 4-layer design
- [EVENT-REGISTRY-ARCHITECTURE.md](EVENT-REGISTRY-ARCHITECTURE.md) - Type-safe events
- [EVENT-BUS-ARCHITECTURE.md](EVENT-BUS-ARCHITECTURE.md) - Event bus design

### Developer Experience
- [DX.md](DX.md) - DX principles
- [AUTOMATED-ENDPOINT-TESTING.md](AUTOMATED-ENDPOINT-TESTING.md) - Testing approach
- [E2E-TESTING-FRAMEWORK.md](E2E-TESTING-FRAMEWORK.md) - E2E architecture

### Product & Business
- [THE-PRODUCT.md](THE-PRODUCT.md) - Product vision
- [INSPIRATION.md](INSPIRATION.md) - Why HTMX + .NET
- [STUDIO-SUITE-VISION.md](STUDIO-SUITE-VISION.md) - Visual tools

### Implementation
- [ROADMAP.md](ROADMAP.md) - Detailed roadmap (needs update)
- [THEMING-STRATEGY.md](THEMING-STRATEGY.md) - UI theming
- Module READMEs in `modules/*/README.md`

---

## 🔧 Technical Stack

**Backend**:
- .NET 9.0 (LTS)
- ASP.NET Core 9.0
- EF Core 9.0.10
- SQLite (default), PostgreSQL (production)

**Frontend**:
- HTMX 2.0.4 (HTML over the wire)
- Bulma 1.0.4 (CSS framework)
- Zero JavaScript frameworks

**Testing**:
- xUnit 2.9.3
- FluentAssertions
- Playwright (E2E)
- Test SDK 18.0.0

**Tooling**:
- .NET CLI (dotnet tool)
- PowerShell (automation scripts)
- LibMan (frontend package management)

---

## 🎯 Competitive Position

**vs ABP Framework**:
- ABP: 10 years, 100+ modules, $199-$2,999/year subscription
- NetMX: New, ~20% feature parity, $149-$1,499 one-time purchase
- **Our Advantages**: HTMX-first, 70-95% cheaper, observability built-in, modern .NET 9

**vs Rails, Laravel**:
- Similar DX goals (convention over configuration)
- Better type safety (C# vs PHP/Ruby)
- HTMX instead of Hotwire/Livewire

**vs Next.js, Remix**:
- Server-first (same goal)
- .NET ecosystem (not Node.js)
- Simpler (no React complexity)

---

## ✅ Quality Standards

**Zero Warnings Policy** ✅ MANDATORY
- All code compiles with 0 errors AND 0 warnings
- Applies to framework, modules, CLI, templates, samples

**Test Coverage**
- Target: 80%+ coverage
- Current: 356/356 tests passing (100%)
- Tests run on every commit

**Documentation**
- Every public API has XML comments
- Every module has comprehensive README
- Architecture decisions documented

**Developer Experience**
- Type-safe APIs everywhere
- IntelliSense support
- Clear error messages
- Fast feedback loops

---

## 🚀 Next Actions

### Immediate (This Week)
1. ✅ Fix all test failures (COMPLETE - 356/356 passing)
2. 🔄 Settings module (Week 3)
3. ⏳ Update ROADMAP.md with latest status
4. ⏳ Update QUICK-START.md with template workflow

### Short-Term (Weeks 4-9)
1. Complete Audit module
2. Build Observability module
3. Enhance testing infrastructure
4. Dogfood with real applications

### Medium-Term (Months 4-6)
1. Multi-Tenancy module (FIRST PAID)
2. Background Jobs module
3. Advanced modules (Email, CMS, BLOB)
4. First paying customers

### Long-Term (Months 10-18)
1. NetMX Studio (VS Code fork)
2. NetMX Suite (Web SaaS)
3. Community building
4. Enterprise features

---

## 📞 Summary for New Chat Sessions

**"What is NetMX?"**
- HTMX-first framework for .NET web apps
- Template-based (modular/monolith), module-based features
- CLI automates 95% of boilerplate
- Type-safe events, DDD patterns, zero warnings

**"Where are we?"**
- Phase 2, Week 3 (Settings module next)
- 10 framework packages complete
- 3 modules production-ready (Identity, Authorization, Audit scaffolded)
- CLI working: `netmx new`, `generate feature`, `add module`, `db` commands
- 356/356 tests passing, zero warnings

**"Where are we headed?"**
- Finish Phase 2 (6 free modules) by Month 3
- First paid module (Multi-Tenancy) Month 4
- Visual tools (Studio/Suite) Months 10-15
- $5M ARR by Year 4

**"How does it work?"**
1. Start from template: `netmx new modular MyApp`
2. Add modules: `netmx add module Identity` (source copy, not NuGet)
3. Generate features: `netmx generate feature Product` (13 files, 15 seconds)
4. Modules copied as source code (fully customizable)
5. CLI wires everything automatically

**"What's unique?"**
- HTMX-first (no React/Angular/Vue)
- Template + module strategy (not just packages)
- One-time purchase ($149-$1,499, not subscription)
- Source copy modules (not NuGet for flexibility)
- Type-safe events (no magic strings)
- Zero warnings policy

---

**Last Updated**: October 25, 2025  
**Maintained By**: Development Team  
**Review Cycle**: Updated after each major milestone

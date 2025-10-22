# 📘 NetMX Master Reference

**Last Updated**: October 22, 2025  
**Purpose**: Single source of truth and navigation hub for the entire NetMX project  
**Status**: Phase 2 - Event Registry Complete, CLI Updates Pending

> **Note**: This document is the **master reference** for understanding NetMX's current state, architecture, and development priorities. Always start here when beginning work or onboarding.

---

## 🎯 Current Project Status

### Where We Are Now
- **Phase**: Phase 2 - Essential Infrastructure (Week 2 of 12)
- **Last Major Milestone**: Event Registry implementation complete
- **Current Focus**: Documentation cleanup & CLI updates
- **Next Milestone**: CLI Phase 3 - Event Registry generation

### Key Metrics
- **Framework Packages**: 10 packages (all building with 0 warnings)
- **Modules**: 3 complete (Authorization, Identity, Audit)
- **Tests Passing**: 47 tests (34 Event Registry + 13 module integration)
- **Documentation**: 65 living documents + 46 archived
- **Feature Parity**: ~23% of ABP Framework

---

## 📚 Essential Reading (Start Here)

### For New Contributors
1. **[README.md](README.md)** - Project overview and vision
2. **[QUICK-START.md](docs/QUICK-START.md)** - Get started in 5 minutes
3. **[TERMINOLOGY.md](docs/TERMINOLOGY.md)** - Key concepts and definitions
4. **[CONTRIBUTING.md](CONTRIBUTING.md)** - How to contribute

### For Understanding Architecture
1. **[ARCHITECTURE-DECISIONS.md](docs/ARCHITECTURE-DECISIONS.md)** - Core architectural choices
2. **[EVENT-REGISTRY-ARCHITECTURE.md](docs/EVENT-REGISTRY-ARCHITECTURE.md)** - Type-safe event system
3. **[HTMX-PATTERNS.md](docs/HTMX-PATTERNS.md)** - HTMX integration patterns
4. **[INTEGRATION-PATTERNS.md](docs/INTEGRATION-PATTERNS.md)** - Module integration

### For Development
1. **[CLI-ARCHITECTURE.md](docs/CLI-ARCHITECTURE.md)** - CLI design and usage
2. **[TESTING-DOGFOODING-STRATEGY.md](docs/TESTING-DOGFOODING-STRATEGY.md)** - Testing approach
3. **[COMPLETE-DEVELOPMENT-ROADMAP.md](docs/COMPLETE-DEVELOPMENT-ROADMAP.md)** - Detailed roadmap
4. **[.github/copilot-instructions.md](.github/copilot-instructions.md)** - AI assistant context

### For Planning & Strategy
1. **[ROADMAP.md](docs/ROADMAP.md)** - High-level roadmap
2. **[PRO-PACKAGE-STRATEGY.md](docs/PRO-PACKAGE-STRATEGY.md)** - Monetization strategy
3. **[STUDIO-SUITE-VISION.md](docs/STUDIO-SUITE-VISION.md)** - Future products
4. **[TIERING-STRATEGY.md](docs/TIERING-STRATEGY.md)** - Pricing tiers

---

## 🏗️ Project Structure

### Repository Layout
```
netmx/
├── framework/                    # Core framework packages (10 packages)
│   ├── NetMX.Core/              # Core abstractions, DI markers
│   ├── NetMX.Events/            # Type-safe event system ⭐ NEW
│   ├── NetMX.Ddd.Domain/        # DDD primitives
│   ├── NetMX.AspNetCore.Mvc/    # HTMX helpers, MVC extensions
│   └── ... (6 more)
│
├── modules/                      # Reusable feature modules
│   ├── Authorization/           # Permissions, roles, policies ✅
│   ├── Identity/                # User management ✅
│   └── Audit/                   # Audit logging ✅
│
├── tools/                        # CLI and development tools
│   └── NetMX.CLI/               # Code generation CLI
│
├── templates/                    # Project templates
│   └── modular/                 # Modular monolith template
│
├── docs/                         # Living documentation
│   ├── archive/                 # Historical documents (46 files)
│   └── ... (65 active docs)
│
└── .github/                      # GitHub workflows & AI context
    └── copilot-instructions.md  # Comprehensive AI context
```

### Key Packages

| Package | Purpose | Status |
|---------|---------|--------|
| **NetMX.Core** | Core abstractions, DI markers | ✅ Stable |
| **NetMX.Events** | Type-safe event system | ⭐ NEW (v0.2.0) |
| **NetMX.Ddd.Domain** | Entities, repositories, value objects | ✅ Stable |
| **NetMX.Ddd.Application** | Application services, DTOs | ✅ Stable |
| **NetMX.AspNetCore.Mvc** | HTMX helpers, controllers | ✅ Stable |
| **NetMX.EntityFrameworkCore** | EF Core integration | ✅ Stable |
| **NetMX.Testing** | Testing infrastructure | ✅ Stable |

---

## 🎨 Core Concepts

### 1. Event Registry Pattern ⭐ NEW
**The new way to define and use events in NetMX**

**Old Pattern (Deprecated)**:
```csharp
// Module-specific partial classes (collision-prone)
public static partial class DomainEvents
{
    public static class Permission
    {
        public const string Created = "permission.created";
    }
}
```

**New Pattern (Event Registry)**:
```csharp
// Centralized registry with type-safe access
Events.Permission.Created // IntelliSense support!
this.HxTrigger(Events.Permission.Created, new { permissionId = id });

// Event definitions registered in modules
public static class AuthorizationEventDefinitions
{
    public static void Register(IEventRegistry registry)
    {
        registry.Register<PermissionCreatedPayload>(
            Events.Permission.Created,
            category: "Permission",
            description: "Triggered when a permission is created"
        );
    }
}
```

**Benefits**:
- ✅ No CS0436 duplicate definition errors
- ✅ IntelliSense across all modules
- ✅ Compile-time type safety
- ✅ No module project references needed
- ✅ Centralized event catalog

**Documentation**: [EVENT-REGISTRY-ARCHITECTURE.md](docs/EVENT-REGISTRY-ARCHITECTURE.md)

### 2. HTMX-First Approach
Server-rendered HTML with HTMX for interactivity (no heavy JS frameworks).

```html
<!-- Delete button with HTMX -->
<button hx-delete="/api/permissions/@permission.Id" 
        hx-target="#permission-row-@permission.Id"
        hx-swap="outerHTML"
        hx-confirm="Delete this permission?">
    Delete
</button>
```

**Documentation**: [HTMX-PATTERNS.md](docs/HTMX-PATTERNS.md)

### 3. CLI-Driven Development
Generate features and modules using the CLI (don't create files manually).

```bash
# Generate a complete CRUD feature
netmx generate feature Product

# Create a new module
netmx create module Catalog

# Generate in a specific module
netmx generate feature AuditLog -m Audit
```

**Documentation**: [CLI-ARCHITECTURE.md](docs/CLI-ARCHITECTURE.md)

### 4. Modular Architecture
Everything is optional. Framework is pure infrastructure, features are modules.

**Framework** (Pure Infrastructure):
- Core abstractions
- DDD patterns
- ASP.NET Core extensions
- HTMX helpers
- Event system

**Modules** (Optional Features):
- Authorization (permissions, roles)
- Identity (user management)
- Audit (logging, compliance)
- CMS (content management) - Coming soon
- Multi-tenancy (paid) - Coming soon

**Documentation**: [TERMINOLOGY.md](docs/TERMINOLOGY.md)

---

## 🚀 Current Priorities (October 2025)

### Phase 2: Essential Infrastructure (Months 1-3)

**Week 2 Status** (October 21-25):
- ✅ Event Registry implementation complete
- ✅ All 3 modules migrated to Event Registry
- ✅ Documentation cleanup complete
- ⏸️ CLI Phase 3: Event Registry generation (HIGH PRIORITY)

**Week 3-4** (October 28 - November 8):
- Settings Module (global, user, tenant-ready)
- CLI improvements (auto-migration, db commands)

**Week 5-6** (November 11-22):
- Complete Audit Logging module
- Entity change tracking

**Week 7-8** (November 25 - December 6):
- Observability Module (metrics, tracing, health checks)

**Week 9-10** (December 9-20):
- Testing Infrastructure (unit, integration, E2E)

**Week 11-12** (December 23 - January 3):
- Multi-Tenancy Module 💰 **FIRST PAID MODULE**

**Documentation**: [COMPLETE-DEVELOPMENT-ROADMAP.md](docs/COMPLETE-DEVELOPMENT-ROADMAP.md)

---

## 📖 Documentation Navigation

### By Category

**Event System**:
- [EVENT-REGISTRY-ARCHITECTURE.md](docs/EVENT-REGISTRY-ARCHITECTURE.md) - Core architecture
- [EVENT-REGISTRY-MULTI-ARCHITECTURE.md](docs/EVENT-REGISTRY-MULTI-ARCHITECTURE.md) - Multi-architecture support
- [TYPE-SAFE-EVENTS-EXAMPLES.md](docs/TYPE-SAFE-EVENTS-EXAMPLES.md) - Usage examples
- [EVENT-BUS-ARCHITECTURE.md](docs/EVENT-BUS-ARCHITECTURE.md) - Future distributed events
- [EVENT-PIPELINES.md](docs/EVENT-PIPELINES.md) - Event processing patterns

**CLI & Development**:
- [CLI-ARCHITECTURE.md](docs/CLI-ARCHITECTURE.md) - CLI design
- [CLI-IMPLEMENTATION.md](docs/CLI-IMPLEMENTATION.md) - Implementation details
- [CLI-IMPROVEMENTS.md](docs/CLI-IMPROVEMENTS.md) - Planned improvements
- [CLI-STRATEGY.md](docs/CLI-STRATEGY.md) - Overall strategy
- [tools/NetMX.CLI/README.md](tools/NetMX.CLI/README.md) - CLI user guide

**Architecture**:
- [ARCHITECTURE-DECISIONS.md](docs/ARCHITECTURE-DECISIONS.md) - Key decisions
- [HTMX-PATTERNS.md](docs/HTMX-PATTERNS.md) - HTMX integration
- [INTEGRATION-PATTERNS.md](docs/INTEGRATION-PATTERNS.md) - Module integration
- [EXTENSIBILITY-PRINCIPLES.md](docs/EXTENSIBILITY-PRINCIPLES.md) - Extension points
- [BRANCHING-STRATEGY.md](docs/BRANCHING-STRATEGY.md) - Git workflow

**Testing & Quality**:
- [TESTING-DOGFOODING-STRATEGY.md](docs/TESTING-DOGFOODING-STRATEGY.md) - Testing approach
- [TESTING-RESULTS.md](docs/TESTING-RESULTS.md) - Test results
- [TESTING-PLAN.md](TESTING-PLAN.md) - Test planning

**Setup & Operations**:
- [QUICK-START.md](docs/QUICK-START.md) - Get started quickly
- [GITHUB-SETUP.md](docs/GITHUB-SETUP.md) - GitHub CI/CD
- [NUGET-PUBLISHING.md](docs/NUGET-PUBLISHING.md) - NuGet workflow
- [LOCAL-NUGET-SETUP.md](docs/LOCAL-NUGET-SETUP.md) - Local development
- [XML-DOCS-STRATEGY.md](docs/XML-DOCS-STRATEGY.md) - Documentation standards

**Strategy & Business**:
- [PRO-PACKAGE-STRATEGY.md](docs/PRO-PACKAGE-STRATEGY.md) - Monetization
- [TIERING-STRATEGY.md](docs/TIERING-STRATEGY.md) - Pricing tiers
- [STUDIO-SUITE-VISION.md](docs/STUDIO-SUITE-VISION.md) - Future products

---

## 🧪 Testing & Quality

### Current Test Coverage
- **Event Registry**: 34 tests passing
- **Module Integration**: 13 tests passing
- **Total**: 47 tests passing
- **Build Status**: 0 warnings

### Testing Strategy
1. **Unit Tests**: Test domain logic in isolation
2. **Integration Tests**: Test module integration with EF Core
3. **E2E Tests**: Test HTMX interactions (Playwright)
4. **Dogfooding**: Build real apps to validate CLI and patterns

**Documentation**: [TESTING-DOGFOODING-STRATEGY.md](docs/TESTING-DOGFOODING-STRATEGY.md)

---

## 💡 Quick Reference

### Common Tasks

**Start a new feature**:
```bash
netmx generate feature Product
# Or in a module:
netmx generate feature AuditLog -m Audit
```

**Run tests**:
```bash
dotnet test framework/NetMX.sln
```

**Build everything**:
```bash
dotnet build framework/NetMX.sln --configuration Release
```

**Update local packages**:
```bash
.\scripts\pack-framework.ps1
.\scripts\pack-modules.ps1
```

**Check documentation**:
```bash
# Read this file first!
# Then check docs/ directory for specific topics
```

### Module Status

| Module | Status | Tests | Events | Notes |
|--------|--------|-------|--------|-------|
| **Authorization** | ✅ Complete | 1 test | 6 events | Permissions, roles, policies |
| **Identity** | ✅ Complete | 1 test | 16 events | User management, authentication |
| **Audit** | ✅ Complete | 1 test | 15 events | Audit logging, entity tracking |

### Package Versions

- **Framework**: 0.2.0-local (Event Registry)
- **Modules**: 0.1.0-local
- **.NET**: 9.0 (LTS)
- **EF Core**: 9.0.10
- **HTMX**: 2.0.4

---

## 📝 Recent Changes

### October 22, 2025 - Major Documentation Cleanup
- Deleted stale ECommerce sample app
- Archived 46 historical documents
- Removed orphaned DomainEvents files
- Consolidated duplicate documentation
- Created this master reference document

### October 21-22, 2025 - Event Registry Complete
- Implemented IEventRegistry and EventRegistry
- Created Events static class for type-safe access
- Migrated all 3 modules (18 controllers, 6 views)
- Added 34 unit tests (all passing)
- Updated to Event Registry pattern across codebase

### October 21, 2025 - CLI Automation Phase 2
- MigrationOrchestrator complete (full workflow automation)
- CLI integration with --migrate flag
- Database commands (migrate, update, rollback, etc.)
- 158 tests passing

---

## 🎯 Success Metrics

### Phase 2 Goals (By January 2026)
- ✅ 23% ABP feature parity (current)
- 🎯 30% ABP feature parity (target)
- 🎯 6 modules complete (3/6 done)
- 🎯 200+ tests passing (47 current)
- 🎯 First paid module (Multi-Tenancy)
- 🎯 First paying customer

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

**Key Principles**:
1. Use CLI to generate code (don't create manually)
2. Follow Event Registry pattern for events
3. Add tests for new features
4. Update documentation
5. Build with 0 warnings

---

## 📞 Getting Help

- **Documentation**: Start with this file, then explore docs/
- **Issues**: GitHub Issues for bugs and feature requests
- **Discussions**: GitHub Discussions for questions
- **AI Context**: .github/copilot-instructions.md for AI assistants

---

## 🗂️ Related Documents

- **[DOCUMENTATION-CLEANUP-SUMMARY.md](DOCUMENTATION-CLEANUP-SUMMARY.md)** - Cleanup status and remaining tasks
- **[CHANGELOG.md](CHANGELOG.md)** - Version history
- **[PROGRESS-REPORT.md](PROGRESS-REPORT.md)** - Current sprint status
- **[docs/archive/](docs/archive/)** - Historical documentation

---

**Last Updated**: October 22, 2025  
**Maintained By**: NetMX Team  
**Review Frequency**: Updated with each major milestone

---

*This is the master reference. When in doubt, start here.*

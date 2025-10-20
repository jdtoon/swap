# NetMX Framework

[![Build Status](https://github.com/toonjd/netmx/actions/workflows/ci-build.yml/badge.svg)](https://github.com/toonjd/netmx/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)

**A pure, modular, HTMX-first framework for building web applications with ASP.NET Core.**

NetMX is designed around a simple principle: **framework first, features optional**. It provides the infrastructure you need without forcing features you don't want, letting you build exactly the application you need.

## 🌟 Why NetMX?

- **🎯 Zero JavaScript Frameworks** - Pure server-rendered HTML with HTMX for interactivity
- **🧩 True Modularity** - Every feature is an optional module you can add or remove
- **🏗️ DDD-First** - Built on Domain-Driven Design principles with clean architecture
- **⚡ Event-Driven** - Loosely coupled components via HTMX events (monolith-first, scalable to distributed)
- **💻 Developer Experience** - Strong typing, IntelliSense, CLI scaffolding, and excellent tooling

## 🚀 Quick Start

**From zero to HTMX app in 5 minutes!** See [QUICK-START.md](docs/QUICK-START.md) for detailed guide.

### 1. Install CLI

```bash
# Install NetMX CLI globally
dotnet tool install --global NetMX.CLI

# Verify installation
netmx --help
```

### 2. Create Project

```bash
# Clone template (dotnet templates coming soon)
git clone https://github.com/netmx-framework/template-modular.git MyApp
cd MyApp
```

### 3. Start Database

```bash
# Using Docker (recommended)
docker-compose up -d db
```

### 4. Generate Your First Feature

```bash
cd src/MyApp.Web

# Generate complete CRUD with HTMX patterns
netmx generate feature Product

# Add DbSet to AppDbContext, then migrate
dotnet ef migrations add AddProduct
dotnet ef database update
```

### 5. Run!

```bash
dotnet run
```

Visit `http://localhost:5263/Product` - Your HTMX-powered CRUD is ready! 🎉

**No manual file creation. No boilerplate. Just CLI commands and business logic.**

## 📦 Framework Packages

NetMX consists of 10 core packages, all available on [NuGet.org](https://www.nuget.org/packages?q=netmx):

| Package | Description | Version |
|---------|-------------|---------|
| **NetMX.Core** | Core abstractions and DI patterns | [![NuGet](https://img.shields.io/nuget/v/NetMX.Core.svg)](https://www.nuget.org/packages/NetMX.Core) |
| **NetMX.Events** | Type-safe event names for HTMX communication | [![NuGet](https://img.shields.io/nuget/v/NetMX.Events.svg)](https://www.nuget.org/packages/NetMX.Events) |
| **NetMX.Ddd.Domain** | DDD building blocks (entities, aggregates, repositories) | [![NuGet](https://img.shields.io/nuget/v/NetMX.Ddd.Domain.svg)](https://www.nuget.org/packages/NetMX.Ddd.Domain) |
| **NetMX.Ddd.Application.Contracts** | DTOs and service interfaces | [![NuGet](https://img.shields.io/nuget/v/NetMX.Ddd.Application.Contracts.svg)](https://www.nuget.org/packages/NetMX.Ddd.Application.Contracts) |
| **NetMX.Ddd.Application** | Application services and use cases | [![NuGet](https://img.shields.io/nuget/v/NetMX.Ddd.Application.svg)](https://www.nuget.org/packages/NetMX.Ddd.Application) |
| **NetMX.Data** | Data access abstractions | [![NuGet](https://img.shields.io/nuget/v/NetMX.Data.svg)](https://www.nuget.org/packages/NetMX.Data) |
| **NetMX.EntityFrameworkCore** | EF Core integration with DDD support | [![NuGet](https://img.shields.io/nuget/v/NetMX.EntityFrameworkCore.svg)](https://www.nuget.org/packages/NetMX.EntityFrameworkCore) |
| **NetMX.AspNetCore.Core** | ASP.NET Core middleware and extensions | [![NuGet](https://img.shields.io/nuget/v/NetMX.AspNetCore.Core.svg)](https://www.nuget.org/packages/NetMX.AspNetCore.Core) |
| **NetMX.AspNetCore.Mvc** | MVC extensions and HTMX helpers | [![NuGet](https://img.shields.io/nuget/v/NetMX.AspNetCore.Mvc.svg)](https://www.nuget.org/packages/NetMX.AspNetCore.Mvc) |
| **NetMX.Htmx** | Strongly-typed HTMX helpers | [![NuGet](https://img.shields.io/nuget/v/NetMX.Htmx.svg)](https://www.nuget.org/packages/NetMX.Htmx) |

## 🎨 HTMX-First Philosophy

NetMX embraces [HTMX](https://htmx.org) for building interactive UIs without heavy JavaScript frameworks:

```html
<!-- View: Clean, declarative HTMX attributes -->
<button hx-delete="/api/users/@user.Id" 
        hx-target="#user-row-@user.Id"
        hx-swap="outerHTML"
        hx-confirm="Are you sure?">
    Delete
</button>
```

```csharp
// Controller: Type-safe event names
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;

[HttpDelete("/api/users/{id}")]
public IActionResult Delete(Guid id)
{
    _userService.Delete(id);
    
    // No magic strings! IntelliSense-supported event names
    this.HxTrigger(DomainEvents.User.Deleted, new { userId = id });
    this.HxReswap(HtmxSwap.Delete);
    
    return Ok();
}
```

```html
<!-- View: Type-safe event listening -->
@using NetMX.Events

<div hx-get="/api/stats" 
     hx-trigger="@DomainEvents.User.Deleted from:body">
    <!-- Auto-refreshes when user deleted -->
</div>
```

**Benefits:**
- ✅ Server-rendered HTML (SEO-friendly, fast initial load)
- ✅ Progressive enhancement (works without JavaScript)
- ✅ Type-safe server-side code (compile-time errors, IntelliSense)
- ✅ **Type-safe events** (no magic strings, refactoring-safe)
- ✅ Simple mental model (HTML over the wire)
- ✅ Excellent performance (minimal client-side overhead)

## 🧩 Optional Modules

All features are packaged as optional modules you can mix and match:

### Available Now
- **Identity** - User management, authentication, roles

### Coming Soon (Phase 2)
- **Audit Logging** - Entity change tracking and user actions
- **Background Jobs** - Task scheduling with Hangfire/Quartz
- **File Storage** - Local, Azure Blob, AWS S3
- **Email/Notifications** - SMTP, SendGrid, templating
- **CMS** - Content management with inline editing

### Future (Phase 3+)
- **Multi-Tenancy** - Tenant isolation and management
- **Workflow Engine** - Visual designer, approvals
- **Reporting** - Report designer, scheduled reports
- **API Gateway** - Rate limiting, authentication

See the complete [roadmap](docs/ROADMAP.md) for details.

## 🏗️ Architecture

NetMX follows a clean, 4-layer architecture for each module:

```
modules/
└── Identity/                    # Example module
    ├── Identity.Core/           # Domain layer (entities, value objects)
    ├── Identity.Contracts/      # Application contracts (DTOs, interfaces)
    ├── Identity.Application/    # Application layer (services, use cases)
    └── Identity.Web/            # Presentation layer (controllers, views)
```

**Key Principles:**
- 🎯 **Domain-Driven Design** - Rich domain models, not anemic entities
- 🔌 **Dependency Inversion** - Depend on abstractions, not implementations
- 📦 **Separation of Concerns** - Clear boundaries between layers
- 🔄 **Event-Driven** - Loose coupling via domain events

## 📚 Documentation

- **[Getting Started](docs/QUICK-START-SETUP.md)** - Complete setup guide
- **[Roadmap](docs/ROADMAP.md)** - Project vision and progress tracking
- **[GitHub Setup](docs/GITHUB-SETUP.md)** - CI/CD and deployment guide
- **[Architecture Guidelines](.github/copilot-instructions.md)** - Development standards
- **[Contributing](CONTRIBUTING.md)** - How to contribute

## 🛠️ Development

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16+ (or use Docker)
- Your favorite IDE (VS 2022, VS Code, Rider)

### Building from Source

```bash
# Clone the repository
git clone https://github.com/toonjd/netmx.git
cd netmx

# Build the framework
dotnet build framework/NetMX.sln

# Run tests (when available)
dotnet test

# Build and run the template
cd templates/modular
dotnet build NetMXApp.sln
cd src/NetMXApp.Web
dotnet run
```

### Project Structure

```
netmx/
├── framework/           # Core framework packages (10 packages)
├── modules/             # Optional feature modules (Identity, etc.)
├── templates/           # Starter templates
│   └── modular/         # Modular monolith template (minimal)
├── tools/               # CLI tool for scaffolding
├── docs/                # Documentation
├── .github/             # GitHub Actions workflows
└── scripts/             # Automation scripts
```

## 🤝 Contributing

We welcome contributions! NetMX is open source and community-driven.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📄 License

NetMX is [MIT licensed](LICENSE). Use it freely in your projects, commercial or otherwise.

## 🙏 Acknowledgments

NetMX is inspired by and builds upon the excellent work of:
- [ABP Framework](https://abp.io) - Modular architecture patterns
- [HTMX](https://htmx.org) - Hypermedia-driven interactivity
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) - World-class web framework
- The DDD community for timeless design principles

## 📊 Project Status

**Current Version:** `0.1.0-dev` (Active Development)

- ✅ Framework SDK (10 packages) - **Complete**
- ✅ Zero-warning builds - **Complete**
- ✅ Type-safe events (NetMX.Events) - **Complete**
- ✅ Identity Module - **Complete**
- ✅ CLI CRUD generation - **Complete**
- ✅ NuGet publishing (pre-release) - **Complete**
- 🔄 CLI versioning - **In Progress**
- 🔄 CreateModuleCommand - **In Progress**
- ⏳ Additional Modules (Audit, CMS) - **Planned**
- ⏳ Production release (1.0.0) - **Q1 2026**

See the [roadmap](docs/ROADMAP.md) for detailed progress and timeline.

## 💬 Community

- **GitHub Discussions** - Coming soon
- **Discord** - Coming soon
- **Twitter** - Coming soon

For now, open an [issue](https://github.com/toonjd/netmx/issues) for questions or feedback!

## 🔗 Links

- **Website**: Coming soon
- **Documentation**: [docs/](docs/)
- **NuGet Packages**: https://www.nuget.org/packages?q=netmx
- **GitHub**: https://github.com/toonjd/netmx
- **Issues**: https://github.com/toonjd/netmx/issues

---

**Built with ❤️ for the .NET community**

*NetMX - Build better web applications, faster.*

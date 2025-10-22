# NetMX - The Product

**Last Updated**: October 22, 2025  
**Status**: Active Development  
**Mission**: Empower developers to build modern web applications faster, better, and with joy

---

## 🎯 What Is NetMX?

**NetMX is a complete framework ecosystem for building web applications with .NET and HTMX.**

It's not just a library or a collection of packages—it's a **complete product** designed to give developers everything they need to build modern web applications without the complexity of heavy JavaScript frameworks.

### The Core Principle

> **Framework First, Features Optional**

NetMX provides the **infrastructure** (DDD patterns, event system, CLI tools) while keeping **features** (Identity, Audit, CMS) completely optional as plug-and-play modules.

---

## 🌐 Our Place in the Web Space

### The Problem We Solve

Modern web development has become unnecessarily complex:
- Heavy JavaScript frameworks (React, Angular, Vue) with steep learning curves
- Boilerplate code that developers write over and over
- Tight coupling between frontend and backend
- Poor developer experience with repetitive tasks
- Fragmented ecosystems with incompatible libraries

### Our Solution

**Server-Rendered HTML + HTMX + .NET**

- ✅ **Simplicity**: Server-side rendering, progressive enhancement
- ✅ **Productivity**: CLI generates 90% of boilerplate automatically
- ✅ **Type Safety**: Full C# type system, compile-time errors
- ✅ **Performance**: Minimal client-side JavaScript, fast initial loads
- ✅ **SEO**: Server-rendered HTML, search engine friendly
- ✅ **Developer Joy**: Build features, not boilerplate

---

## 📦 Product Components

NetMX is comprised of **five core product pillars**:

### 1. Templates

**Out-of-the-box project structures to hit the ground running**

#### Simple Monolith
- All code in one application
- Perfect for small to medium sites
- Driven by HTMX for interactivity
- Similar to ASP.NET MVC starter—but way better
- **Use Case**: Landing pages, blogs, small business sites, prototypes

#### Modular Monolith (Our Flagship)
- Structured, loosely coupled within one deployment
- Modules are isolated but share the same process
- Easy to reason about, easy to debug
- Can extract modules to microservices later if needed
- **Use Case**: SaaS applications, e-commerce, internal tools, most business apps

#### Microservices
- Distributed systems made easy
- Pre-configured with event bus, API gateway
- Service-to-service communication handled
- Deploy and scale independently
- **Use Case**: Large-scale applications, high-traffic systems, enterprise

**Status**: 
- ✅ Modular Monolith template available (`templates/modular/`)
- 🔄 Simple Monolith in development
- ⏳ Microservices template planned for Phase 4

---

### 2. Themes

**Beautiful, production-ready UI out of the box**

#### Basic Theme (Free)
- Clean, professional design
- Based on Bulma CSS (lightweight, modern)
- Responsive, accessible (WCAG compliant)
- Customizable via CSS variables
- **Components**: Forms, tables, cards, modals, navigation, alerts
- **Status**: ✅ Available now in templates

#### NetMX Premium Theme (Paid)
- Polished, high-end design
- Every component you'll ever need pre-designed
- Dashboard layouts, admin panels, data visualizations
- Dark mode support
- **Components**: 50+ components (charts, kanban boards, calendar, file manager, etc.)
- **Status**: ⏳ Planned for Phase 5

**Philosophy**: Developers shouldn't need to hire a designer for a professional-looking app.

---

### 3. Modules

**Easy add-on features via NuGet packages**

Modules are **self-contained feature sets** that you can add to any NetMX project:

#### Free Modules (MIT License)
- **Identity**: User management, authentication, ASP.NET Core Identity integration
- **Authorization**: Permissions, roles, policies (attribute-based, policy-based)
- **Audit**: Entity change tracking, user action logging, compliance reports
- **Settings**: Global, user, tenant-scoped settings with UI
- **Observability**: Health checks, metrics, distributed tracing

#### Paid Modules (One-Time Purchase)
- **Multi-Tenancy** ($299): Database-per-tenant, shared database, tenant isolation
- **Background Jobs** ($149): Hangfire/Quartz integration, scheduled tasks, queues
- **Email/SMS** ($149): Templates, SMTP/SendGrid/Twilio, delivery tracking
- **CMS** ($249): Content management, WYSIWYG editing, media management
- **BLOB Storage** ($149): Azure Blob, AWS S3, local file system abstraction
- **Payment Integration** ($199): Stripe, PayPal, webhooks, invoice management

**Key Features**:
- Drop-in installation: `netmx add module Identity`
- Zero configuration (sensible defaults)
- Fully customizable when needed
- Well-documented, well-tested
- Type-safe events for inter-module communication

**Status**:
- ✅ 3 free modules ready (Identity, Authorization, Audit)
- 🔄 Settings module in development
- ⏳ Paid modules starting Phase 3 (Multi-Tenancy first)

---

### 4. Tools

**Rich, powerful development tools**

#### NetMX CLI (Available Now)
**Command-line tool for rapid development**

```bash
# Create new project from template
netmx new modular ECommerceApp

# Generate complete CRUD feature (entity, DTOs, service, controller, views)
netmx generate feature Product

# Add modules to your project
netmx add module Identity

# Database operations (Rails-inspired)
netmx db migrate AddProducts
netmx db update
netmx db rollback
netmx db reset
```

**What It Does**:
- Creates projects from templates
- Generates 100% production-ready code
- Saves 4-6 hours per feature (vs manual creation)
- Applies best practices automatically (DDD, HTMX patterns, type-safe events)
- Zero configuration needed

**Status**: ✅ Core functionality complete, ongoing improvements

---

#### NetMX Studio (Planned - Phase 5)
**Customized VS Code for NetMX development**

A **forked version of VS Code** optimized for NetMX:

**Features**:
- Pre-installed NetMX extensions
- Integrated module marketplace
- Visual entity designer (drag-and-drop)
- Live HTMX preview
- Built-in observability dashboard
- Database schema viewer
- One-click deployment

**Why Fork VS Code?**
- VS Code is open source (MIT license)
- Already familiar to millions of developers
- Extensive extension ecosystem
- Cross-platform (Windows, Mac, Linux)
- We can add NetMX-specific features

**Status**: ⏳ Planned for Month 10-15

---

#### NetMX Suite (Planned - Phase 5)
**Web-based low-code/no-code solution builder**

A **SaaS platform** for visual application development:

**Features**:
- Visual project builder
- Drag-and-drop entity designer
- Visual UI designer (HTMX component builder)
- Business rules engine (visual)
- Permission designer (role/permission matrix)
- One-click deployment wizard

**Pricing**:
- **Free**: 1 project, 5 entities, NetMX branding
- **Standard** ($49/mo): Unlimited projects, export code
- **Enterprise** ($199/mo): Team collaboration, white-label

**Target Users**:
- Non-technical founders (build MVP without developers)
- Agencies (rapid prototyping for clients)
- Enterprises (standardized app development)

**Status**: ⏳ Planned for Month 12-15

---

### 5. Documentation & Community

**World-class documentation and support**

#### Documentation
- **Quick Start**: Get running in 5 minutes
- **Guides**: Step-by-step tutorials for common scenarios
- **API Reference**: Complete API documentation with examples
- **Architecture**: Deep dives into framework design
- **Video Tutorials**: Visual learning for all skill levels

#### Community
- **GitHub Discussions**: Ask questions, share projects
- **Discord Server**: Real-time help and community
- **Blog**: Architecture decisions, best practices, case studies
- **Sample Apps**: Real-world examples to learn from

**Status**: 🔄 Documentation in progress, community launching Phase 2

---

## 🎯 Technical Goals

### Short-Term (Phase 2 - Next 3 Months)
1. ✅ **Solid Foundation**: 10 framework packages, zero warnings, high test coverage
2. ✅ **Event System**: Type-safe events for component communication
3. 🔄 **CLI Maturity**: `netmx new`, `netmx generate`, `netmx db` commands fully working
4. 🔄 **Core Modules**: Identity, Authorization, Audit, Settings production-ready
5. ⏳ **Template Completion**: Simple Monolith template

### Mid-Term (Phase 3-4 - Months 4-9)
1. First paid module (Multi-Tenancy) launched
2. 8 modules available (4 free, 4 paid)
3. Microservices template ready
4. Premium theme designed and implemented
5. 50% feature parity with ABP Framework

### Long-Term (Phase 5-6 - Months 10-18)
1. NetMX Studio (VS Code fork) launched
2. NetMX Suite (SaaS) beta released
3. 15+ modules available
4. Visual Studio integration
5. 80% feature parity with ABP Framework

---

## 🏆 Success Metrics

**We measure success by developer productivity and satisfaction:**

1. **Time Saved**: 
   - Generating a CRUD feature: 5 seconds (vs 4-6 hours manually)
   - Creating a new project: 30 seconds (vs 2-3 hours manually)

2. **Code Quality**:
   - Zero warnings in all builds
   - 80%+ test coverage
   - Type-safe everywhere (no magic strings)

3. **Developer Experience**:
   - "Wow" moment in first 5 minutes
   - Documentation clarity (can find answer in < 2 minutes)
   - Support responsiveness (< 24 hours for issues)

4. **Adoption**:
   - GitHub stars (target: 20K+ by Month 18)
   - NuGet downloads (target: 100K+ by Month 18)
   - Active community (Discord members, GitHub discussions)

---

## 🚀 What Makes NetMX Different?

| Feature | NetMX | ABP Framework | ASP.NET Core |
|---------|-------|---------------|--------------|
| **JavaScript Framework** | None (HTMX) | Angular/Blazor | None |
| **CLI** | Type-safe, fast | Available | Limited |
| **Templates** | 3 types | 4 types | Basic |
| **Themes** | 2 (Basic + Premium) | Multiple | None |
| **Modules** | Free + Paid | Free + Commercial | None |
| **DDD Support** | First-class | First-class | Minimal |
| **Learning Curve** | Low (server-side) | High (Angular/Blazor) | Medium |
| **Pricing** | One-time purchase | Subscription | Free |
| **Event System** | Type-safe HTMX events | Domain events | None |
| **Visual Tools** | Studio + Suite (planned) | ABP Studio + Suite | None |

**Our Competitive Edge**:
1. **HTMX-First**: Simpler than Blazor, more powerful than MVC
2. **Better DX**: Faster CLI, better IntelliSense, type-safe everywhere
3. **Better Pricing**: One-time purchase (vs ABP's subscription)
4. **Observability**: Built-in from day one (not bolted on)
5. **Modern .NET**: .NET 9+, latest patterns, async/await everywhere

---

## 💡 Core Philosophy

> **Make developers' lives easier. Empower them to ship better products faster. And have fun doing it.**

We believe:
- **Simplicity > Complexity**: Choose the simpler solution when possible
- **Convention > Configuration**: Sensible defaults, override when needed
- **Type Safety > Runtime Errors**: Catch bugs at compile-time
- **Documentation > Assumptions**: If it's not documented, it doesn't exist
- **Testing > Hoping**: If it's not tested, it's broken
- **Observability > Debugging**: Instrument everything from day one

---

## 🎯 Summary

**NetMX is a complete framework ecosystem** for building web applications with:
- **Templates**: Simple, Modular, Microservices
- **Themes**: Basic (free), Premium (paid)
- **Modules**: Identity, Auth, Audit, CMS, Multi-Tenancy, Jobs, Email, etc.
- **Tools**: CLI, NetMX Studio, NetMX Suite
- **Documentation**: Comprehensive, beginner-friendly, example-rich

**Our Mission**: Make web development with .NET and HTMX the most productive, enjoyable experience possible.

**Our Goal**: Become the go-to framework for .NET developers who value simplicity, productivity, and type safety over heavy JavaScript frameworks.

---

**Next**: Read [INSPIRATION.md](INSPIRATION.md) to understand our design philosophy and influences.

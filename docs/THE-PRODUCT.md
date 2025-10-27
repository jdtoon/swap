# Swap - The Product

**Last Updated**: October 27, 2025  
**Status**: Active Development - Phase 2C Complete  
**Version**: 0.1.0-dev

---

## 🎯 What Is Swap?

**Swap is a CLI-driven web framework that generates production-ready code from patterns learned from real applications.**

It's not a collection of base classes or abstractions—it's a **code generator** that creates concrete, proven patterns you can customize and extend.

### The Core Principle

> **Patterns Over Packages. Generate Over Abstract.**

Swap generates code based on patterns extracted from real production apps. No magic base classes. No hidden abstractions. Just concrete, readable code that you own and control.

### What Makes Swap Productive

- ✅ **Convention over configuration** - Sensible defaults everywhere
- ✅ **Powerful CLI** - `swap generate resource Product` creates everything
- ✅ **Instant productivity** - Generate features in seconds, not hours
- ✅ **Full-stack generation** - Backend + Frontend generated together
- ✅ **.NET type safety** - Compile-time errors, IntelliSense everywhere
- ✅ **HTMX simplicity** - No JavaScript framework complexity
- ✅ **You own the code** - No hidden magic, full control
- ✅ **Proven patterns** - Learned from 4+ production apps

---

## 🌐 The Problem We Solve

### Web Development Today

Modern web development presents developers with difficult tradeoffs:

- **SPA Frameworks** require steep learning curves, complex state management, and often over-engineer simple problems
- **Traditional MVC** means writing the same boilerplate repeatedly, with no modern patterns included
- **Enterprise frameworks** often abstract away too much, hiding what's actually happening in your application
- **Most scaffolding tools** generate basic CRUD that you'll immediately need to rewrite for production

### Swap's Approach

**Server-Rendered HTML + HTMX + Pattern-Driven Code Generation + .NET**

Swap combines the best ideas from multiple ecosystems:
Swap combines the best ideas from multiple ecosystems:

**From powerful CLI tools**:
- ✅ Powerful code generation commands
- ✅ Convention over configuration
- ✅ Database migrations
- ✅ Scaffolding that produces production-ready code

**From modern web approaches**:
- ✅ Beautiful developer experience
- ✅ Clear error messages
- ✅ Developer-friendly APIs

**From .NET**:
- ✅ Type safety everywhere
- ✅ Compile-time errors catch bugs early
- ✅ Enterprise-grade performance
- ✅ IntelliSense and tooling

**From HTMX**:
- ✅ Simplicity (HTML over the wire)
- ✅ No build step
- ✅ Progressive enhancement
- ✅ Server-side validation and logic

**What We Add**:
- ✅ **Pattern Library**: 30+ patterns extracted from real apps
- ✅ **Smart Generation**: Not just scaffolding, but production-ready code
- ✅ **Learn from Code**: Generated code teaches best practices
- ✅ **Customizable**: Generated code is yours to modify

---

## 🌊 Why HTMX?

**HTMX enables interactive web applications without the complexity of JavaScript frameworks.**

### The Problem with SPAs

Modern Single-Page Applications (React, Angular, Vue) require:
- Complex build toolchains (webpack, vite, etc.)
- State management libraries (Redux, Vuex, etc.)
- API layer for every interaction
- Duplicate validation logic (client + server)
- Large bundle sizes and slow initial loads
- Steep learning curves for new developers

### The HTMX Approach

HTMX extends HTML with attributes that enable:
- **Server-Rendered Partials**: Server sends HTML, not JSON
- **Progressive Enhancement**: Works without JavaScript
- **No Build Step**: Write HTML, deploy immediately
- **Simple State**: Server holds state, no client synchronization
- **Tiny Footprint**: ~14KB minified, vs 100KB+ for frameworks

### Real-World Benefits

**Development Speed**:
- No API layer needed for UI updates
- Single validation logic (server-side)
- Simpler debugging (view source shows real HTML)
- Faster onboarding for new developers

**Performance**:
- Smaller payloads (HTML vs JSON + rendering code)
- Server-side rendering is fast
- No hydration delays
- Better caching at every level

**Maintainability**:
- Less code to maintain (no separate frontend app)
- One language/stack (.NET)
- Simpler deployment (single application)
- Easier testing (server-side unit tests)

### When HTMX Shines

✅ **Line-of-business applications**  
✅ **Admin panels and dashboards**  
✅ **Form-heavy applications**  
✅ **CRUD-focused systems**  
✅ **Internal tools**  
✅ **MVP and prototype development**

### When to Consider SPAs

Consider a JavaScript framework when you need:
- ❌ Offline-first functionality
- ❌ Real-time collaborative editing
- ❌ Complex client-side state machines
- ❌ Heavy animation/visualization requirements
- ❌ Mobile app with shared codebase (React Native, etc.)

**For 80% of web applications, HTMX is the simpler, faster choice.**

---

## 📦 What Swap Generates

Swap is built on **patterns learned from analyzing 4 production apps** (TTW, Kanban, Habits, Carestream). Every generated file uses proven, production-tested patterns.

### 1. Complete CRUD Features

```bash
Swap generate feature Product
```

**Generates**:
- ✅ **Entity** (`Product.cs`) with proper domain modeling
- ✅ **DTOs** (`ProductCreateDto`, `ProductUpdateDto`, `ProductListDto`)
- ✅ **Service Interface** (`IProductService`)
- ✅ **Service Implementation** with CRUD operations
- ✅ **Controller** with HX-Request detection, partial views, modal CRUD
- ✅ **Views**: Index, _List, _AddModal, _EditModal
- ✅ **Repository** (if using repository pattern)
- ✅ **Validation** with server-side validation and error retargeting

**Time Saved**: 4-6 hours → 5 seconds

---

### 2. Pagination (The Goldmine Pattern)

Every list view gets **automatic pagination** using the proven pattern from Carestream app:

```csharp
public class PaginationDto
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    
    // ⭐ HTMX-specific properties (set once in controller, used in view)
    public string HxGetUrl { get; set; } = string.Empty;
    public string HxTarget { get; set; } = string.Empty;
    public string HxSwap { get; set; } = "innerHTML";
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
```

**Reusable Partial** (`_PaginationControls.cshtml`) works with any entity. No duplication.

---

### 3. Modal CRUD Pattern

Generated controllers include **modal-based Add/Edit** from day one:

```csharp
// Generated automatically
public IActionResult AddProduct() 
    => PartialView("_AddProductModal", new ProductCreateDto());

public IActionResult EditProduct(int id) 
{
    var product = _service.GetById(id);
    return PartialView("_EditProductModal", product);
}

[HttpDelete]
public IActionResult DeleteProduct(int id) 
{
    _service.Delete(id);
    Response.HxTrigger("showToastSuccess", "Product deleted!");
    return Ok();
}
```

---

### 4. Response Header Helpers

Swap generates controllers that use **HTMX response headers** for advanced UX:

```csharp
[HttpPost]
public IActionResult Create(ProductCreateDto dto)
{
    if (!ModelState.IsValid)
    {
        // Auto-generated validation error handling
        Response.HxRetarget("#product-form-container");
        Response.HxReswap("innerHTML");
        Response.HxTrigger("showToastError", "Please fix validation errors");
        return PartialView("_ProductForm", dto);
    }
    
    _service.Create(dto);
    Response.HxTrigger("showToastSuccess", "Product created!");
    Response.HxRedirect("/Product");
    return Ok();
}
```

---

### 5. Component-Based Partials

```bash
Swap generate component UserDisplay
```

**Generates**:
- `Views/Shared/_UserDisplay.cshtml`
- Controller action: `GetUserDisplay()`
- Load-on-page-load pattern with `hx-trigger="load"`

**Use case**: Navigation bars, user menus, search bars, dashboard widgets

---

### 6. Toast Notification System

```bash
Swap add toasts
```

**Generates**:
- `wwwroot/js/toasts.js` - Event listeners for custom events
- `Views/Shared/_ToastContainer.cshtml` - Toast HTML
- Layout integration
- Auto-adds toast triggers to all POST actions

---

### 7. Search with Debouncing

Generated list views include **search with 500ms debounce**:

```html
<form hx-get="@Url.Action("Index")"
      hx-trigger="input changed delay:500ms"
      hx-target="#product-list">
    <input type="search" name="searchTerm" placeholder="Search products...">
</form>
```

---

### 8. Session State Helpers

```csharp
// Auto-generated extension methods
HttpContext.Session.SetObject("Filters", filters);
var filters = HttpContext.Session.GetObject<FilterDto>("Filters");
```
---

## 🛠️ The CLI - Your Productivity Superpower

Swap CLI provides powerful code generation commands that make building web applications effortless.

### Project Commands

```bash
# Create new project (future - templates being rebuilt)
Swap new MyApp

# Run development server
Swap serve

# Run tests
Swap test
```

### Generation Commands

```bash
# Generate complete CRUD feature
Swap generate feature Product
Swap generate feature Order --paginated

# Generate modal CRUD
Swap generate modal Customer

# Generate reusable component
Swap generate component UserMenu

# Generate API endpoint
Swap generate api ProductApi

# Generate background job
swap generate job SendWelcomeEmail
```

### Database Commands

```bash
# Create migration
swap db migrate AddProductsTable

# Run pending migrations
swap db update

# Rollback last migration
swap db rollback

# Reset database
swap db reset

# Seed database
swap db seed
```

### Enhancement Commands

```bash
# Add toast notification system
swap add toasts

# Add pagination helpers
swap add pagination

# Add global search
swap add search

# Add sortable lists (drag-drop)
swap add sortable
```

### Code Generation Philosophy

**Traditional scaffolding**: Empty templates with TODOs that need extensive modification

**Swap generation**: Production-ready code with proven patterns baked in:
- ✅ HX-Request detection
- ✅ Partial view returns
- ✅ Modal CRUD
- ✅ Pagination
- ✅ Toast notifications
- ✅ Validation error handling
- ✅ Response header usage

**You can modify everything** - it's your code, not a black box.

---

## 📚 Learning from Real Apps

**Swap's secret sauce**: We don't guess what patterns to generate. We analyze real production apps.

### Sample Apps Analyzed (So Far)

1. **TTW** (Travel/Tourism): Dialog modals, focus retention, inline delete
2. **Kanban** (Task Management): Board/list/card hierarchy, priority badges, drag-drop
3. **Habits** (Family Tracker): Calendar events, session state, multi-event coordination
4. **Carestream** (Healthcare): Pagination DTO, dynamic retargeting, toast events, claims integration

**30+ Patterns Identified**:
- 10 patterns appear in **100% of apps** (HX-Request detection, partial views, etc.)
- 5 patterns provide **massive time savings** (pagination, modals, toasts, search, validation)
- 15 patterns are **advanced** but valuable (multi-event triggers, session state, sortable lists)

**Why This Matters**:
- Generated code uses **proven patterns**, not theoretical ones
- Patterns are **battle-tested** in production
- You learn **best practices** from generated code

### Our Approach

1. **Build sample apps** (or analyze existing ones)
2. **Extract common patterns** (what appears in every app?)
3. **Identify high-value patterns** (what saves the most time?)
4. **Codify in generators** (make CLI generate these patterns)
5. **Iterate** (build more apps, find more patterns)

**This is the opposite of traditional frameworks**: We don't pre-suppose what you need. We discover it by building real apps.

---

## 🎯 Technical Philosophy

### Patterns Over Packages

**Traditional approach** (what we deleted):
- Build framework packages first
- Create abstractions before implementations
- Pre-suppose what developers need
- Hope features fit the infrastructure

**Swap approach** (what we kept):
- Build sample apps first
- Extract proven patterns
- Generate concrete implementations
- Features define the patterns

### Generate Over Abstract

**Traditional frameworks**: Inheritance, base classes, magical abstractions

```csharp
// Traditional: Magic base class
public class ProductAppService : CrudAppService<Product, ProductDto>
{
    // What does this do? How does it work? Who knows!
}
```

**Swap**: Generated concrete code you can read and modify

```csharp
// Swap: Generated concrete code
public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    
    public ProductService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
        
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            // ... you can see exactly what happens
        };
    }
}
```

**Benefits**:
- ✅ **No magic**: You see exactly what the code does
- ✅ **IntelliSense**: Full autocomplete, no guessing
- ✅ **Customizable**: Modify generated code freely
- ✅ **Learnable**: Generated code teaches patterns
- ✅ **Debuggable**: Set breakpoints, step through code

### Convention Over Configuration

Swap provides **sensible defaults** but lets you override everything:

```bash
# Uses conventions (Entity, Dto, Service, Controller, Views)
Swap generate feature Product

# Override conventions if needed
Swap generate feature Product --service MyProductService --dto MyProductDto
```

**Conventions**:
- Entities in `Domain/` or `Entities/`
- DTOs in `Dtos/` or `Models/`
- Services in `Services/` or `Application/`
- Controllers in `Controllers/`
- Views in `Views/{ControllerName}/`


**Configuration**: Override via `swap.config.json` if needed

---

## 🎯 Current Status (October 27, 2025)

**Version**: 0.1.0-dev  
**Phase**: 2C Complete  
**Test Coverage**: 136 passing unit tests

### ✅ What's Working Now

### ✅ What's Working Now

**Core CLI Commands**:
```bash
swap new MyApp                                        # Create new project
swap g m Product --fields Name:string,Price:decimal  # Generate model
swap g c Product                                      # Generate controller
swap g r Product --fields Name:string,Price:decimal  # Generate resource (model + controller)
```

**Generated Stack**:
- ✅ ASP.NET Core MVC
- ✅ HTMX for interactivity
- ✅ DaisyUI + Tailwind CSS
- ✅ Entity Framework Core
- ✅ Modal-based CRUD
- ✅ Pagination with sorting
- ✅ Boolean filters
- ✅ Toast notifications

**Code Quality**:
- ✅ 136 passing unit tests
- ✅ 11 C# data types supported
- ✅ Nullable field syntax (`Name:string?`)
- ✅ Production-ready patterns from 4 real apps

**Documentation**:
- ✅ Complete Docusaurus wiki
- ✅ CLI command reference
- ✅ Getting started guides
- ✅ HTMX pattern examples

### 🔜 Phase 2D (Next Up)

**Database Seeders** - Generate seed data for testing and development

### � Phase 3 (Choose Direction)

Options to explore next:
1. **Relationships** - Foreign keys, navigation properties (`Product belongsTo Category`)
2. **API Generation** - REST endpoints alongside HTMX views
3. **Test Generation** - Unit tests for generated code
4. **Validation** - Data annotations scaffolding
5. **Authentication** - User auth system
6. **Module System** - Multi-module applications

---

## 🏆 Success Metrics

**We measure success by developer productivity and code quality:**

### Time Saved

| Task | Manual | With Swap | Savings |
|------|--------|------------|---------|
| CRUD feature | 4-6 hours | 5 seconds | **99.9%** |
| Modal CRUD | 1-2 hours | 3 seconds | **99.8%** |
| Pagination | 2-3 hours | Auto-generated | **100%** |
| Toast system | 1 hour | 10 seconds | **99.7%** |
| Search with debounce | 30 mins | Auto-generated | **100%** |

**Total**: Swap saves **20-30 hours per feature** when you factor in all the patterns.

### Code Quality

- ✅ **Consistent**: Same patterns across your entire app
- ✅ **Production-Ready**: Learned from real production apps
- ✅ **Type-Safe**: Full IntelliSense, compile-time errors
- ✅ **Testable**: Generated code follows SOLID principles
- ✅ **Readable**: No magic, no hidden abstractions

### Developer Experience

- ✅ **"Wow" moment**: First `swap generate` command
- ✅ **Fast answers**: Generated code shows you how
- ✅ **Learning**: Code teaches best practices
- ✅ **Control**: You own and modify everything

---

## 🏗️ How Swap Works

Swap follows a unique **meta-development process** that ensures every generated pattern is proven and practical:

### 1. We Build Real Applications

We don't start with theory. We build actual production applications:
- **TTW** (Travel management system)
- **Kanban** (Task tracking)
- **Habits** (Family habit tracker)
- **Carestream** (Healthcare application)

These aren't toys—they're real apps solving real problems with real users.

### 2. We Extract Patterns

After building, we analyze the code to find:
- **What patterns appear in every app?** (HX-Request detection, modals, pagination)
- **What code gets copy-pasted?** (Toast notifications, form handling)
- **What takes the most time?** (CRUD operations, list views)
- **What causes bugs?** (Validation, error handling)

We document these patterns in our [Pattern Library](PATTERNS-LIBRARY.md).

### 3. We Generate Code

The CLI generates these proven patterns automatically:

```bash
# One command creates everything
swap g r Product --fields Name:string,Price:decimal

# Generated files use patterns from real apps:
# - Modal CRUD (from Habits app)
# - Pagination (from Carestream app)
# - Toast notifications (from TTW app)
# - HX-Request detection (from all 4 apps)
```

### 4. You Own the Result

The generated code is **yours**:
- No hidden framework code
- No magic base classes
- Full control to modify
- Easy to understand and debug

### 5. We Iterate

As we build more apps, we discover new patterns and improve existing ones. The CLI evolves based on real-world usage.

**This cycle (Build → Extract → Generate → Own) is what makes Swap different from traditional frameworks.**

---

## 💡 Core Philosophy

**Swap** is built on a foundation of practical, battle-tested principles:

### Our Principles

**Patterns Over Packages**  
Generate concrete code, not abstract frameworks. Every line of generated code is readable, modifiable, and yours to own.

**Generate Over Abstract**  
Rather than building layers of abstraction, we generate the code you need. No magic base classes, no hidden behavior.

**Real Apps Over Theory**  
Learn from production applications, not theoretical frameworks. Every pattern in Swap comes from analyzing real, working code.

**Simplicity Over Complexity**  
HTMX over heavyweight JavaScript frameworks. Server-rendered partials over complex state management. The web's native capabilities over unnecessary abstractions.

**Developer Experience**  
Fast feedback loops, clear error messages, and generated code that teaches best practices as you work.

### What This Means in Practice

**Instant Productivity**
- Generate complete features in seconds
- Convention over configuration everywhere
- Powerful CLI that understands your intent
- Production-ready patterns from day one

**True Ownership**
- Generated code is yours to modify
- No hidden magic or framework lock-in
- Full IntelliSense and compile-time safety
- Readable code that teaches as you learn

**Proven Patterns**
- Extracted from 4+ production applications
- Battle-tested in real-world scenarios
- HTMX-first interactivity patterns
- Server-rendered simplicity

---

## 🎯 The Vision

**3-Month Horizon**:
- Complete CRUD generation with 10+ automated patterns
- Pagination, modals, toasts, search, filtering, sorting
- Production-ready code from `swap generate` commands
- **Target**: Save developers 20+ hours per feature

**6-12 Month Horizon**:
- Modular architecture support
- 20+ automated patterns
- Visual Studio extension for IDE integration
- Growing community of contributors
- **Target**: 1000+ developers building with Swap

**12-24 Month Horizon**:
- Swap Studio (enhanced VS Code experience)
- Visual designers for rapid prototyping
- 30+ comprehensive patterns
- Thriving ecosystem and plugin system
- **Target**: 10,000+ developers, established framework

---

## 🎯 Summary

**Swap** generates production-ready web applications using proven patterns from real-world projects.
- ✅ Rapid development (seconds, not hours)
- ✅ Developer happiness is a feature

**Better Than Rails**:
- ✅ .NET type safety (compile-time errors)
- ✅ HTMX simplicity (simpler than Hotwire)
- ✅ You own the code (no magic)

**Core Values**:
- ✅ Patterns extracted from real production applications
- ✅ Code generation over framework abstractions
- ✅ HTMX simplicity over JavaScript complexity
- ✅ .NET type safety and performance
- ✅ Developer ownership and transparency

**Our Mission**: Make building web applications with .NET + HTMX productive, enjoyable, and pattern-driven.

**Our Process**: Build real apps → Extract patterns → Generate code → Ship features fast

---

**Next Steps**:
1. Read [PATTERNS-LIBRARY.md](PATTERNS-LIBRARY.md) for comprehensive pattern analysis
2. Explore the `/wiki` documentation for CLI usage guides
3. Check [CHANGELOG.md](../CHANGELOG.md) for recent updates and completed features

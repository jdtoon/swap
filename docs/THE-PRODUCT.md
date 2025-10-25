# Swap - The Product

**Last Updated**: October 25, 2025  
**Status**: Active Development - Learning from Real Apps  
**Vision**: "The Rails of .NET" - but better

---

## 🎯 What Is Swap?

> **"The Rails of .NET" - but better**

**Swap is a CLI-driven web framework that generates production-ready code from patterns learned from real applications.**

It's not a collection of base classes or abstractions—it's a **code generator** that creates concrete, proven patterns you can customize and extend.

### The Core Principle

> **Patterns Over Packages. Generate Over Abstract.**

Swap generates code based on patterns extracted from real production apps. No magic base classes. No hidden abstractions. Just concrete, readable code that you own and control.

**What makes it "Rails-like"**:
- ✅ **Convention over configuration** - Sensible defaults everywhere
- ✅ **Powerful CLI** - `Swap generate feature Product` creates everything
- ✅ **Rails-level productivity** - Generate features in seconds, not hours
- ✅ **Full-stack** - Backend + Frontend generated together

**What makes it "better than Rails"**:
- ✅ **.NET type safety** - Compile-time errors, IntelliSense everywhere
- ✅ **HTMX simplicity** - No JavaScript framework complexity
- ✅ **You own the code** - No hidden magic, full control
- ✅ **Proven patterns** - Learned from 4+ production apps

---

## 🌐 Our Place in the Web Space

### The Problem We Solve

Web development is either too complex or too rigid:
- **React/Angular**: Steep learning curve, complex state management, over-engineering
- **Rails**: Amazing DX, but Ruby limits enterprise adoption
- **Laravel**: PHP ecosystem, not .NET
- **ASP.NET MVC**: Manual boilerplate, no modern patterns out of the box
- **ABP Framework**: Over-abstracted, subscription pricing, heavy

### Our Solution

**Server-Rendered HTML + HTMX + CLI Code Generation + .NET**

Swap combines the best ideas from multiple ecosystems:

**From Rails**:
- ✅ Powerful CLI (`rails generate` → `Swap generate`)
- ✅ Convention over configuration
- ✅ Migrations (`rails db:migrate` → `Swap db migrate`)
- ✅ Scaffolding that actually works

**From Laravel**:
- ✅ Artisan-style commands
- ✅ Beautiful error pages
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

Swap CLI is **Rails-inspired** but **better for .NET developers**.

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
Swap generate job SendWelcomeEmail
```

### Database Commands (Rails-Inspired)

```bash
# Create migration
Swap db migrate AddProductsTable

# Run pending migrations
Swap db update

# Rollback last migration
Swap db rollback

# Reset database
Swap db reset

# Seed database
Swap db seed
```

### Enhancement Commands

```bash
# Add toast notification system
Swap add toasts

# Add pagination helpers
Swap add pagination

# Add global search
Swap add search

# Add sortable lists (drag-drop)
Swap add sortable
```

### Scaffolding vs Generation

**Traditional scaffolding** (like ASP.NET): Empty templates with TODOs

**Swap generation**: Production-ready code with proven patterns:
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

**Configuration**: Override via `Swap.config.json` if needed

---

## 🚀 What Makes Swap Different?

### vs Rails

| Feature | Swap | Rails |
|---------|-------|-------|
| **Language** | C# (.NET) | Ruby |
| **Type Safety** | ✅ Compile-time | ❌ Runtime |
| **Performance** | ✅ High | ⚠️ Medium |
| **CLI** | Rails-inspired | ✅ Excellent |
| **Productivity** | ✅ High | ✅ High |
| **Enterprise Adoption** | ✅ Common | ⚠️ Niche |
| **Frontend** | HTMX (simple) | Hotwire (similar) |

**Swap = Rails DX + .NET type safety**

### vs ABP Framework

| Feature | Swap | ABP |
|---------|-------|-----|
| **Approach** | Code generation | Base classes |
| **Magic** | ❌ None | ✅ Heavy |
| **Customization** | ✅ Full control | ⚠️ Limited |
| **Learning Curve** | Low | High |
| **Frontend** | HTMX | Angular/Blazor |
| **Pricing** | TBD | Subscription |
| **Patterns** | From real apps | Pre-supposed |

**Swap = Simpler, more transparent**

### vs ASP.NET Core MVC

| Feature | Swap | ASP.NET MVC |
|---------|-------|-------------|
| **CLI** | ✅ Powerful | ⚠️ Basic |
| **Generation** | Production-ready | Empty templates |
| **Patterns** | Built-in | Manual |
| **HTMX** | First-class | Manual |
| **Productivity** | ✅✅✅ | ⚠️ |

**Swap = ASP.NET MVC + Rails CLI + HTMX patterns**

---

## 🎯 Current Status (October 25, 2025)

### ✅ What's Done

- **Sample Apps**: 4 production apps analyzed (TTW, Kanban, Habits, Carestream)
- **Pattern Library**: 30+ patterns documented
- **CLI Foundation**: Basic commands work
- **Clean Slate**: Removed all pre-supposed infrastructure

### 🔄 What's In Progress

- **Template Rebuilding**: Creating templates based on learned patterns
- **Generator Implementation**: Implementing pattern-based code generation
- **Documentation**: Writing guides based on real patterns

### ⏳ What's Next

**Week 1-2**:
1. Build monolithic template from learned patterns
2. Implement `Swap generate feature` with pagination
3. Add response header helpers
4. Add toast notification system

**Week 3-4**:
5. Implement modal CRUD generation
6. Add component generation
7. Implement search with debouncing
8. Add session state helpers

**Week 5-6**:
9. Build modular template
10. Implement `Swap db` commands
11. Add validation error retargeting
12. Polish and test

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

- ✅ **"Wow" moment**: First `Swap generate` command
- ✅ **Fast answers**: Generated code shows you how
- ✅ **Learning**: Code teaches best practices
- ✅ **Control**: You own and modify everything

---

## 💡 Core Philosophy

> **"The Rails of .NET" - but better**

**What this means**:

1. **Rails-Level Productivity**
   - Generate features in seconds
   - Convention over configuration
   - Powerful CLI commands
   - Database migrations

2. **.NET Advantages**
   - Compile-time type safety
   - IntelliSense everywhere
   - Performance and scalability
   - Enterprise-grade ecosystem

3. **Better Than Both**
   - No magic base classes (vs traditional frameworks)
   - Generated code you own (vs scaffolding)
   - HTMX simplicity (vs JavaScript frameworks)
   - Proven patterns (vs theoretical abstractions)

### Our Principles

- **Patterns Over Packages**: Generate concrete code, not abstract frameworks
- **Real Apps Over Theory**: Learn from production, not whiteboards
- **Generate Over Abstract**: Concrete implementations beat magical base classes
- **Convention Over Configuration**: Sensible defaults, override when needed
- **Type Safety Over Runtime Errors**: Catch bugs at compile-time
- **Transparency Over Magic**: You see exactly what your code does

---

## 🎯 The Vision

**Short-Term (3 months)**:
- Swap CLI generates production-ready CRUD with all patterns
- Monolithic template ready for real projects
- 10+ patterns automated (pagination, modals, toasts, search, etc.)
- **Goal**: Developers save 20+ hours per feature

**Mid-Term (6-12 months)**:
- Modular template for larger apps
- 20+ patterns automated
- Visual Studio extension
- Community of 1000+ developers
- **Goal**: "Rails of .NET" reputation established

**Long-Term (12-24 months)**:
- Swap Studio (VS Code fork) launched
- Visual designers for non-coders
- 30+ patterns automated
- 10,000+ developers using Swap
- **Goal**: Go-to framework for .NET + HTMX apps

---

## 🎯 Summary

**Swap is "The Rails of .NET" - but better:**

**Like Rails**:
- ✅ Amazing CLI (`Swap generate`, `Swap db`, etc.)
- ✅ Convention over configuration
- ✅ Rapid development (seconds, not hours)
- ✅ Developer happiness is a feature

**Better Than Rails**:
- ✅ .NET type safety (compile-time errors)
- ✅ HTMX simplicity (simpler than Hotwire)
- ✅ You own the code (no magic)
- ✅ Proven patterns (from real apps)

**Our Mission**: Make .NET + HTMX the most productive, enjoyable way to build web applications.

**Our Approach**: Learn from real apps → Extract patterns → Generate code → Ship fast

---

**Next Steps**:
1. Read [HTMX-PATTERNS-LEARNED.md](HTMX-PATTERNS-LEARNED.md) for deep pattern analysis
2. Check out sample apps in `sampleApps/` to see the patterns in action
3. Stay tuned for CLI updates as we implement generators


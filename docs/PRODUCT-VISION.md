# Swap Product Vision

**Last Updated**: November 1, 2025  
**Status**: Vision Reset & Refocus

---

## 🎯 The Core Vision

**Swap is a developer productivity toolkit that provides:**

1. **Base Templates** - Ready-to-run project architectures
2. **Component Templates** - Drop-in HTMX UI code you own
3. **CLI Orchestration** - Tools to wire everything together seamlessly

This is NOT a code generation framework focused on database-driven CRUD. That exists, but it's secondary to the main goal: **making it easy for developers to pick up and build apps quickly**.

---

## 🏗️ The Three Pillars

### 1. Base Templates (Project Archetypes)

**What they are:** Fully-configured, production-ready project structures that developers can start with.

**Available Templates:**
- ✅ **Monolith** - Single deployable ASP.NET Core app
  - Location: `templates/monolith/`
  - CLI: `swap new MyApp`
  - Stack: ASP.NET Core MVC + EF Core + HTMX + DaisyUI + Tailwind
  - Pre-configured: Authentication, database, migrations, Docker

**Planned Templates:**
- 🔜 **Layered Architecture** - Clean separation of concerns
  - Presentation → Application → Domain → Infrastructure
  - Use case: Traditional N-tier applications
  
- 🔜 **Modular Monolith** - Domain-driven modules within single deployment
  - Each module: Own controllers, views, services, data models
  - Use case: Large apps that need organization but not microservices
  
- 🔜 **Microservices** - Distributed system with separate services
  - API Gateway + Service Template + Shared Infrastructure
  - Use case: Independently deployable, scalable services

**Template Philosophy:**
- Templates are **complete starting points**, not scaffolds
- They include **best practices**, sensible folder structure, and working examples
- Developers can run `swap new` and immediately start building features
- No "TODO: Configure this" - everything works out of the box

---

### 2. Component Templates (Drop-in code you own)

**What they are:** Battle-tested UI templates (partials, controllers, assets) that use the "HTMX Container" pattern and are copied into your app as source. You fully own and customize them—no framework package required.

**The Container Pattern** (from `framework/Swap.Htmx/`):
```csharp
// SwapController - automatically handles full page vs partial rendering
public class ArticlesController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var articles = await _service.GetArticlesAsync();
        return SwapView(articles); // Returns full page OR partial based on HX-Request header
    }
}
```

**Component Categories:**

#### **Layout Components** (Shell Containers)
- Navigation bars (top nav, sidebar nav)
- Breadcrumbs
- Page headers with actions
- Footer sections

#### **Data Display Components**
- Tables (sortable, paginated, filterable)
- Cards/Grid layouts
- Lists (simple, detailed, nested)
- Data badges/chips
- Empty state messages

#### **Form Components**
- Form containers with validation
- Input fields (text, number, date, select, checkbox, radio)
- Multi-step forms
- File upload widgets
- Search bars with debounce

#### **Interaction Components**
- Modals (create, edit, confirm delete)
- Dropdowns/Select menus
- Tabs
- Accordions
- Tooltips
- Toast notifications
- Loading spinners/skeletons

#### **Specialized Components**
- User menus/avatars
- Notification panels
- Calendar/Date pickers
- Tag inputs
- Rich text editors (Markdown, TinyMCE integration)

**Component API:**

Each component is:
1. **Self-contained** - HTML + HTMX attributes + minimal CSS
2. **Composable** - Works with other components
3. **Server-driven** - State lives on server, uses `SwapController`
4. **Documented** - Clear examples and props/parameters
5. **Themeable** - Uses DaisyUI classes (or user's CSS framework)

**Template locations:**
```
templates/components/           # Source templates shipped with Swap
├── layout/
├── data-display/
├── forms/
├── interaction/
└── specialized/

# After installation via CLI, files are copied into your app, e.g.:
YourApp/
├── Views/Components/Table/
├── Views/Shared/_Modal.cshtml
├── wwwroot/js/components/
└── Controllers/Partials/
```

**Usage Example:**
```csharp
// In controller
public class ProductsController : SwapController
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var model = new ProductListViewModel
        {
            Products = await _service.SearchAsync(search, page, pageSize: 25),
            Pagination = new PaginationModel { CurrentPage = page, ... }
        };
        return SwapView(model);
    }
}
```

```html
<!-- In view - using ready-made components -->
@model ProductListViewModel

<!-- Page Header Component -->
<partial name="_PageHeader" model="new { Title = 'Products', ActionText = 'Add Product', ActionUrl = Url.Action('Create') }" />

<!-- Search Component -->
<partial name="_SearchBar" model="new { Placeholder = 'Search products...', HxGet = Url.Action('Index'), HxTarget = '#product-list' }" />

<!-- Table Component -->
<div id="product-list">
    <partial name="_Table" model="Model.Products" />
    <partial name="_Pagination" model="Model.Pagination" />
</div>

<!-- Modal Component (loaded on demand) -->
<div id="modal-container"></div>
```

**The Goal:** Developers don't write repetitive HTML. They compose components.

---

### 3. CLI Orchestration (The Wiring System)

**What it does:** The CLI handles setup, configuration, and connecting pieces together.

**Current CLI Commands:**

```bash
# Project initialization
swap new MyApp                    # Create from template (monolith, layered, modular, microservices)
swap new MyApp --template layered # Choose specific architecture

# Component installation
swap add component table          # Add table component to project
swap add component modal          # Add modal component
swap add components layout        # Add all layout components

# Configuration
swap config db sqlserver          # Switch database provider
swap config theme custom          # Use custom CSS instead of DaisyUI

# Development helpers
swap doctor                       # Check environment setup
swap list                         # List installed components/resources
swap db info                      # Show database information
swap db migrate AddFeature        # Create migration
```

**Planned CLI Features:**

```bash
# Template management
swap template list                # Show available templates
swap template install <url>       # Install custom template

# Component browsing
swap component list               # List all available components
swap component preview <name>     # Preview component in browser
swap component docs <name>        # Show component documentation

# Module management (for modular monolith)
swap module add Orders            # Add new module to modular monolith
swap module list                  # List all modules

# Service management (for microservices)
swap service add ProductCatalog   # Add new microservice
swap service list                 # List all services
```

**CLI Philosophy:**
- CLI is a **productivity multiplier**, not a code generator
- Commands are **intuitive** and **discoverable** (good help text, examples)
- CLI **wires things together** but doesn't hide what's happening
- Developers can do everything manually if they want - CLI just speeds it up

---

## 📦 Current State vs. Vision

### ✅ What's Built (Current)

**Templates:**
- ✅ Monolith template (`templates/monolith/`)
- ✅ `swap new MyApp` command works

**Framework:**
- ✅ Swap.Htmx package with `SwapController` base class
- ✅ HTMX response header helpers (HxTrigger, HxRedirect, etc.)
- ✅ Middleware for enforcing partial responses

**CLI:**
- ✅ `swap new` - Create monolith project
- ✅ `swap generate` - CRUD scaffolding (model, controller, views)
- ✅ `swap db` - Database migrations
- ✅ `swap doctor` - Environment checks
- ✅ `swap list` - Resource listing

**Components (Partial):**
- ⚠️ Some components exist in generated views (tables, modals, pagination)
- ⚠️ Not extracted into reusable library yet
- ⚠️ Mixed with CRUD generation code

### 🔄 What Needs Refocusing

**Problems with current direction:**
1. **Too much focus on CRUD generation** - This became the primary feature instead of templates + components
2. **Database-driven UI complexity** - Relationships, foreign keys, dropdowns got too complex
3. **Component library not separate** - Components are embedded in CRUD templates, not standalone
4. **Missing architecture templates** - Only have monolith, need layered/modular/microservices
5. **CLI is code generator first** - Should be orchestrator/installer first, generator second

**What to change:**
1. Extract components from CRUD templates into installable component templates (copied into the app by the CLI)
2. Keep CRUD generation as **optional addon**, not core feature
3. Build out remaining templates (layered, modular, microservices)
4. Enhance CLI with component browsing/installation commands (that copy templates, not add packages)
5. Create component documentation site (preview, props, examples)

---

## 🎯 Revised Roadmap

### Phase 1: Component Templates Extraction (Next)

**Goal:** Turn embedded CRUD components into standalone templates that are copied into the app (no framework package).

**Tasks:**
1. Create a `templates/components/` source tree
2. Extract existing templates:
   - Table with sorting/pagination
   - Modal (create, edit, delete confirm)
   - Toast notifications
   - Search bar with debounce
   - Form validation display
   - Pagination controls
3. Document each component with examples
4. Add `swap add component <name>` CLI command
5. Update monolith template to use component templates by default

**Outcome:** Developers can use components without CRUD generation.

---

### Phase 2: Architecture Templates

**Goal:** Provide multiple starting architectures.

**Tasks:**
1. **Layered Architecture Template:**
   - Folder structure: `/Presentation`, `/Application`, `/Domain`, `/Infrastructure`
   - Example: Orders feature across layers
   - Documentation: When to use layered vs. monolith

2. **Modular Monolith Template:**
   - Folder structure: `/Modules/Orders`, `/Modules/Products`, `/Modules/Customers`
   - Each module: Controllers, Services, Data, Views
   - Documentation: Module boundaries, shared infrastructure

3. **Microservices Template:**
   - API Gateway project
   - Service template (example: ProductCatalog service)
   - Shared infrastructure (logging, auth, messaging)
   - Docker Compose orchestration
   - Documentation: When to use microservices

4. Update CLI:
   ```bash
   swap new MyApp --template monolith      # Default
   swap new MyApp --template layered
   swap new MyApp --template modular
   swap new MyApp --template microservices
   ```

**Outcome:** Developers choose architecture upfront, not retrofit later.

---

### Phase 3: Component Gallery & Documentation

**Goal:** Make components discoverable and easy to use.

**Tasks:**
1. Build component gallery website (part of wiki or separate)
2. Live previews for each component
3. Copy-paste code snippets
4. Props/parameters reference
5. Composition examples (how to combine components)
6. CLI command to preview components locally:
   ```bash
   swap component preview table
   # Opens browser with live table example
   ```

**Outcome:** Developers can browse components like Shadcn UI or DaisyUI.

---

### Phase 4: Enhanced CLI Orchestration

**Goal:** CLI becomes primary developer interface.

**Tasks:**
1. Template management:
   - `swap template list`
   - `swap template install <github-url>` (custom templates)
2. Component management:
   - `swap component search <keyword>`
   - `swap add components forms` (install category)
3. Module management (modular monolith):
   - `swap module add Orders`
   - `swap module remove Orders`
4. Service management (microservices):
   - `swap service add ProductCatalog`
   - `swap service list`
5. Better help and discoverability:
   - `swap help` with command tree
   - Examples in every command

**Outcome:** CLI orchestrates entire development workflow.

---

## 🚫 What We're NOT Building

To stay focused, here's what Swap is explicitly **not**:

❌ **Database-first CRUD framework** - We have basic CRUD generation, but it's not the focus  
❌ **ORM abstraction layer** - Use EF Core directly  
❌ **Form builder with drag-drop designer** - Components are code-first  
❌ **Low-code platform** - Developers write code, just faster  
❌ **API-first framework** - We're server-rendered HTML first  
❌ **Pattern library for domain patterns** - Swap.Patterns can exist, but separate concern  
❌ **Enterprise framework with abstractions** - Keep it concrete and simple  

---

## 🎯 Success Metrics

**How we measure if Swap achieves its vision:**

### Developer Experience
- ⏱️ **Time to first feature:** Developer runs `swap new MyApp`, adds a component, has working page in <10 minutes
- 📚 **Discoverability:** Developer can find and use components without reading extensive docs
- 🔧 **Flexibility:** Developer can eject/customize any component without breaking everything
- 🎓 **Learning curve:** Junior developer can be productive in <1 day

### Adoption Metrics
- 📦 **Templates used:** Downloads of different architecture templates
- 🧩 **Components used:** Which components get installed most
- ⭐ **GitHub stars:** Community interest
- 💬 **Community feedback:** Developers report "I shipped faster"

### Technical Quality
- ✅ **Zero magic:** All generated code is readable and modifiable
- 📖 **Documentation:** Every component has working example
- 🧪 **Tested:** Components have test coverage
- ⚡ **Performance:** Server-rendered pages load fast (<200ms)

---

## 💭 Philosophical Foundations

### Components Over Code Generation

**Traditional approach:** Generate custom code for every feature.

**Swap approach:** Provide pre-built components, compose them together.

**Why better:**
- Components are **tested and maintained**
- Updates flow to all users (NuGet update)
- Less generated code to maintain
- Focus on composition, not customization

### Templates Over Frameworks

**Traditional approach:** Framework provides base classes and abstractions.

**Swap approach:** Templates provide concrete, working code you own.

**Why better:**
- No vendor lock-in
- Full control over architecture
- No "magic" behavior to debug
- Learn by reading generated code

### CLI as Orchestrator, Not Generator

**Traditional approach:** CLI generates tons of code.

**Swap approach:** CLI installs component templates and wires them together (by copying source into your app).

**Why better:**
- Less code to maintain
- Faster updates (change component, not generated code)
- Clear separation: templates (your code) vs. components (library code)
- Developer stays in control

---

## 📝 Next Steps

### Immediate Actions (This Week)

1. **Create `templates/components/` source tree**
   - Extract table, modal, toast, pagination, search templates
   - Provide lightweight helpers only where needed (no framework base classes)
   - Add README with usage examples

2. **Update CRUD generation to use templates**
   - Replace inline HTML with `<partial name="_Table" />`
   - Make CRUD generation copy component templates as needed
   - Clean up template bloat

3. **Document container architecture**
   - Write `docs/CONTAINER-PATTERN.md`
   - Explain SwapController + SwapView
   - Show component composition examples

### Short Term (This Month)

4. **Build layered architecture template**
   - `/Presentation`, `/Application`, `/Domain`, `/Infrastructure` structure
   - Working example (Orders feature)
   - Documentation

5. **Component gallery mockup**
   - List of all components
   - Props/parameters
   - Basic examples

### Medium Term (Next 3 Months)

6. **Build modular monolith template**
7. **Build microservices template**
8. **Full component gallery website**
9. **Enhanced CLI with component/template management**
10. **Community feedback loop** (Discord/GitHub Discussions)

---

## 🤝 How This Changes Development Approach

### Before (CRUD-focused)

```bash
# Developer workflow
swap new MyApp
swap generate resource Product --fields Name:string,Price:decimal
# → Generates: Model, Controller, Views, Service, Repository
# → 500+ lines of generated code
# → Developer maintains all of it
```

### After (Component-focused)

```bash
# Developer workflow
swap new MyApp --template modular
swap add components layout
swap add components forms
swap add component table

# Developer writes controller:
public class ProductsController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var products = await _service.GetProductsAsync();
        return SwapView(products);
    }
}

# Developer writes view:
# @model List<Product>
# <partial name="_PageHeader" model="..." />
# <partial name="_Table" model="Model" />

# → Templates are copied into your app (you own and customize)
# → Developer writes business logic + composition
# → ~50 lines of code vs. 500+
```

**Result:** Less generated code, more reusable components, faster development.

---

## 📚 Related Documentation

- [Templates README](../templates/README.md) - Template structure and variables
- [Swap.Htmx README](../framework/Swap.Htmx/README.md) - Container pattern documentation
- [THE-PRODUCT.md](THE-PRODUCT.md) - Original product vision (needs update)

---

## ✨ Vision Summary

**Swap in one sentence:**

> Swap provides ready-to-use project templates, composable HTMX component templates (source code), and a CLI to wire it all together—so developers can build web apps fast without wrestling with boilerplate.

**Core pillars:**
1. 🏗️ **Templates** - Start with the right architecture
2. 🧩 **Component Templates** - Compose UI from pre-built pieces you own
3. ⚡ **CLI** - Orchestrate and install seamlessly

**What makes it different:**
- ✅ Components over code generation
- ✅ Templates over frameworks
- ✅ HTMX simplicity over JavaScript complexity
- ✅ You own the code, no magic

**The goal:** Make .NET + HTMX as productive as Rails/Laravel, without sacrificing type safety or developer control.

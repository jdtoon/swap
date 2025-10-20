# NetMX Terminology Guide

**Purpose**: Clear, unambiguous definitions for NetMX concepts  
**Audience**: All developers using NetMX

## Core Concepts

### 🏗️ Module

**Definition**: A **reusable package** containing related features that can be shared across multiple projects.

**Characteristics**:
- Located in `modules/ModuleName/`
- Has 4-layer structure: Core, Contracts, Application, Web
- Includes `module.json` descriptor
- Can be packaged as NuGet packages
- Provides infrastructure + features

**Examples**:
- **Identity** - Authentication, authorization, user management
- **Audit** - Audit logging, change tracking, compliance
- **CMS** - Content management, pages, media
- **Email** - Email templates, SMTP, queuing

**CLI Commands**:
```bash
# Create new module
netmx create module Audit

# Add existing module to project
netmx add module Identity

# List available modules
netmx list modules
```

**When to Create a Module**:
- ✅ Feature is reusable across projects
- ✅ Feature has clear boundaries
- ✅ Feature will be maintained independently
- ✅ Feature can be open-sourced or sold

**Structure**:
```
modules/Identity/
├── Identity.Core/              # Domain entities, value objects
│   ├── Entities/
│   │   ├── AppUser.cs
│   │   └── AppRole.cs
│   └── ValueObjects/
├── Identity.Contracts/         # DTOs, service interfaces
│   ├── Dtos/
│   │   ├── UserDto.cs
│   │   └── RoleDto.cs
│   └── Services/
│       └── IUserService.cs
├── Identity.Application/       # Service implementations, use cases
│   ├── Services/
│   │   └── UserService.cs
│   └── UseCases/
├── Identity.Web/              # Controllers, views, Razor components
│   ├── Controllers/
│   │   └── UsersController.cs
│   └── Views/
│       └── Users/
└── module.json               # Module metadata
```

---

### ⚡ Feature

**Definition**: A **single business entity** with complete CRUD operations (entity, DTOs, service, controller, views).

**Characteristics**:
- Can be in a **module** (reusable) or **app** (app-specific)
- Generated with `netmx generate feature`
- Includes all layers: domain, contracts, application, presentation
- Has HTMX patterns built-in
- Follows DDD best practices

**Examples**:
- **In a module**: AuditLog (Audit module), BlogPost (CMS module)
- **In an app**: Product (e-commerce app), Order (e-commerce app)

**CLI Commands**:
```bash
# Generate feature in current app
netmx generate feature Product

# Generate feature in a module
netmx generate feature AuditLog -m Audit

# With additional options
netmx generate feature Product --search --export
```

**What Gets Generated**:
1. **Entity** - `Models/Product.cs` (or `Audit.Core/Entities/AuditLog.cs`)
2. **DTOs** - Read, Create, Update DTOs
3. **Service Interface** - `IProductService.cs`
4. **Service Implementation** - `ProductService.cs`
5. **Controller** - With HTMX helpers (`HxTrigger`, `HxReswap`)
6. **Views** - Index, _List (table), _Form (create/edit)

**Generated Code Includes**:
- ✅ Validation attributes
- ✅ HTMX patterns (hx-get, hx-post, hx-delete, hx-trigger)
- ✅ Event-driven architecture
- ✅ Delete confirmation
- ✅ Inline editing
- ✅ Bulma CSS styling
- ✅ Font Awesome icons

**When to Use**:
- ✅ You need standard CRUD operations
- ✅ Entity follows common patterns
- ✅ You want HTMX patterns automatically
- ✅ You want to save 2+ hours of manual work

---

### 🎨 Component

**Definition**: A **reusable HTMX UI pattern** that can be shared across views and projects.

**Characteristics**:
- Typically Razor partial views or view components
- Implements specific HTMX pattern (click-to-edit, infinite scroll, etc.)
- Can be packaged in Razor class libraries
- Self-contained with styles and behavior

**Examples**:
- **ContactCard** - Display + inline edit for contact info
- **FileUpload** - HTMX file upload with progress
- **SearchBox** - Debounced search with results
- **DataTable** - Table with sorting, filtering, pagination
- **InfiniteScroll** - Lazy loading content
- **Toast** - HTMX-driven notifications

**CLI Commands** (Future):
```bash
# Generate new component
netmx generate component ContactCard

# List available components
netmx list components

# Add component library to project
netmx add components
```

**Current Status**: Manual creation  
**Future**: CLI-generated with best practices

**When to Create**:
- ✅ UI pattern is reused multiple times
- ✅ Pattern is complex (HTMX + CSS + validation)
- ✅ You want consistent behavior across app
- ✅ Pattern can benefit other developers

---

## Comparison Table

| Aspect | Module | Feature | Component |
|--------|--------|---------|-----------|
| **Scope** | Multiple features | Single entity | UI pattern |
| **Reusability** | High (cross-project) | Medium (within module/app) | High (cross-project) |
| **Location** | `modules/` | Module or app | Views or class library |
| **Layers** | 4 (Core, Contracts, Application, Web) | Generated in appropriate layers | Presentation only |
| **CLI Command** | `netmx create module` | `netmx generate feature` | `netmx generate component` |
| **Examples** | Identity, Audit, CMS | Product, AuditLog, BlogPost | ContactCard, FileUpload |
| **Contains** | Multiple features + infrastructure | Entity, DTOs, service, views | Partial view + HTMX |

---

## Decision Tree

### Should I create a Module, Feature, or Component?

```
Do you need multiple related entities + infrastructure?
├─ YES → Create a MODULE
│         Example: Identity (users, roles, permissions)
│         Command: netmx create module Identity
│
└─ NO → Do you need one entity with CRUD?
         ├─ YES → Generate a FEATURE
         │         Example: Product (entity + CRUD)
         │         Command: netmx generate feature Product
         │
         └─ NO → Do you need a reusable UI pattern?
                  └─ YES → Create a COMPONENT
                            Example: ContactCard (display + edit)
                            Command: netmx generate component ContactCard
```

---

## Common Scenarios

### Scenario 1: Building E-Commerce App

**Goal**: Product catalog with shopping cart

**Approach**:
```bash
# 1. Create project
netmx new modular MyShop

# 2. Add standard modules
netmx add module Identity    # User authentication
netmx add module Audit       # Audit logging

# 3. Generate app-specific features
netmx generate feature Product
netmx generate feature Category
netmx generate feature Order
netmx generate feature Customer
```

**Result**: 
- Modules: Reusable infrastructure (Identity, Audit)
- Features: App-specific entities (Product, Order)

---

### Scenario 2: Building Reusable Audit Module

**Goal**: Audit logging module for multiple projects

**Approach**:
```bash
# 1. Create module
netmx create module Audit

# 2. Generate features in module
cd modules/Audit/Audit.Web
netmx generate feature AuditLog -m Audit
netmx generate feature AuditEntry -m Audit

# 3. Add custom business logic
# Edit generated files to add:
# - Automatic capture logic
# - Filtering/querying
# - Retention policies

# 4. Package and distribute
dotnet pack modules/Audit/Audit.Web
```

**Result**: Reusable NuGet package with multiple features

---

### Scenario 3: Building HTMX Component Library

**Goal**: Reusable HTMX UI components

**Approach** (Future):
```bash
# 1. Create component library project
netmx create components MyComponents

# 2. Generate components
netmx generate component ContactCard
netmx generate component FileUpload
netmx generate component DataTable

# 3. Package and distribute
dotnet pack MyComponents
```

**Result**: Razor class library with HTMX components

---

## Best Practices

### Modules
- ✅ Keep focused (single responsibility)
- ✅ Document in module.json
- ✅ Version carefully (SemVer)
- ✅ Minimize external dependencies
- ✅ Provide migration scripts
- ✅ Include usage examples

### Features
- ✅ Use CLI to generate (don't create manually)
- ✅ Follow generated structure
- ✅ Add business logic after generation
- ✅ Keep entity names singular (Product, not Products)
- ✅ Use descriptive property names
- ✅ Leverage HTMX patterns in generated views

### Components
- ✅ Make self-contained (include CSS if needed)
- ✅ Document required HTMX attributes
- ✅ Provide usage examples
- ✅ Test in multiple scenarios
- ✅ Consider accessibility (ARIA)
- ✅ Support both GET and POST patterns

---

## Anti-Patterns

### ❌ Don't Create Modules Manually
```bash
# WRONG
mkdir modules/Audit
# ... manually create 4 projects, references, etc.

# RIGHT
netmx create module Audit
```

### ❌ Don't Create Features Manually
```bash
# WRONG
# Manually create entity, DTOs, service, controller, views

# RIGHT
netmx generate feature AuditLog -m Audit
```

### ❌ Don't Mix Concerns
```bash
# WRONG: App-specific feature in reusable module
netmx generate feature MySpecificBusinessEntity -m Audit

# RIGHT: App-specific features stay in app
netmx generate feature MySpecificBusinessEntity
```

### ❌ Don't Create Mega-Modules
```bash
# WRONG: One module with unrelated features
modules/Everything/
├── Users/
├── Products/
├── Orders/
└── CMS/

# RIGHT: Focused modules
modules/Identity/    # Just user management
modules/Catalog/     # Just products
modules/Orders/      # Just orders
modules/CMS/         # Just content
```

---

## Glossary

| Term | Definition |
|------|------------|
| **Module** | Reusable package with multiple features |
| **Feature** | Single entity with CRUD operations |
| **Component** | Reusable HTMX UI pattern |
| **Entity** | Domain model (Product, User, AuditLog) |
| **DTO** | Data Transfer Object (for API/service layer) |
| **Aggregate Root** | Entity that controls a cluster of related entities |
| **Repository** | Data access abstraction |
| **Service** | Business logic implementation |
| **Controller** | HTTP request handler |
| **View** | HTML template (Razor) |
| **Partial View** | Reusable view fragment |
| **View Component** | Reusable view with logic |
| **HTMX** | Library for server-side HTML with AJAX |
| **Bulma** | CSS framework used in NetMX |
| **DDD** | Domain-Driven Design |

---

## FAQ

**Q: When should I create a new module vs add to existing module?**  
A: Create new module if features are unrelated or serve different projects. Add to existing if tightly coupled.

**Q: Can I have modules depend on other modules?**  
A: Yes, but minimize dependencies. Document in module.json.

**Q: Should every feature be in a module?**  
A: No. App-specific features stay in app. Only create modules for reusable features.

**Q: Can I customize generated features?**  
A: Yes! Generated code is a starting point. Add business logic, validations, custom views.

**Q: What if I need non-CRUD operations?**  
A: Generate the feature for basic structure, then add custom actions to controller.

**Q: Should components be in separate projects?**  
A: Future: Yes, in Razor class libraries. Current: Create as partial views in Web project.

---

## Additional Resources

- [CLI Implementation Guide](CLI-IMPLEMENTATION.md)
- [HTMX Patterns Guide](HTMX-PATTERNS.md)
- [Quick Start Guide](QUICK-START.md)
- [Contributing Guide](../CONTRIBUTING.md)

---

**Remember**: Use the CLI, don't create manually. Focus on business logic, not boilerplate!

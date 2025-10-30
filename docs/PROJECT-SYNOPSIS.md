# Swap Framework - Project Synopsis

## What is Swap?

Swap is a lightweight, modern .NET 9 web framework designed for building server-rendered web applications with HTMX. It provides a **container-based architecture** that enables partial page updates without full page reloads, offering a simpler alternative to heavy JavaScript SPA frameworks.

## Core Philosophy

- **Server-rendered first** - HTML generated on the server, minimal JavaScript
- **HTMX-powered** - Progressive enhancement via HTMX attributes
- **Container architecture** - Shell → Page → Component hierarchy for surgical DOM updates
- **Pattern-driven** - Common behaviors (soft delete, slugs, audit trails, etc.) applied via CLI
- **Convention over configuration** - Sensible defaults, minimal boilerplate

## Current State (October 2025)

### What's Working ✅

1. **Framework Packages**
   - `Swap.Htmx` (0.0.1) - Core HTMX middleware, shell management, SwapController base
   - `Swap.Patterns` (0.0.1) - 8 reusable patterns with interfaces and helpers
   - `Swap.Testing` - Test utilities for HTMX apps (HtmxTestClient, snapshot testing)

2. **CLI Tool** (`swap`)
   - `swap new <name>` - Create new projects (SQLite/SQL Server/PostgreSQL)
   - `swap generate model <name>` - Scaffold entities with custom fields
   - `swap generate controller <name>` - Full CRUD controllers with HTMX views
   - `swap generate pattern <type> <entity>` - Apply patterns (auditable, publishable, etc.)
   - `swap generate seed <entity>` - Faker-based data seeders
   - `swap generate factory <entity>` - Test data factories

3. **Test Coverage**
   - ✅ 35/35 Swap.Htmx tests passing
   - ✅ 72/72 Swap.Patterns tests passing
   - ✅ Container architecture manually verified in FreshAppTest
   - ✅ Pattern generation tested (found + fixed template issue)

4. **Template System**
   - Monolith template with 3-layer architecture
   - Includes Swap.Htmx + Swap.Patterns by default (recently fixed)
   - Pre-configured TailwindCSS, Libman, Entity Framework

### Known Issues 🐛

1. **Pattern CLI has vestigial `--use-package` flag**
   - Used to support "embed code" vs "use NuGet" modes
   - Now obsolete since Swap.Patterns is always in template
   - Creates confusion with `usePackage`/`fallback` parameters

2. **Pattern application can fail mid-stream**
   - If build fails during migration creation, whole pattern fails
   - Should apply pattern to model even if migration fails

3. **No pattern compatibility validation**
   - IAuditable (nullable UpdatedAt) vs ITimestampable (non-nullable UpdatedAt) conflict
   - Should detect/warn about incompatible pattern combinations

4. **Manual configuration required for some patterns**
   - Auditable: Requires HttpContextAccessor setup in Program.cs
   - SoftDelete: Requires ConfigureSoftDeleteFilter() in DbContext
   - Should auto-configure or provide better guidance

## Architecture

### 3-Layer Container System

```
┌─────────────────────────────────┐
│  Shell (_Layout.cshtml)         │ ← Never reloads
│  - Persistent nav, header, etc  │
│                                  │
│  ┌─────────────────────────────┐│
│  │ Page Container              ││ ← Swaps on navigation
│  │ (Index, Details, etc.)      ││
│  │                              ││
│  │  ┌─────────────────────────┐││
│  │  │ Component               │││ ← Updates via events
│  │  │ (Partials, forms)       │││
│  │  └─────────────────────────┘││
│  └─────────────────────────────┘│
└─────────────────────────────────┘
```

### Key Files Structure

```
YourApp/
├── Controllers/         # MVC controllers inherit from SwapController
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml    # Shell with hx-swap-oob
│   └── Entity/
│       ├── Index.cshtml      # Page containers
│       ├── _Form.cshtml      # Components
│       └── _Row.cshtml
├── Models/              # Entities with pattern interfaces
├── Data/
│   ├── AppDbContext.cs
│   └── Seeders/
└── wwwroot/
```

## Available Patterns (Swap.Patterns)

1. **Auditable** - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy tracking
2. **SoftDelete** - IsDeleted, DeletedAt instead of hard deletes
3. **Sluggable** - URL-friendly slugs with uniqueness
4. **Timestampable** - CreatedAt, UpdatedAt (simpler than Auditable)
5. **Orderable** - Position field for manual ordering
6. **Publishable** - IsPublished, PublishedAt for draft/publish workflow
7. **Versionable** - Version field for optimistic concurrency
8. **Visibility** - IsVisible, VisibleFrom, VisibleTo for scheduling

## Where We're Headed

### Immediate Goals (Pre-Release)

1. **Fix CLI Pattern Command**
   - Remove --use-package/--fallback options
   - Always use NuGet package mode
   - Make migration creation non-blocking

2. **Comprehensive Testing**
   - Create full-featured test app using ALL CLI commands
   - Document every issue encountered
   - Validate Docker builds
   - Test PostgreSQL provider

3. **Documentation**
   - Getting started guide
   - Pattern library reference
   - Architecture deep-dive
   - Migration from other frameworks

### Future Direction

- Open source release on GitHub
- NuGet package publishing
- Community feedback integration
- Additional patterns as needed
- Performance optimization
- More database providers

## Documentation References

See `/docs` folder for detailed documentation:

- **CONTAINER-ARCHITECTURE.md** - Deep dive into 3-layer system
- **DEVELOPER-EXPERIENCE.md** - CLI usage, workflow, conventions
- **THE-PRODUCT.md** - Vision, philosophy, target audience
- **archive/PATTERNS-LIBRARY.md** - Detailed pattern documentation

## Current Blockers

**Right now:** Working on refactoring the Pattern CLI command to remove legacy `--use-package` complexity. The regex-based approach broke the file structure, so we've reverted and need a more careful approach.

**Next:** Once CLI is fixed, create a comprehensive test application (BlogSite) that exercises all CLI features to identify remaining issues before OSS release.

---

**Status as of Oct 29, 2025:** Framework is solid and tested. CLI needs minor cleanup. Ready for comprehensive integration testing before v0.1.0 release.

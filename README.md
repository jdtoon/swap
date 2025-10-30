# Swap CLI

[![GitHub License](https://img.shields.io/github/license/jdtoon/swap)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/badge/NuGet-v0.0.14--prerelease-blue?logo=nuget)](https://www.nuget.org/packages/Swap.CLI)
[![GitHub Stars](https://img.shields.io/github/stars/jdtoon/swap?style=social)](https://github.com/jdtoon/swap/stargazers)

**Generate production-ready ASP.NET Core + HTMX applications with beautiful DaisyUI components.**

Swap CLI is a code generator that creates complete, modern web applications using ASP.NET Core MVC, HTMX for interactivity, DaisyUI for UI components, and Entity Framework Core for data access. Generate full CRUD operations with pagination, search, sorting, filtering, and modal-based editing in seconds.

## 🌟 Why Swap?

- **⚡ Production-Ready Code** - Generate complete CRUD with modals, pagination, sorting, filtering, and search
- **🎯 HTMX Simplicity** - Modern, interactive web apps without JavaScript frameworks
- **🎨 DaisyUI + Tailwind** - Beautiful, accessible components out of the box
- **🗄️ Entity Framework Core** - Full database integration with migrations support
- **🐳 Docker Ready** - Every project includes Dockerfile and docker-compose.yml with multi-stage builds
- **💻 Developer Experience** - CLI-driven workflow, no manual boilerplate
- **📦 Proven Patterns** - Every pattern extracted from real production applications

## 🚀 Quick Start

### Prerequisites

Before installing Swap CLI, ensure you have:

- **.NET 9.0 SDK** or later - [Download](https://dotnet.microsoft.com/download)
- **Node.js (LTS)** - Includes npm for Tailwind CSS compilation
  - Windows: `winget install OpenJS.NodeJS.LTS` or download from [nodejs.org](https://nodejs.org/)
  - macOS: `brew install node`
  - Linux: Use your package manager
- **libman CLI** - Manages client libraries (HTMX, DaisyUI)
  ```bash
  dotnet tool install -g Microsoft.Web.LibraryManager.Cli
  ```

Verify installations:
```bash
dotnet --version   # Should be 9.0 or higher
npm --version      # Any recent version
libman --version   # Any version
```

### Installation

```bash
# Install the Swap CLI tool
dotnet tool install --global Swap.CLI --prerelease

# Verify installation
swap --version
```

### Create Your First Project

```bash
# Create a new ASP.NET Core + HTMX application
swap new MyApp
cd MyApp

# Apply migrations and run
dotnet ef database update
dotnet run
```

Visit `http://localhost:5000` - Your HTMX-powered application is running! 🎉

**Note:** The CLI automatically runs `npm install`, `libman restore`, and `npm run build:css` during project creation.

### Generate Your First CRUD

```bash
# Generate a complete CRUD controller with all features
swap generate controller Product --fields "Name:string Price:decimal InStock:bool:f"

# Short alias with automatic navigation link
swap g c Product --fields "Name:string Price:decimal InStock:bool:f" --add-nav
```

Visit `http://localhost:5000/Product` - Full CRUD with pagination, search, sorting, and filtering! 🚀

**No manual file creation. No boilerplate. Just CLI commands and business logic.**

> **Note:** Swap automatically creates migrations after generating models or controllers. The CLI builds your project first to verify there are no compilation errors before creating the migration. If the build fails, you'll see the error output and the migration won't be created. This prevents confusing EF Core errors and helps catch issues early.

## 🔒 Build-Before-Migration Safety

Swap CLI uses a **build-first approach** for all migration operations to ensure code quality and prevent cryptic Entity Framework errors.

### How It Works

When you generate:
- A new project (`swap new`)
- A controller (`swap g controller`)
- A pattern (`swap g pattern`)
- Auth scaffolding (`swap g auth`)

The CLI will:
1. Generate your model and controller code
2. **Build the project** (`dotnet build`)
3. If build succeeds → Create migration (e.g., `AddProduct`, `AddIdentity`)
4. If build fails → Show compiler errors, stop (no migration created)

### Why This Matters

Without building first, EF Core migrations can fail with cryptic errors like:
- "The entity type X cannot be mapped to a table because it is derived from Y"
- "No suitable constructor found for entity type X"
- "Unable to determine the relationship represented by navigation X"

These errors often mask simple compiler issues like:
- Nullable reference warnings (`DateTime?` without null-conditional operators)
- Missing using statements
- Typos in property names

By building first, you see clear C# compiler messages instead of confusing EF Core errors.

### Migration Workflow

Swap **never** applies migrations automatically (`dotnet ef database update`). You always control when schema changes hit your database:

```bash
# After generating a controller, Swap creates the migration
swap g c Product --fields "Name:string Price:decimal"
# Output: ✓ Migration created: AddProduct

# Review the migration file
cat Migrations/20250129_AddProduct.cs

# Apply when ready
dotnet ef database update
```

This gives you the opportunity to:
- Review migration code before applying
- Modify migrations if needed (add indexes, seed data, etc.)
- Run migrations in your CI/CD pipeline
- Maintain strict control over schema changes

## 🐳 Docker Support

Every generated project is Docker-ready with production-optimized configurations.

### Quick Start with Docker

```bash
# Create a new project
swap new MyApp --database postgres

# Build and run with Docker Compose
cd MyApp
docker-compose up --build

# Visit http://localhost:5000
```

### What's Included

Each project generates:

**Dockerfile:**
- Multi-stage build (Build stage: .NET SDK 9.0 + Node.js 20.x, Runtime stage: ASP.NET 9.0)
- Automatic `libman restore` for HTMX/DaisyUI libraries
- Tailwind CSS compilation with `npm run build:css`
- Optimized layer caching for faster rebuilds
- HTTP-only configuration for Development environment
- Production-ready runtime image

**docker-compose.yml:**
- App service with environment-specific configuration
- Database service (SQL Server, PostgreSQL, or SQLite with volumes)
- Health checks ensuring database readiness before app starts
- Persistent volumes for data and data protection keys
- Network isolation for security
- Port mappings (app: 5000, database: default port)

**Features:**
- ✅ Auto-apply migrations on container startup
- ✅ Database health checks (SQL Server: 30s, PostgreSQL: 10s)
- ✅ Data protection keys persist across container restarts
- ✅ SQLite with persistent volume mount
- ✅ Environment variable configuration
- ✅ Production-ready Dockerfile optimizations

### Database-Specific Configurations

**SQL Server:**
```bash
swap new MyApp --database sqlserver
cd MyApp
docker-compose up --build
# App: http://localhost:5000
# SQL Server: localhost:1433
```

**PostgreSQL:**
```bash
swap new MyApp --database postgres
cd MyApp
docker-compose up --build
# App: http://localhost:5000
# PostgreSQL: localhost:5432
```

**SQLite:**
```bash
swap new MyApp --database sqlite
cd MyApp
docker-compose up --build
# App: http://localhost:5000
# Database: Persistent volume at /app/data
```

All database credentials are pre-configured in docker-compose.yml (change for production!).

## 🎯 What You Get

### Complete Feature Set

Every generated controller includes:

- ✅ **CRUD Operations** - Create, Read, Update, Delete via HTMX modals
- ✅ **Pagination** - Configurable page sizes (10, 25, 50, 100)
- ✅ **Real-Time Search** - 500ms debounced search across fields
- ✅ **Column Sorting** - Ascending/descending toggle per field
- ✅ **Boolean Filtering** - Dropdown filters (All/Yes/No) for bool fields
- ✅ **Bulk Operations** - Select multiple items and bulk delete
- ✅ **Toast Notifications** - Success/error messages with DaisyUI alerts
- ✅ **Modal Editing** - No page reloads, smooth UX
- ✅ **Validation** - Client and server-side with clear error messages
- ✅ **Responsive Design** - Works perfectly on mobile and desktop

### Generated Stack

- **Backend:** ASP.NET Core 9.0 MVC
- **Frontend:** HTMX + DaisyUI + Tailwind CSS
- **Database:** Entity Framework Core (SQLite, SQL Server, PostgreSQL)
- **UI Library:** DaisyUI 4.x components
- **Styling:** Tailwind CSS 3.x utilities
- **Containerization:** Docker with multi-stage builds and health checks

## 📋 CLI Commands

### `swap new <name>`

Create a new ASP.NET Core + HTMX application with DaisyUI components.

```bash
# Create with SQLite (default) - includes HTMX shell middleware
swap new MyApp

# Create without HTMX shell middleware (opt-out)
swap new MyApp --no-htmx-shell

# Create with SQL Server
swap new MyApp --database sqlserver

# Create with PostgreSQL
swap new MyApp --database postgres
```

**Options:**
- `--database` or `-d` - Database provider: `sqlite` (default), `sqlserver`, `postgres`
- `--no-htmx-shell` - Disable HTMX shell middleware (advanced use cases only)

**Generates:**
- Complete ASP.NET Core MVC project structure
- Entity Framework Core with your chosen database
- DaisyUI + Tailwind CSS configuration
- Sample TodoItem model and CRUD
- Database migrations with auto-apply on startup
- **HTMX Shell Middleware** (default) - Enforces partial view responses for HTMX requests
- **HTMX-First Layout** - `hx-boost="true"` on body, `id="main-content"` on main element
- **DaisyUI Navbar** - Aligned with `navbar-start`/`navbar-end` components
- **Dockerfile** with multi-stage build (Node.js + .NET SDK → ASP.NET runtime)
- **docker-compose.yml** with database service and health checks
- **.dockerignore** optimized for ASP.NET Core
- Ready to run with `dotnet run` or `docker-compose up`

**HTMX Shell Middleware:**

By default, new projects include HTMX shell middleware that enforces partial view responses for HTMX requests. This prevents full page reloads when navigating with HTMX and ensures your app behaves like a SPA.

The middleware checks for `HX-Request` headers and verifies responses don't include the `<html>` tag. If detected, it throws an exception with the problematic view name, helping you catch layout rendering bugs during development.

Configure the allowlist in `Middleware/HtmxShellMiddleware.cs`:
```csharp
private static readonly HashSet<string> AllowFullPagePaths = new(StringComparer.OrdinalIgnoreCase)
{
    "/",           // Home page
    "/auth/login", // Login page
    "/auth/register"
};
```

To disable the middleware, use `--no-htmx-shell` when creating the project.

**HTMX Navigation:**

All new projects are configured for HTMX-first navigation:
- `hx-boost="true"` on the `<body>` element enables automatic AJAX navigation
- Navigation links use `hx-target="#main-content"` to swap only the content area
- `hx-push-url="true"` maintains browser history and back/forward navigation
- Partials are returned for HTMX requests (via `HX-Request` header detection)
- Controllers check `Request.Headers.ContainsKey("HX-Request")` to return partial vs full views

**Docker Features:**
- Multi-stage build optimized for production
- libman restore for HTMX/DaisyUI dependencies
- Automatic migration on container startup
- Health checks for database readiness
- Persistent volumes for data storage
- Data protection keys configured for containers

### `swap generate controller <name> --fields <fields>`

Generate a complete CRUD controller with all features.

```bash
# Generate Product controller with fields
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# With nullable fields (use ? suffix for DateTime, decimal, etc.)
swap g c Customer --fields "Name:string Email:string Notes:string?"
swap g c Article --fields "Title:string Content:string PublishedAt:DateTime?"

# Add navigation link automatically (includes HTMX attributes)
swap g c Product --fields "Name:string Price:decimal" --add-nav

# Control sorting and filtering per field (space or comma separated)
swap g c Order --fields "OrderNumber:string:ns Total:decimal Date:DateTime Status:bool:f"
swap g c Order --fields OrderNumber:string:ns,Total:decimal,Date:DateTime,Status:bool:f

# Preview without writing files (dry-run)
swap g c Product --fields "Name:string Price:decimal" --dry-run

# Overwrite existing files without prompting
swap g c Product --fields "Name:string Price:decimal" --force

# Generate in a different project directory
swap g c Product --fields "Name:string" --project path/to/project
```

**Options:**
- `--fields` or `-f` - Field definitions (space or comma separated)
- `--add-nav` - Automatically inject navigation link into `_Layout.cshtml` with HTMX attributes
- `--dry-run` - Preview what would be generated without writing files
- `--force` - Overwrite existing files without prompting
- `--project` or `-p` - Path to project directory (default: current directory)

**Field Flags:**
- `:sortable` or `:s` - Enable sorting (default for all fields)
- `:nosort` or `:ns` - Disable sorting
- `:filterable` or `:f` - Enable filtering (bool fields only)

**Generates:**
- Controller with full CRUD operations
- Model class with validation
- View model for list operations
- Views (Index, _List, _CreateModal, _EditModal, _DetailsModal)
- Automatic DbContext updates
- Database migration (`Add<Entity>` - auto-created after build verification)
- Navigation link (when using `--add-nav`)

### `swap generate model <name> --fields <fields>`

Generate just a model class (no controller or views).

```bash
swap g m Category --fields "Name:string Description:string?"
swap g m Category --fields Name:string,Description:string?

# Preview the generated model
swap g m Product --fields "Name:string Price:decimal" --dry-run

# Overwrite without prompting
swap g m Category --fields "Name:string" --force

# Generate in a different project
swap g m Category --fields "Name:string" --project path/to/project
```

**Options:**
- `--fields` or `-f` - Field definitions (space or comma separated)
- `--dry-run` - Preview what would be generated without writing files
- `--force` - Overwrite existing files without prompting
- `--project` or `-p` - Path to project directory (default: current directory)

### `swap generate resource <name> --fields <fields>`

Generate model + controller together (alias for backward compatibility).

### `swap generate auth`

Generate ASP.NET Core Identity authentication scaffolding with login, registration, and user management.

```bash
# Generate complete auth system
swap g auth
```

**Generates:**
- `Models/AppUser.cs` - Extended IdentityUser with DisplayName, CreatedAt, LastLoginAt
- `Controllers/AuthController.cs` - Login, Register, Logout actions with HTMX support
- `Views/Auth/` - Login.cshtml, Register.cshtml with DaisyUI forms
- `Views/Shared/_LoginPartial.cshtml` - Navigation bar auth state
- Identity configuration in `Program.cs`
- Database migration (`AddIdentity` - auto-created after build verification)

**Features:**
- Cookie-based authentication
- DisplayName field for personalized UX
- LastLoginAt tracking
- HTMX-compatible forms (partial responses for validation errors)
- DaisyUI styled forms with proper validation messages
- Auto-wired into `_Layout.cshtml` navigation

> **Note:** After generating auth, apply the migration: `dotnet ef database update`

### `swap generate pattern <pattern> <entity>`

Apply battle-tested entity patterns to your models. Patterns can be embedded (default) or use the Swap.Patterns NuGet package.

```bash
# Apply sluggable pattern (embedded code)
swap g pattern sluggable BlogPost

# Apply auditable pattern using Swap.Patterns package
swap g pattern auditable Article --use-package

# Available patterns
swap g pattern softdelete <entity>      # Soft delete with IsDeleted flag
swap g pattern auditable <entity>       # Audit trail (CreatedAt, CreatedBy, etc.)
swap g pattern sluggable <entity>       # URL-friendly slugs
swap g pattern timestampable <entity>   # CreatedAt, UpdatedAt timestamps
swap g pattern publishable <entity>     # Publishing workflow (draft/published)
swap g pattern orderable <entity>       # Display order sorting
swap g pattern versionable <entity>     # Version tracking
swap g pattern visibility <entity>      # Public/private visibility
```

**Options:**
- `--use-package` - Use `Swap.Patterns` NuGet package (interface-based) instead of embedding code
- `--force` - Overwrite existing files without prompting
- `--project` or `-p` - Path to project directory

**Generates:**
- Pattern properties added to entity model
- DbContext configuration (indexes, filters, etc.)
- Database migration (e.g., `AddArticleSlug` - auto-created after build verification)
- Controller updates (for slug generation, soft delete queries, etc.)

**Embedded vs Package Mode:**

- **Embedded (default):** Copies pattern code directly into your model (no external dependency)
- **Package mode (`--use-package`):** Implements interfaces from `Swap.Patterns` NuGet package (v0.0.1)

For full pattern documentation, see [`tools/Swap.CLI/README.md`](tools/Swap.CLI/README.md#-swappatterns-common-entity-patterns).

### `swap generate test <controller>`

Generate an integration test class scaffold for a controller using Swap.Testing.

```bash
# Generate tests for TodoItemController
swap g test TodoItem

# Overwrite without prompt
swap g test TodoItem --force

# Generate into a specific project/output folder
swap g test TodoItem --project testApps/SeedersDemo --output Tests
```

**Options:**
- `--force, -f` Overwrite existing file
- `--project, -p` Path to project (default: current dir)
- `--output, -o` Output folder (default: `Tests/`)

Generates: `<Output>/<ControllerName>Tests.cs` with HTMX partial tests and a snapshot example.

### `swap generate factory <entity>`

Generate a Bogus-powered test data factory for an entity model.

```bash
# Generate a factory from Models/TodoItem.cs
swap g factory TodoItem

# Force overwrite
swap g factory TodoItem --force
```

Generates: `Tests/Factories/<Entity>Factory.cs` with intelligent defaults inferred from property names and types.

> Note: If Bogus/Swap.Testing packages are missing, the CLI will print the commands to add them.

## 🧪 Swap.Testing (HTMX Testing Framework)

Swap.Testing is a fluent testing library for asserting HTMX partials and headers.

**NuGet Package:** `Swap.Testing` (v0.0.1)

```bash
dotnet add package Swap.Testing --prerelease
```

Highlights:
- HTMX-aware client: `HtmxTestClient` with `HtmxGetAsync/PostAsync/PutAsync/DeleteAsync`
- Fluent assertions: status, elements, attributes, HTMX headers, CSS classes, partial view detection
- Snapshot testing via `AssertMatchesSnapshotAsync` and `UPDATE_SNAPSHOTS=true`

See `framework/Swap.Testing/README.md` for full API and examples.

## 📦 Swap.Patterns (Entity Patterns Library)

Swap.Patterns provides battle-tested interfaces and implementations for common entity patterns with **automatic wiring** and configuration tracking.

**NuGet Package:** `Swap.Patterns` (v0.0.1)

```bash
dotnet add package Swap.Patterns --prerelease
```

**Available Patterns:**
- `ISoftDeletable` - Soft delete with `IsDeleted` flag and `DeletedAt` timestamp
  - **Auto-wires:** Global query filter in DbContext
- `IAuditable` - Full audit trail (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`)
  - **Auto-wires:** IHttpContextAccessor + audit interceptor in DbContext
- `ITimestampable` - Created and updated timestamps
  - **Auto-wires:** Timestamp interceptor in DbContext
- `ISluggable` - SEO-friendly slugs with collision handling
  - **Auto-wires:** Unique index on Slug column
- `IPublishable` - Publishing workflow (`PublishedAt`, `IsPublished`)
- `IOrderable` - Display order with `DisplayOrder` property
- `IVersionable` - Version tracking with `Version` property
- `IVisibility` - Public/private visibility with `IsPublic` flag

**Automatic Wiring:**

When you apply a pattern, Swap CLI automatically:
1. Adds pattern interface and properties to your model
2. Wires up required infrastructure (filters, interceptors, services)
3. Tracks configuration in `swap-config.json`
4. Creates database migration

```bash
# Apply soft delete - automatically adds global query filter
swap g pattern softdelete Post

# Apply auditable - automatically wires IHttpContextAccessor and interceptor
swap g pattern auditable Product
```

**Pattern Removal:**

Remove patterns safely with automatic cleanup:

```bash
# Remove a pattern from an entity
swap g pattern remove Post softdelete

# Smart cleanup: only removes shared wiring when safe
# - Checks swap-config.json to see if other entities use the pattern
# - Removes global filters/interceptors only when no entities need them
# - Database columns are preserved by default (manual migration for drop)
```

**Configuration Tracking:**

Swap maintains a `swap-config.json` file in your project root that tracks:
- Which patterns are applied to which entities
- Shared wiring state (filters, interceptors, services)
- Pattern configuration history

This enables safe removal and prevents duplicate wiring.

**Usage:**

Use `swap g pattern <pattern> <entity> --use-package` to apply patterns with the NuGet package:

```bash
# Install the package
dotnet add package Swap.Patterns --prerelease

# Apply pattern using interface
swap g pattern auditable Article --use-package
```

Or use the default embedded mode (no package dependency):

```bash
# Apply pattern with embedded code
swap g pattern auditable Article
```

See `framework/Swap.Patterns/README.md` for full API, interceptors, and extension methods.


```bash
swap g r BlogPost --fields "Title:string Content:string PublishedDate:DateTime"
swap g r BlogPost --fields Title:string,Content:string,PublishedDate:DateTime

# With generator ergonomics options
swap g r Order --fields "Total:decimal Status:string" --dry-run
swap g r Order --fields "Total:decimal Status:string" --force --project path/to/project
```

**Options:**
- `--fields` or `-f` - Field definitions (space or comma separated)
- `--dry-run` - Preview what would be generated without writing files
- `--force` - Overwrite existing files without prompting
- `--project` or `-p` - Path to project directory (default: current directory)

### `swap generate seed <name>`

Generate database seeders with realistic fake data using Bogus.

```bash
# Generate a seeder for a single entity
swap g seed Product --count 100 --locale en --if-empty

# Generate seeders for all entities in your DbContext
swap g seed all --count 50 --locale en --if-empty

# Short alias
swap g s all --count 50 --locale en --if-empty

# Overwrite without prompting
swap g s Product --force

# Generate in a different project
swap g s all --project path/to/project
```

**Options:**
- `--count` (default: 50) - Number of records to generate
- `--locale` (default: "en") - Bogus locale (en, en_GB, de, fr, etc.)
- `--if-empty` - Only seed when the table is empty (idempotent)
- `--force` - Overwrite existing seeder files without prompting
- `--project` or `-p` - Path to project directory (default: current directory)

**What it generates:**
- `Data/Seeders/<Entity>Seeder.cs` with smart Bogus rules based on field names
- `Data/Seeders/SeedRunner.cs` orchestrator (auto-registered)
- Adds `Bogus` package reference if missing
- Hooks into `Program.cs` for Development environment seeding

**Field intelligence:**
- Strings: emails, URLs, names, titles, descriptions, phone numbers, addresses
- Numbers: realistic ranges based on field names (age, price, quantity)
- Booleans: weighted probabilities (e.g., IsActive ~70% true)
- Dates: distributed over the last 3 years
- Foreign keys: picks from existing related entities

**Environment control:**
```bash
# Control seeding via environment variables
$env:SEED_COUNT = "200"
$env:SEED_LOCALE = "en_GB"
$env:SEED_IFEMPTY = "true"
dotnet run
```

### `swap database` / `swap db`

Database workflow commands for easier development.

#### `swap db info`

Display database configuration and migration status.

```bash
swap db info
```

Shows:
- Project name
- Database provider (SQLite, SQL Server, PostgreSQL)
- Connection string (masked)
- Number of migrations
- Last applied migration
- Seeder status

#### `swap db migrate [name] [--apply]`

Create and/or apply Entity Framework Core migrations.

```bash
# Create a new migration
swap db migrate AddProductTable

# Create and apply immediately
swap db migrate AddProductTable --apply

# Apply pending migrations (no name argument)
swap db migrate --apply
```

**Options:**
- `name` - Optional migration name (if omitted with --apply, applies pending migrations)
- `--apply` or `-a` - Apply the migration after creating it

#### `swap db reset [--force]`

Drop and recreate the database for a fresh start.

```bash
# With confirmation prompt
swap db reset

# Skip confirmation
swap db reset --force
```

**Options:**
- `--force` or `-f` - Skip confirmation prompt

**Warning:** This command drops all data!

#### `swap db seed [--count] [--locale] [--if-empty]`

Run database seeders via application startup.

```bash
# Run seeders with default settings
swap db seed

# Customize seeding
swap db seed --count 100 --locale en_GB --if-empty
```

**Options:**
- `--count` or `-c` - Number of records per seeder (default: 50)
- `--locale` or `-l` - Bogus locale (default: "en")
- `--if-empty` - Only seed empty tables

**Note:** This runs `dotnet run` with environment variables set.

### `swap doctor`

Check your development environment and dependencies.

```bash
swap doctor
```

Checks:
- ✅ .NET SDK installation and version
- ✅ dotnet-ef tool installation
- ⚠️ Node.js (optional, for Tailwind CSS)
- ⚠️ npm (optional, comes with Node.js)
- ⚠️ libman (optional, for client libraries)

Displays a table with status, versions, and installation instructions for missing tools.

### `swap list [--project]`

List all resources (entities) in your project with their completeness status.

```bash
# List resources in current project
swap list

# List resources in another project
swap list --project path/to/project
```

Shows a table with:
- Entity names from DbContext
- ✓/✗ Model file exists
- ✓/✗ Controller file exists
- ✓/✗ Seeder file exists

**Options:**
- `--project` or `-p` - Path to project directory (default: current directory)

## 📚 Documentation

### 🎯 Core Documentation
- **[Container Architecture](docs/CONTAINER-ARCHITECTURE.md)** - ⭐ **START HERE** - The foundational pattern for Swap
- **[Developer Experience Guide](docs/DEVELOPER-EXPERIENCE.md)** - Best practices and workflow
- **[Pattern Library](docs/PATTERNS-LIBRARY.md)** - 30+ proven HTMX patterns from production apps
- **[Testing Checklist](docs/TESTING-CHECKLIST.md)** - Comprehensive validation guide

### 📖 Framework Documentation
- **[Swap.Htmx Framework](framework/Swap.Htmx/README.md)** - SwapController, SwapView(), extensions
- **[Swap.Patterns](framework/Swap.Patterns/README.md)** - Reusable model patterns
- **[Swap.Testing](framework/Swap.Testing/README.md)** - HTMX testing utilities

### 🌐 Online Resources
- **[Getting Started](https://jdtoon.github.io/swap/)** - Complete setup guide
- **[CLI Reference](https://jdtoon.github.io/swap/docs/cli/overview)** - All commands and options
- **[Features Guide](https://jdtoon.github.io/swap/docs/features/pagination)** - Pagination, search, sorting, filtering
- **[The Product Vision](docs/THE-PRODUCT.md)** - Philosophy and approach

## 🛠️ Development

### Prerequisites
- .NET 9.0 SDK or later
- Your favorite IDE (Visual Studio 2022, VS Code, Rider)

### Building the CLI from Source

```bash
# Clone the repository
git clone https://github.com/jdtoon/swap.git
cd swap

# Build the CLI tool
cd tools/Swap.CLI
dotnet build

# Run tests
cd ../Swap.CLI.Tests
dotnet test

# Install locally for testing
cd ../Swap.CLI
dotnet pack
dotnet tool install --global --add-source ./nupkg Swap.CLI
```

### Project Structure

```
swap/
├── tools/
│   ├── Swap.CLI/              # CLI tool source code
│   │   ├── Commands/          # Command implementations
│   │   ├── Infrastructure/    # Template engine, helpers
│   │   └── Program.cs         # CLI entry point
│   └── Swap.CLI.Tests/        # 145 passing tests
│       ├── Commands/          # Command tests
│       └── Infrastructure/    # Template engine tests
├── templates/                 # Code generation templates
│   ├── monolith/             # New project template
│   └── generate/             # CRUD generation templates
│       ├── controller/       # Controller, views, view model
│       └── model/            # Model class
├── docs/                     # Documentation
│   ├── THE-PRODUCT.md        # Product vision
│   └── PATTERNS-LIBRARY.md   # HTMX patterns
├── wiki/                     # Docusaurus documentation site
└── README.md                 # This file
```

## 🤝 Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or code contributions.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes and add tests
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'feat: Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📄 License

Swap CLI is [MIT licensed](LICENSE). Use it freely in your projects, commercial or otherwise.

## 📊 Project Status

**Current Version:** `0.0.14` (Active Development)

### ✅ Phase 2C Complete

- ✅ **New Project Generation** - `swap new` command with full ASP.NET Core setup
- ✅ **Controller Generation** - `swap g c` with all CRUD operations
- ✅ **Model Generation** - `swap g m` for entity classes
- ✅ **Pagination** - Configurable page sizes (10, 25, 50, 100)
- ✅ **Search** - Real-time search with 500ms debounce
- ✅ **Sorting** - Column sorting with field-level control
- ✅ **Filtering** - Boolean filters with dropdown UI
- ✅ **Modal Editing** - Create, Edit, Details modals via HTMX
- ✅ **Bulk Delete** - Select multiple items and delete
- ✅ **Toast Notifications** - DaisyUI alerts for success/error
- ✅ **DaisyUI Components** - Modern, accessible UI library
- ✅ **Tailwind CSS** - Utility-first styling
- ✅ **145 Passing Tests** - Comprehensive test coverage
- ✅ **Documentation** - Complete wiki with examples
- ✅ **Docker Support** - Multi-stage builds, health checks, all databases

### ✅ Phase 2D Complete: Database Seeders

- ✅ **Seeder Generation** - `swap g seed <entity>` and `swap g seed all`
- ✅ **Bogus Integration** - Realistic fake data with smart field heuristics
- ✅ **Environment Control** - SEED_COUNT, SEED_LOCALE, SEED_IFEMPTY
- ✅ **Foreign Key Support** - Automatic relationship handling
- ✅ **Development Startup** - Auto-seed on app launch in Development mode
- ✅ **Idempotent Seeding** - `--if-empty` flag for safe repeated runs

### ✅ Phase 2E Complete: Developer Experience

- ✅ **Generator Ergonomics** - `--dry-run`, `--force`, `--project` options on all generators
- ✅ **Database Commands** - `swap db info`, `db migrate`, `db seed`, `db reset`
- ✅ **DX Utilities** - `swap doctor` for environment checks, `swap list` for resource inventory
- ✅ **Field Delimiter Flexibility** - Support both space and comma-separated field definitions
- ✅ **Consistent Aliases** - `-f` for fields, `-p` for project, `-a` for apply, `-c` for count
- ✅ **Smart DbSet Scanning** - Handles both simple and fully-qualified entity types

### ✅ Phase 2F Complete: HTMX-First & Safety Features

- ✅ **Build-Before-Migration** - Rigid gates prevent cryptic EF errors, surface compiler issues early
- ✅ **Auto-Migration Creation** - All generators (auth, controller, pattern) create migrations automatically
- ✅ **HTMX-First Layout** - `hx-boost="true"`, `id="main-content"` target, `hx-push-url` navigation
- ✅ **HTMX Shell Middleware** - Default in new projects, enforces partial responses (opt-out with `--no-htmx-shell`)
- ✅ **DaisyUI Navbar Alignment** - `navbar-start`/`navbar-end` structure for consistent layouts
- ✅ **Auto-Nav Injection** - `--add-nav` flag injects navigation links with HTMX attributes
- ✅ **ASP.NET Identity Scaffolding** - `swap g auth` with auto-migration and HTMX support
- ✅ **Pattern Library** - 8 entity patterns (sluggable, auditable, soft delete, etc.) with `--use-package` option
- ✅ **NuGet Packages** - Swap.Patterns (v0.0.1), Swap.Testing (v0.0.1) ready for publishing
- ✅ **DateTime? Nullable Support** - Null-conditional operators in views for clean code

### 🎯 Phase 3: Polish & Release

- ⏳ **NuGet Publishing** - Push Swap.CLI (v0.0.14), Swap.Patterns, Swap.Testing to NuGet.org
- ⏳ **VS Code Extension** - Integrated CLI experience
- ⏳ **Video Tutorials** - Getting started screencasts
- ⏳ **Production Release** (v1.0.0) - Q1 2026

See the complete [roadmap](docs/ROADMAP.md) for details.

## 💬 Community

- **Documentation**: https://jdtoon.github.io/swap/
- **GitHub Issues**: https://github.com/jdtoon/swap/issues
- **GitHub Discussions**: Coming soon

For questions or feedback, open an [issue](https://github.com/jdtoon/swap/issues)!

## 🔗 Links

- **Documentation**: https://jdtoon.github.io/swap/
- **GitHub**: https://github.com/jdtoon/swap
- **Issues**: https://github.com/jdtoon/swap/issues
- **NuGet** (coming soon): https://www.nuget.org/packages/Swap.CLI

---

**Built with ❤️ for the .NET community**

*Swap CLI - Generate production-ready ASP.NET + HTMX applications in seconds.*



---
sidebar_position: 2
---

# swap new

Create a new ASP.NET Core project with HTMX and Docker support.

## Synopsis

```bash
swap new <name> [options]
```

## Options

- `<name>` - Project name (required)
- `--database <provider>` or `-d` - Database provider: `sqlite` (default), `sqlserver`, or `postgres`
- `--output <path>` or `-o` - Output directory (default: `./{name}`)
- `--skip-setup` - Skip prerequisites check, npm/libman steps, and initial migration (useful for CI/tests)
- `--local-nuget` - Use local NuGet feed for Swap packages (for framework development only)
 - `--template <name>` - Choose template; `swap-monolith` wires the Event System (chains, middleware, dev endpoints)

## Description

Generates a production-ready ASP.NET Core MVC project with:
- **Entity Framework Core** with your chosen database
- **HTMX** for interactive UI without JavaScript
- **HTMX-First Layout** - `hx-boost="true"` on body, `id="main-content"` on main element
- **DaisyUI + Tailwind CSS** for modern, accessible components
- **DaisyUI Navbar** - Aligned with `navbar-start`/`navbar-end` structure
- **Docker support** with Dockerfile and docker-compose.yml
- **Sample TodoItem CRUD** with modals, pagination, and search
- **Toast notifications** for user feedback
- **Auto-migrations** that run on startup
- **Production-ready patterns** from real applications

### HTMX Shell Middleware + Event System (swap-monolith template)

The HTMX shell middleware is available as an **optional** feature via the `Swap.Htmx` NuGet package or can be generated directly into your project with `swap generate htmx-shell`.

**What it does:**
- Detects `HX-Request` headers on incoming requests
- Verifies response HTML doesn't contain full `<html>` tags
- Throws exception with view name if full page detected (development aid)
- Helps catch layout rendering bugs early

**Already included:** In the `swap-monolith` template, the Event System is fully wired with `AddSwapHtmx(...)`, `UseSwapHtmxShell()`, and `UseSwapHtmx()`; dev endpoints at `/_swap/dev/events(.json)` map only in Development.

**Customization:** If you need to customize the middleware behavior, you can generate a local copy with `swap generate htmx-shell` and modify the allowlist.

### HTMX Navigation

All new projects are configured for SPA-like navigation with HTMX:
- `hx-boost="true"` on `<body>` enables automatic AJAX navigation
- Links use `hx-target="#main-content"` to swap only the content area
- `hx-push-url="true"` maintains browser history
- Controllers detect `HX-Request` header to return partials vs full views

**Example navigation link:**
```html
<a href="/Article" hx-target="#main-content" hx-push-url="true">Articles</a>
```

The main content container:
```html
<main id="main-content" class="container mx-auto p-4">
    @RenderBody()
</main>
```

## Examples

```bash
# Create with SQLite (default)
swap new MyApp
cd MyApp
dotnet run

# Create with SQL Server
swap new MyApp --database sqlserver
cd MyApp
docker-compose up --build

# Create with PostgreSQL
swap new MyApp --db postgres
cd MyApp
docker-compose up --build
```

Navigate to `http://localhost:5000` to see the Todo CRUD interface.

## Generated Structure

```
MyApp/
├── Controllers/
│   ├── HomeController.cs
│   └── TodoItemController.cs    # Sample CRUD with all features
├── Models/
│   ├── TodoItem.cs              # Sample entity
│   └── TodoItemListViewModel.cs # View model with pagination
├── Views/
│   ├── TodoItem/
│   │   ├── Index.cshtml         # Main view with search and filters
│   │   ├── _List.cshtml         # HTMX partial with table
│   │   ├── _AddModal.cshtml     # Create form in modal
│   │   └── _EditModal.cshtml    # Edit form in modal
│   ├── Home/
│   │   └── Index.cshtml         # Welcome page
│   └── Shared/
│       ├── _Layout.cshtml       # DaisyUI layout with navbar
│       └── _Pagination.cshtml   # Reusable pagination component
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── wwwroot/
│   ├── lib/
│   │   ├── htmx/                # HTMX library
│   │   └── toastify-js/         # Toast notifications
│   └── css/
│       ├── tailwind.css         # Generated Tailwind CSS
│       └── site.css             # Custom styles
├── tailwind.config.js           # Tailwind + DaisyUI configuration
├── appsettings.json             # Connection strings
├── Program.cs                   # App configuration
└── MyApp.csproj
```

## Sample Code

The generated TodoItem controller demonstrates all patterns:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1, 
    int pageSize = 10,
    string? searchTerm = null,
    string? sortBy = null,
    string? sortOrder = "asc")
{
    var query = _context.TodoItems.AsQueryable();
    
    // Search
    if (!string.IsNullOrWhiteSpace(searchTerm))
        query = query.Where(t => t.Title.Contains(searchTerm));
    
    // Sort
    query = sortBy switch {
        "Title" => sortOrder == "desc" ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
        "IsComplete" => sortOrder == "desc" ? query.OrderByDescending(t => t.IsComplete) : query.OrderBy(t => t.IsComplete),
        _ => query.OrderBy(t => t.Id)
    };
    
    // Pagination
    var totalItems = await query.CountAsync();
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    var viewModel = new TodoItemListViewModel {
        Items = items,
        Pagination = new PaginationDto { /* ... */ }
    };
    
    return Request.Headers.ContainsKey("HX-Request")
        ? PartialView("_List", viewModel)
        : View(viewModel);
}
```

The Index view uses HTMX for zero-reload interactions:

```html
<!-- Search bar -->
<input type="text" 
       class="input input-bordered"
       hx-get="@Url.Action("Index")" 
       hx-trigger="keyup changed delay:500ms"
       hx-target="#todo-list"
       placeholder="Search..." />

<!-- Todo list container -->
<div id="todo-list" 
     hx-get="@Url.Action("Index")" 
     hx-trigger="load"
     hx-include="[name='searchTerm']">
    <div class="flex justify-center p-8">
        <span class="loading loading-spinner loading-lg"></span>
    </div>
</div>
```

## Docker Support

Every project is **Docker-ready** out of the box! Generated Docker files include:

**Dockerfile:**
- Multi-stage build (Build: .NET SDK 9.0 + Node.js, Runtime: ASP.NET 9.0)
- Automatic `libman restore` for HTMX/DaisyUI
- Tailwind CSS compilation with `npm run build:css`
- Optimized layer caching for fast rebuilds
- Production-ready configuration

**docker-compose.yml:**
- App service with your chosen database
- Health checks ensuring database readiness
- Persistent volumes for data storage
- Auto-migrations on container startup
- Pre-configured ports (app: 5000, database: default)

**Run with Docker:**

```bash
# SQLite - Single container
swap new MyApp --database sqlite
cd MyApp
docker-compose up --build
# Visit http://localhost:5000

# SQL Server - App + SQL Server 2022 containers
swap new MyApp --database sqlserver
cd MyApp
docker-compose up --build
# Visit http://localhost:5000
# SQL Server: localhost:1433

# PostgreSQL - App + PostgreSQL 16 containers
swap new MyApp --database postgres
cd MyApp
docker-compose up --build
# Visit http://localhost:5000
# PostgreSQL: localhost:5432
```

**Key Features:**
- ✅ Database health checks (SQL Server: 30s, PostgreSQL: 10s)
- ✅ Migrations auto-apply on startup (no manual steps!)
- ✅ Data persists across container restarts
- ✅ HTMX/DaisyUI libraries included via libman
- ✅ Data protection keys configured for sessions/cookies

See the [Docker Deployment Guide](/docs/deployment/docker) for production configuration.

## Next Steps

After creating your project:

### Running Locally (without Docker)

```bash
# Create initial migration (already done by CLI)
# dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update

# Run the app
dotnet run
```

### Running with Docker (recommended)

```bash
# Start everything (builds, runs DB, runs app, applies migrations)
docker-compose up --build

# View logs
docker-compose logs -f app

# Stop
docker-compose down
```

**Note:** With Docker, migrations run automatically on startup - no manual steps needed!

### Generate More Resources

```bash
# Generate a new CRUD feature
swap g r Product --fields "Name:string Price:decimal InStock:bool:f"

# Without Docker: Create and apply migration
dotnet ef migrations add AddProduct
dotnet ef database update

# With Docker: Just rebuild
docker-compose up --build
```

## See Also

- [Your First Project](../getting-started/first-project) - Complete tutorial
- [swap generate resource](./generate-resource) - Generate models and controllers

# Swap CLI

[![GitHub License](https://img.shields.io/github/license/jdtoon/swap)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/badge/NuGet-coming_soon-blue?logo=nuget)](https://www.nuget.org/packages/Swap.CLI)
[![GitHub Stars](https://img.shields.io/github/stars/jdtoon/swap?style=social)](https://github.com/jdtoon/swap/stargazers)

**Generate production-ready ASP.NET Core + HTMX applications with beautiful DaisyUI components.**

Swap CLI is a code generator that creates complete, modern web applications using ASP.NET Core MVC, HTMX for interactivity, DaisyUI for UI components, and Entity Framework Core for data access. Generate full CRUD operations with pagination, search, sorting, filtering, and modal-based editing in seconds.

## 🌟 Why Swap?

- **⚡ Production-Ready Code** - Generate complete CRUD with modals, pagination, sorting, filtering, and search
- **🎯 HTMX Simplicity** - Modern, interactive web apps without JavaScript frameworks
- **� DaisyUI + Tailwind** - Beautiful, accessible components out of the box
- **🗄️ Entity Framework Core** - Full database integration with migrations support
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

# Short alias
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# Update database
dotnet ef migrations add AddProduct
dotnet ef database update
```

Visit `http://localhost:5000/Product` - Full CRUD with pagination, search, sorting, and filtering! 🚀

**No manual file creation. No boilerplate. Just CLI commands and business logic.**

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

## 📋 CLI Commands

### `swap new <name>`

Create a new ASP.NET Core + HTMX application with DaisyUI components.

```bash
swap new MyApp
```

**Generates:**
- Complete ASP.NET Core MVC project structure
- Entity Framework Core with SQLite (configurable)
- DaisyUI + Tailwind CSS configuration
- Sample TodoItem model and CRUD
- Database migrations
- Ready to run immediately

### `swap generate controller <name> --fields <fields>`

Generate a complete CRUD controller with all features.

```bash
# Generate Product controller with fields
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# With nullable fields
swap g c Customer --fields "Name:string Email:string Notes:string?"

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
### `swap generate test <controller>`

Generate an integration test class scaffold for a controller using Swap.Testing.

```bash
# Generate tests for TodoItemController
swap g test TodoItem

# Short alias
swap g t TodoItem

# Force overwrite
swap g test TodoItem --force

# Specify project/output
swap g test TodoItem --project path/to/project --output Tests
```

**Options:**
- `--force, -f` Overwrite existing file
- `--project, -p` Path to project (default: current dir)
- `--output, -o` Output folder (default: `Tests/`)

**What it generates:**
- `<Output>/<ControllerName>Tests.cs` with HTMX partial assertions
- Common test scenarios: index partial, create/edit forms, snapshot example
- References `Swap.Testing` package

**Example test:**
```csharp
[Fact]
public async Task Index_AsHtmx_ReturnsPartial()
{
    var resp = await _client.HtmxGetAsync("/todos");
    resp.AssertSuccess();
    await resp.AssertPartialViewAsync();
    await resp.AssertElementCountAsync(".todo-item", 3);
}
```

### `swap generate factory <entity>`

Generate a Bogus-powered test data factory for an entity model.

```bash
# Generate a factory from Models/Post.cs
swap g factory Post

# Short alias
swap g f Post

# Force overwrite
swap g factory Post --force

# Specify project/output
swap g factory Post --project path/to/project --output Tests/Factories
```

**Options:**
- `--force` Overwrite existing file
- `--project, -p` Path to project (default: current dir)
- `--output, -o` Output folder (default: `Tests/Factories/`)

**What it generates:**
- `<Output>/<Entity>Factory.cs` with intelligent property mappings
- Bogus rules based on property names (Email → f.Internet.Email(), etc.)
- Navigation properties skipped
- Nullable type support

**Example factory:**
```csharp
public static class PostFactory
{
    public static Post Generate()
    {
        var faker = new Faker<Post>()
            .RuleFor(p => p.Title, f => f.Lorem.Sentence())
            .RuleFor(p => p.Body, f => f.Lorem.Paragraphs(2))
            .RuleFor(p => p.PublishedAt, f => f.Date.Past());
        return faker.Generate();
    }
}
```

> If Bogus/Swap.Testing packages are missing, the CLI prints the commands to install them.

## 🧪 Swap.Testing (HTMX Testing Framework)

A fluent testing library purpose-built for HTMX applications, included with Swap.

**Key Features:**
- 🎯 **HTMX-Aware Client** - `HtmxGetAsync`, `HtmxPostAsync` with automatic HX-Request headers
- 🔍 **Rich Assertions** - `AssertPartialViewAsync`, `AssertHxGetAsync`, `AssertHxTriggered`
- 📸 **Snapshot Testing** - `AssertMatchesSnapshotAsync` with `UPDATE_SNAPSHOTS=true`
- ✅ **Validation Helpers** - `AssertHasValidationErrorsAsync`, `AssertFieldValidationErrorAsync`
- 🔄 **Form Helpers** - `SubmitFormAsync`, `FollowHxRedirectAsync`
- 🧹 **Snapshot Scrubbers** - Auto-replace GUIDs/timestamps/tokens for stable snapshots

**Quick Example:**
```csharp
public class PostControllerTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;
    public PostControllerTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task Create_Form_IsPartial_WithHtmxAttributes()
    {
        var resp = await _client.HtmxGetAsync("/posts/create");
        resp.AssertSuccess();
        await resp.AssertPartialViewAsync();
        await resp.AssertHxPostAsync("form", "/posts");
        await resp.AssertHxTargetAsync("form", "#post-list");
    }
}
```

**See also:**
- [Swap.Testing Framework Guide](../framework/Swap.Testing/README.md)
- [Testing Framework Wiki](https://jdtoon.github.io/swap/docs/features/testing-framework)
- Demo app: `testApps/HtmxTestingDemo/`

## 🧩 Swap.Patterns (Common Entity Patterns)

Add battle-tested patterns to your entities with single commands.

### `swap generate pattern softdelete <entity>`

Add soft delete functionality to any entity. Deleted records are hidden from queries but remain in the database.

```bash
# Add soft delete to Post entity
swap g pattern softdelete Post

# Short aliases
swap g p soft Post
```

**What it does:**
1. Adds `ISoftDeletable` interface to your entity
2. Adds three properties: `IsDeleted`, `DeletedAt`, `DeletedBy`
3. Adds using statement for `Swap.Patterns.SoftDelete`
4. Ensures `Swap.Patterns` package reference

**After generation:**
```csharp
public class Post : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // ISoftDeletable properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

**Next steps:**
1. Configure query filter in your `DbContext`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureSoftDeleteFilter();
}
```

2. Create and apply migration:
```bash
dotnet ef migrations add AddSoftDeleteToPost
dotnet ef database update
```

**Usage in code:**
```csharp
// Soft delete
post.SoftDelete("user@example.com");
await db.SaveChangesAsync();

// Restore
post.Restore();
await db.SaveChangesAsync();

// Query only deleted
var deleted = await db.Posts.OnlyDeleted().ToListAsync();

// Include deleted in results
var all = await db.Posts.IncludeDeleted().ToListAsync();

// Normal queries automatically exclude deleted
var active = await db.Posts.ToListAsync();
```

**See also:**
- [Swap.Patterns Library Guide](../framework/Swap.Patterns/README.md)
- [Entity Patterns Wiki](https://jdtoon.github.io/swap/docs/features/patterns)

---

### `swap generate pattern auditable <entity>`

Add automatic timestamp and user tracking to any entity. Records who created and modified each record and when.

```bash
# Add auditable to Post entity
swap g pattern auditable Post

# Short aliases
swap g p audit Post
```

**What it does:**
1. Adds `IAuditable` interface to your entity
2. Adds four properties: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
3. Adds using statement for `Swap.Patterns.Auditable`
4. Ensures `Swap.Patterns` package reference

**After generation:**
```csharp
public class Post : IAuditable
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // IAuditable properties
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**Next steps:**
1. Add HTTP context accessor in `Program.cs`:
```csharp
builder.Services.AddHttpContextAccessor();
```

2. Configure audit interceptor in your `DbContext`:
```csharp
private readonly IHttpContextAccessor _httpContextAccessor;

public AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor httpContextAccessor) : base(options)
{
    _httpContextAccessor = httpContextAccessor;
}

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor());
}
```

3. Create and apply migration:
```bash
dotnet ef migrations add AddAuditableToPost
dotnet ef database update
```

**How it works:**
- Timestamps and user IDs are set automatically on `SaveChanges()`
- `CreatedAt` and `CreatedBy` are set once when entity is added
- `UpdatedAt` and `UpdatedBy` are updated on every modification
- User ID comes from Claims (`NameIdentifier`, `Name`, or `Email`)

---

### `swap generate pattern sluggable <entity>`

Add SEO-friendly URL slugs to any entity. Perfect for blog posts, products, and content pages.

```bash
# Add sluggable to BlogPost entity
swap g pattern sluggable BlogPost

# Short aliases
swap g p slug BlogPost
```

**What it does:**
1. Adds `ISluggable` interface to your entity
2. Adds `Slug` property
3. Adds using statement for `Swap.Patterns.Sluggable`
4. Ensures `Swap.Patterns` package reference

**After generation:**
```csharp
public class BlogPost : ISluggable
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // ISluggable property
    public string Slug { get; set; } = "";
}
```

**Next steps:**
1. Add unique index in your `DbContext`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureSlugIndexes();
    
    // OR manually for specific entity:
    modelBuilder.Entity<BlogPost>()
        .HasIndex(e => e.Slug)
        .IsUnique();
}
```

2. Generate slug before saving (in controller or service):
```csharp
var post = new BlogPost { Title = "Hello World!" };
await post.GenerateSlugAsync(post.Title, _db);
_db.BlogPosts.Add(post);
await _db.SaveChangesAsync();
// post.Slug is now "hello-world"
```

3. Create and apply migration:
```bash
dotnet ef migrations add AddSlugToBlogPost
dotnet ef database update
```

**Features:**
- Converts text to URL-safe slugs: "Hello World!" → "hello-world"
- Handles international characters: "Café München" → "cafe-munchen"
- Automatic collision handling: "post" → "post-2" → "post-3"
- Configurable max length (default: 80 characters)

**Usage in routes:**
```csharp
// Find by slug instead of ID
var post = await _db.BlogPosts
    .FirstOrDefaultAsync(p => p.Slug == slug);
    
// Pretty URLs
// Before: /blog/123
// After:  /blog/hello-world
```

---

### `swap generate pattern timestampable <entity>`

Track creation and update timestamps automatically without user attribution. A lightweight alternative to Auditable when you only need dates.

```bash
# Add timestampable to Post entity
swap g pattern timestampable Post

# Short aliases
swap g p ts Post
```

**What it does:**
1. Adds `ITimestampable` interface to your entity
2. Adds two properties: `CreatedAt`, `UpdatedAt`
3. Adds using statement for `Swap.Patterns.Timestampable`
4. Ensures `Swap.Patterns` package reference

**After generation:**
```csharp
public class Post : ITimestampable
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // ITimestampable properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Next steps:**
1. Configure timestamp interceptor in your `DbContext`:
```csharp
using Swap.Patterns.Timestampable;

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new TimestampInterceptor());
}
```

2. Create and apply migration:
```bash
dotnet ef migrations add AddTimestampsToPost
dotnet ef database update
```

**How it works:**
- `CreatedAt` set on insert; `UpdatedAt` set on every modification
- Requires no `IHttpContextAccessor`
- Safe to combine with most patterns (but not with Auditable)

---

### `swap generate pattern orderable <entity>`

Add a stable `Position` property and helpers for ordering lists and drag-and-drop UIs.

```bash
# Add orderable to Category entity
swap g pattern orderable Category

# Short aliases
swap g p order Category
```

**What it does:**
1. Adds `IOrderable` interface to your entity
2. Adds `Position` property
3. Adds using statement for `Swap.Patterns.Orderable`
4. Ensures `Swap.Patterns` package reference

**After generation:**
```csharp
public class Category : IOrderable
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // IOrderable property
    public int Position { get; set; }
}
```

**Usage:**
```csharp
// Get next position for a new item
var next = await _db.Categories.GetNextPositionAsync();
db.Categories.Add(new Category { Name = "New", Position = next });

// Reorder an item (1-based index)
var item = await _db.Categories.FindAsync(id);
await _db.Categories.ReorderAsync(item!, newPosition: 1);
await _db.SaveChangesAsync();

// Normalize positions after deletes/bulk ops
await _db.Categories.NormalizePositionsAsync();
await _db.SaveChangesAsync();

// Convenient ordering for queries
var ordered = await _db.Categories.OrderByPosition().ToListAsync();
```

---

### Combining Patterns

You can mix and match compatible patterns on the same entity. Do not combine Auditable and Timestampable together (both define CreatedAt/UpdatedAt).

```csharp
// Example with Auditable
public class Product : ISoftDeletable, IAuditable, ISluggable, IOrderable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    
    // Soft Delete (3 properties)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Auditable (4 properties)
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Sluggable (1 property)
    public string Slug { get; set; } = "";

    // Orderable (1 property)
    public int Position { get; set; }
}
```

Apply all patterns in sequence:
```bash
swap g pattern softdelete Product
swap g pattern auditable Product
swap g pattern sluggable Product
swap g pattern orderable Product
```

Alternatively, replace Auditable with Timestampable when you don't need user attribution:
```bash
swap g pattern softdelete Product
swap g pattern timestampable Product
swap g pattern sluggable Product
swap g pattern orderable Product
```

---

## 📦 Generate Resource

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

#### `swap db migrate [name] [--apply]`

Create and/or apply Entity Framework Core migrations.

```bash
# Create a new migration
swap db migrate AddProductTable

# Create and apply immediately
swap db migrate AddProductTable --apply

# Apply pending migrations
swap db migrate --apply
```

#### `swap db reset [--force]`

Drop and recreate the database for a fresh start.

```bash
swap db reset
swap db reset --force
```

#### `swap db seed [--count] [--locale] [--if-empty]`

Run database seeders via application startup.

```bash
swap db seed --count 100 --locale en_GB --if-empty
```

### `swap doctor`

Check your development environment and dependencies.

```bash
swap doctor
```

Checks .NET SDK, dotnet-ef, Node.js, npm, and libman installations.

### `swap list [--project]`

List all resources (entities) in your project with their completeness status.

```bash
swap list
swap list --project path/to/project
```

Shows which entities have models, controllers, and seeders.

## 📚 Documentation

- **[Getting Started](https://jdtoon.github.io/swap/)** - Complete setup guide
- **[CLI Reference](https://jdtoon.github.io/swap/docs/cli/overview)** - All commands and options
- **[Features Guide](https://jdtoon.github.io/swap/docs/features/pagination)** - Pagination, search, sorting, filtering
- **[Pattern Library](docs/PATTERNS-LIBRARY.md)** - 30+ proven HTMX patterns
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

## � Project Status

**Current Version:** `0.1.0-dev` (Active Development)

### ✅ Phase 2C Complete (Current)

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

### ✅ Phase 2D Complete: Database Seeders

- ✅ **Seeder Generation** - `swap g seed <entity>` and `swap g seed all`
- ✅ **Bogus Integration** - Realistic fake data with smart field heuristics
- ✅ **Environment Control** - SEED_COUNT, SEED_LOCALE, SEED_IFEMPTY
- ✅ **Foreign Key Support** - Automatic relationship handling
- ✅ **Development Startup** - Auto-seed on app launch in Development mode
- ✅ **Idempotent Seeding** - `--if-empty` flag for safe repeated runs

### 🎯 Phase 3: Polish & Release

- ⏳ **NuGet Package** - Publish to NuGet.org
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



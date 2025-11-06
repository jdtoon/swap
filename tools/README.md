# Swap CLI Tools

[![NuGet: Swap.CLI](https://img.shields.io/nuget/v/Swap.CLI.svg?label=Swap.CLI&color=0078d4)](https://www.nuget.org/packages/Swap.CLI)

The Swap CLI is a comprehensive code generation tool for creating and scaffolding ASP.NET Core + HTMX applications.

---

## 🎯 What Swap CLI Does

Swap CLI generates complete, production-ready applications and features from templates. It's the fastest way to:

- 📦 Create new projects from templates (Monolith, Layered, Modular Monolith)
- 🏛️ Generate CRUD controllers with full feature set (search, pagination, sorting, filtering, modals)
- 🔗 Add relationships and auto-generate UI (dropdowns, checkboxes)
- 🗄️ Create database seeders and test factories
- 🧪 Generate integration test scaffolds
- 🔐 Scaffold authentication flows
- 📐 Apply entity patterns (Soft Delete, Auditable, Sluggable, etc.)

**No manual boilerplate. Just CLI commands.**

---

## ⚡ Quick Start

### Install

```bash
dotnet tool install --global Swap.CLI
swap --version
```

### Create Your First Project

```bash
swap new MyApp              # Monolith (default)
cd MyApp
dotnet run
```

Visit `http://localhost:5000` — your app is live! 🚀

---

## 🛠️ Command Reference

### Project Generation

```bash
# Create new monolith project
swap new MyApp

# Create layered architecture project
swap new MyApp --template swap-layered

# Create modular monolith
swap new MyApp --template swap-modular

# Choose database provider
swap new MyApp --database sqlserver     # SQL Server
swap new MyApp --database postgres      # PostgreSQL
# (default is sqlite)

# Use local NuGet packages (dev only)
swap new MyApp --local-nuget

# Skip setup steps (npm, libman, migrations)
swap new MyApp --skip-setup
```

### Model Generation

```bash
# Create a simple model
swap generate model Product --fields "Name:string Price:decimal"

# With nullable fields
swap g m Post --fields "Title:string Content:string? Tags:string?"

# Shorthand: swap g m
swap g m Category --fields "Name:string Description:string?"
```

**Field Syntax:** `Name:Type` where Type is: `string`, `int`, `decimal`, `bool`, `DateTime`, etc.

### Controller Generation

```bash
# Generate full CRUD controller
swap generate controller Product --fields "Name:string Price:decimal"

# With all features enabled
swap g c Product --fields "Name:string Price:decimal InStock:bool:f" --add-nav

# Regenerate existing controller (useful after adding relationships)
swap g c Product --force

# Shorthand: swap g c
```

**Features included:**
- ✅ Index with search, pagination, sorting, filtering
- ✅ Create modal with form validation
- ✅ Edit modal with pre-filled form
- ✅ Delete confirmation modal
- ✅ Details view
- ✅ Automatic relationship dropdowns

### Relationship Management

```bash
# Many-to-one (Post → Category)
swap generate rel -s Post -t Category --type many-to-one

# One-to-many (Author has many Posts)
swap generate rel -s Author -t Post --type one-to-many

# Many-to-many (Post ↔ Tags)
swap generate rel -s Post -t Tag --type many-to-many

# One-to-one (User ↔ Profile)
swap generate rel -s User -t Profile --type one-to-one
```

After adding relationships, regenerate controllers to get automatic dropdowns/checkboxes:
```bash
swap g c Post --force
dotnet ef database update
```

### Entity Patterns

```bash
# Apply Soft Delete pattern
swap generate pattern softdelete Post --use-package

# Apply Auditable pattern
swap g pattern auditable Post --use-package

# Apply Sluggable pattern
swap g pattern sluggable Post --use-package

# Available patterns:
# - softdelete     (Logical deletion with restore)
# - auditable      (Track who created/modified/deleted)
# - sluggable      (URL-friendly slugs)
# - timestampable  (Auto CreatedAt/UpdatedAt)
# - publishable    (Draft/published workflow)
# - orderable      (Display order management)
# - versionable    (Version tracking)
# - visibility     (Public/private scheduling)
```

### Testing

```bash
# Generate integration test scaffold
swap generate test Product
swap g test Post

# Generates test class using Swap.Testing with HTMX assertions
```

### Authentication

```bash
# Scaffold identity/auth
swap generate auth
swap g auth

# Generates:
# - ApplicationUser.cs
# - AuthController with login/register/password reset
# - Views for auth pages
# - _LoginPartial for navbar
```

### Database Operations

```bash
# Show database configuration
swap db info

# Create and apply migrations
swap db migrate

# Run seeders
swap db seed

# Reset database (drop/recreate/seed)
swap db reset
```

### Event System Tooling

```bash
# List all event chains in your project
swap events list -p .

# Validate chains for cycles and errors
swap events validate -p .

# Generate chain diagram (Mermaid format)
swap events graph -p . --format mermaid

# Export as GraphViz DOT
swap events graph -p . --format dot --output diagram.dot

# Query running app for active events
swap events from-server --url http://localhost:5000
```

### Utilities

```bash
# List all entities in project
swap list

# Diagnose issues
swap doctor

# Show version
swap --version
```

---

## 📚 Project Structure

```
tools/
├── Swap.CLI/               # CLI implementation
│   ├── Commands/           # Command implementations
│   │   ├── NewCommand.cs                    # swap new
│   │   ├── GenerateModelCommand.cs          # swap g m
│   │   ├── GenerateControllerCommand.cs     # swap g c
│   │   ├── GenerateRelationshipCommand.cs   # swap g rel
│   │   ├── GeneratePatternCommand.cs        # swap g pattern
│   │   ├── GenerateAuthCommand.cs           # swap g auth
│   │   ├── GenerateTestCommand.cs           # swap g test
│   │   ├── EventsCommand.cs                 # swap events
│   │   └── DatabaseCommand.cs               # swap db
│   ├── Infrastructure/     # Core utilities
│   │   ├── TemplateEngine.cs        # Template processing
│   │   ├── FieldHelper.cs           # Field parsing
│   │   ├── MigrationHelper.cs       # EF Core migration creation
│   │   ├── ProjectScanner.cs        # Project analysis
│   │   └── RelationshipHelper.cs    # Relationship logic
│   ├── Program.cs          # CLI entry point
│   └── README.md           # Detailed CLI reference
└── Swap.CLI.Tests/         # Comprehensive test suite (160+ tests)
    ├── Commands/           # Command tests
    ├── Infrastructure/     # Infrastructure tests
    └── README.md           # Test documentation
```

---

## 🔧 How It Works

### 1. Template Processing

All generated code comes from templates in `templates/`:

- **`templates/monolith/`** — Single-project template
- **`templates/swap-layered/`** — 4-project layered template
- **`templates/swap-modular/`** — Modular monolith template
- **`templates/generate/`** — CRUD and feature generation templates

Templates use variable substitution: `{{ProjectName}}`, `{{EntityName}}`, etc.

### 2. Variable Replacement

The template engine replaces placeholders with your values:

```csharp
// Template
namespace {{ProjectName}}.Controllers;
public class {{EntityName}}Controller : SwapController { }

// After processing
namespace MyApp.Controllers;
public class ProductController : SwapController { }
```

### 3. Conditional Generation

Templates can include conditional blocks for database providers:

```csharp
{{#if_sqlite}}
    options.UseSqlite(connectionString);
{{/if_sqlite}}

{{#if_sqlserver}}
    options.UseSqlServer(connectionString);
{{/if_sqlserver}}
```

### 4. Build-Before-Migration

For quality assurance, the CLI:

1. Generates your code
2. **Builds the project** with `dotnet build`
3. If build succeeds → Creates EF Core migration
4. If build fails → Shows errors, stops (no migration created)

This prevents cryptic EF Core errors and catches issues early.

### 5. Post-Generation Setup

After creating a project, the CLI automatically:
- ✅ Runs `npm install` (for Tailwind)
- ✅ Runs `libman restore` (for HTMX, DaisyUI)
- ✅ Builds CSS with Tailwind
- ✅ Creates and applies EF Core migrations
- ✅ Applies development migrations on `dotnet run`

---

## 💡 Example Workflows

### Complete Blog in 5 Minutes

```bash
swap new Blog
cd Blog

# Create entities
swap g m Author --fields "Name:string Email:string"
swap g m Category --fields "Name:string"
swap g m Post --fields "Title:string Content:string AuthorId:int CategoryId:int"

# Add relationships
swap g rel -s Post -t Author --type many-to-one
swap g rel -s Post -t Category --type many-to-one

# Generate controllers
swap g c Author
swap g c Category
swap g c Post --force  # Regenerate to get dropdowns

# Add soft delete pattern
swap g pattern softdelete Post --use-package

# Run
dotnet run
```

### Layered Architecture

```bash
swap new MyApp --template swap-layered
cd MyApp

# Create domain models in Domain/
swap g m Product --fields "Name:string Price:decimal"
swap g m Category --fields "Name:string"

# Add relationships
swap g rel -s Product -t Category --type many-to-one

# Generate Web controllers
swap g c Product
swap g c Category

# Test with auth
swap g auth
```

### Modular Monolith

```bash
swap new MyApp --template swap-modular
cd MyApp

# Each module owns its features
# Core module already has User entity

# Generate for Todos module
swap g m TodoItem --fields "Title:string Completed:bool" --module Todos
swap g c TodoItem --module Todos

# Generate for Demo module  
swap g m DemoItem --fields "Name:string" --module Demo
swap g c DemoItem --module Demo
```

---

## 🏗️ Advanced Features

### Local NuGet Development

When working on framework changes (Swap.Htmx, Swap.Modularity, etc.):

```bash
# Pack all framework packages locally
.\scripts\pack-local.ps1

# Create test project with local packages
swap new MyTestApp --local-nuget

# Test your changes
cd MyTestApp
dotnet restore
dotnet build
```

See [CONTRIBUTING.md](../CONTRIBUTING.md) for complete local development workflow.

### Custom Templates

To add custom generation templates:

1. Create template file in `templates/generate/`
2. Add variable placeholders (`{{VariableName}}`)
3. Create corresponding command in `tools/Swap.CLI/Commands/`
4. Register template in command's handler

Example:
```csharp
// In your command
var templatePath = Path.Combine(templatesDir, "generate", "mytype", "Template.cs.template");
var content = File.ReadAllText(templatePath);
var processed = TemplateEngine.Process(content, variables);
```

---

## 🧪 Testing

### Run All Tests

```bash
cd tools/Swap.CLI.Tests
dotnet test
```

### Run Specific Tests

```bash
# Test model generation
dotnet test --filter "FullyQualifiedName~ModelGeneration"

# Test controller generation
dotnet test --filter "FullyQualifiedName~ControllerGeneration"
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🐛 Troubleshooting

### "Tool 'swap.cli' is not installed"

Install the CLI:
```bash
dotnet tool install --global Swap.CLI
```

Or reinstall from local packages:
```bash
.\scripts\reinstall-cli.ps1
```

### "Project is not a valid Swap project"

Make sure you're in a project directory created by `swap new` with:
- `Project.csproj` file
- `Program.cs` with Swap setup
- Proper project structure

### "Build failed before migration"

The CLI detected compilation errors. Fix them before retrying:
```bash
dotnet build
# Fix errors...
dotnet build  # Verify it compiles
```

### "Template not found"

Ensure Swap CLI is the latest version:
```bash
dotnet tool update --global Swap.CLI
```

### Local NuGet feed not working

Ensure packages are packed and CLI is reinstalled:
```bash
.\scripts\pack-local.ps1
.\scripts\reinstall-cli.ps1
```

---

## 📦 Release Notes

See [CHANGELOG.md](../CHANGELOG.md) for version history and breaking changes.

---

## 🤝 Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for:
- Development setup
- Running tests locally
- Code style guidelines
- Pull request process

---

## 📚 More Resources

- **[Main README](../README.md)** — Swap Framework overview
- **[framework/README.md](../framework/README.md)** — Library documentation
- **[docs/](../docs/)** — Templates, architecture, patterns
- **[templates/README.md](../templates/README.md)** — Template reference
- **[CONTRIBUTING.md](../CONTRIBUTING.md)** — Development guide

---

**Questions?** Open an [issue](https://github.com/jdtoon/swap/issues) or start a [discussion](https://github.com/jdtoon/swap/discussions)!

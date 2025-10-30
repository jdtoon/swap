---
sidebar_position: 1
---

# CLI Overview

Swap CLI scaffolds ASP.NET Core applications with HTMX views.

## Command Structure

```bash
swap <command> [subcommand] [arguments] [options]
```

## Available Commands

| Command | Description | Aliases |
|---------|-------------|---------|
| `swap new` | Create a new project | - |
| `swap generate model` | Generate an entity model | `g m`, `generate m` |
| `swap generate controller` | Generate a CRUD controller | `g c`, `generate c` |
| `swap generate resource` | Generate model + controller + views | `g r`, `generate r` |
| `swap generate pattern` | Apply entity patterns (soft delete, etc.) | `g pattern`, `g p` |
| `swap generate auth` | Scaffold ASP.NET Identity authentication | `g auth` |
| `swap generate seed` | Generate database seeders | `g s`, `generate s`, `g seed` |
| `swap generate test` | Generate integration test scaffold | `g test`, `g t` |
| `swap generate factory` | Generate Bogus test data factory | `g factory`, `g f` |
| `swap database info` | Show database configuration and status | `db info` |
| `swap database migrate` | Create and apply EF migrations | `db migrate` |
| `swap database seed` | Run database seeders | `db seed` |
| `swap database reset` | Drop and recreate database | `db reset` |
| `swap doctor` | Check development environment | - |
| `swap list` | List all resources in project | - |

## Quick Examples

### Create Project

```bash
swap new MyApp
```

### Generate Model

```bash
swap g m Product --fields "Name:string Price:decimal Stock:int"
swap g m Product --fields Name:string,Price:decimal,Stock:int

# Preview without writing files
swap g m Product --fields "Name:string Price:decimal" --dry-run

# Overwrite existing file
swap g m Product --fields "Name:string Price:decimal" --force
```

### Generate Controller

```bash
swap g c Product --fields "Name:string Price:decimal Stock:int"
swap g c Product --fields Name:string,Price:decimal,Stock:int

# Preview what would be generated
swap g c Product --fields "Name:string" --dry-run

# Overwrite without prompting
swap g c Product --fields "Name:string" --force

# Generate in different directory
swap g c Product --fields "Name:string" --project path/to/project
```

### Generate Complete Resource

```bash
swap g r Order --fields "CustomerId:int Total:decimal OrderDate:datetime"
swap g r Order --fields CustomerId:int,Total:decimal,OrderDate:datetime

# With generator options
swap g r Order --fields "Total:decimal" --dry-run --project path/to/project
```

### Generate Seeders

```bash
# Single entity
swap g seed Product --count 100 --locale en --if-empty
# All entities
swap g seed all --count 50 --locale en --if-empty
# Short alias
swap g s all --count 50 --locale en --if-empty
# Overwrite existing seeder
swap g s Product --force
```

### Generate Tests and Factories

```bash
# Generate integration test scaffold
swap g test TodoItem
swap g t TodoItem  # short alias

# Generate Bogus test data factory
swap g factory Post
swap g f Post  # short alias

# In different project
swap g test TodoItem --project testApps/MyApp --output Tests
swap g factory Post --project testApps/MyApp --output Tests/Factories
```

### Apply Entity Patterns

```bash
# Add soft delete pattern to entity
swap g pattern softdelete Post
swap g p soft Post  # aliases

# Apply to specific project
swap g pattern softdelete Product --project path/to/project

# Force overwrite
swap g pattern softdelete Order --force
```

### Database Commands

```bash
# Show database info
swap db info

# Create migration
swap db migrate AddProductTable

# Create and apply migration
swap db migrate AddProductTable --apply

# Apply pending migrations
swap db migrate --apply

# Reset database (with confirmation)
swap db reset

# Reset database (skip confirmation)
swap db reset --force

# Run seeders
swap db seed --count 100 --locale en_GB --if-empty
```

### Developer Utilities

```bash
# Check environment and dependencies
swap doctor

# List all resources in project
swap list

# List resources in another project
swap list --project path/to/project
```

## HTMX Integration

All generated views use HTMX for dynamic updates:

- **List views** - Load data without page refresh
- **Forms** - Submit via AJAX
- **Inline editing** - Update records in place
- **Partial rendering** - Return HTML fragments

Example generated pattern:

```html
<!-- Index.cshtml -->
<div hx-get="/Product/List" hx-trigger="load" hx-target="#list">
    <div id="list">Loading...</div>
</div>

<!-- Controller action -->
public async Task<IActionResult> List()
{
    var products = await _context.Products.ToListAsync();
    return PartialView("_ProductList", products);
}
```

## Field Types

Supported types in `--fields`:

| Type | Example | C# Type |
|------|---------|---------|
| `string` | `Name:string` | `string` (required) |
| `string?` | `Notes:string?` | `string?` (nullable) |
| `int` | `Age:int` | `int` |
| `decimal` | `Price:decimal` | `decimal` |
| `bool` | `IsActive:bool` | `bool` |
| `datetime` | `CreatedAt:datetime` | `DateTime` |
| `long` | `FileSize:long` | `long` |
| `double` | `Rating:double` | `double` |
| `float` | `Score:float` | `float` |
| `guid` | `UniqueId:guid` | `Guid` |

## Field Flags

You can add flags to fields to control sorting and filtering behavior:

| Flag | Short | Description | Applies To |
|------|-------|-------------|------------|
| `:sortable` | `:s` | Enable sorting on this column (default for most fields) | All fields |
| `:nosort` or `:ns` | `:nosort`, `:ns` | Disable sorting on this column | All fields |
| `:filterable` | `:f` | Add a filter dropdown | `bool` fields only |

### Flag Syntax

Use comma-separated flags after the type:

```bash
# Space or comma separated fields, flags after colon
swap g r Product --fields "Name:string:s,f Price:decimal:s Stock:int:ns IsActive:bool:f"
swap g r Product --fields Name:string:s,f,Price:decimal:s,Stock:int:ns,IsActive:bool:f
```

This creates:
- **Name** - Sortable column with filter (note: string filters not yet implemented)
- **Price** - Sortable column (has sort arrows)
- **Stock** - Non-sortable column (plain text header)
- **IsActive** - Filterable boolean (dropdown with Yes/No/All options)

### Examples

```bash
swap g r Post --fields "Title:string:s Content:string:ns PublishedAt:datetime:s"
swap g r Post --fields Title:string:s,Content:string:ns,PublishedAt:datetime:s

swap g r Task --fields "Title:string Done:bool:f Priority:int:s"
swap g r Task --fields Title:string,Done:bool:f,Priority:int:s

swap g r Product --fields "Name:string:s,f Price:decimal:s InStock:bool:f"
swap g r Product --fields Name:string:s,f,Price:decimal:s,InStock:bool:f
```

## Common Workflows

### Simple CRUD

```bash
# Create project
swap new MyApp
cd MyApp

# Generate resource
swap g r Product --fields Name:string,Price:decimal

# Create migration
dotnet ef migrations add AddProduct
dotnet ef database update

# Run
dotnet run
```

### Multiple Resources

```bash
# Customer
swap g r Customer --fields Name:string,Email:string

# Order
swap g r Order --fields CustomerId:int,Total:decimal

# OrderItem  
swap g r OrderItem --fields OrderId:int,ProductId:int,Quantity:int

# Migrate
dotnet ef migrations add AddECommerce
dotnet ef database update
```

## Project Structure

Generated projects follow this structure:

```
MyApp/
├── Controllers/
│   ├── HomeController.cs
│   └── ProductController.cs
├── Models/
│   └── Product.cs
├── Views/
│   ├── Product/
│   │   ├── Index.cshtml          # Main view
│   │   ├── _List.cshtml          # HTMX partial for table
│   │   ├── _AddModal.cshtml      # Create form modal
│   │   └── _EditModal.cshtml     # Edit form modal
│   └── Shared/
│       ├── _Layout.cshtml        # DaisyUI layout
│       └── _Pagination.cshtml    # Pagination component
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── wwwroot/
│   ├── lib/
│   │   ├── htmx/                 # HTMX library
│   │   └── toastify-js/          # Toast notifications
│   └── css/
│       ├── tailwind.css          # Tailwind output
│       └── site.css              # Custom styles
├── tailwind.config.js            # Tailwind + DaisyUI config
├── Program.cs
└── MyApp.csproj
```

## Next Steps

- [swap new](./new) - Project scaffolding options
- [swap generate model](./generate-model) - Model generation details
- [swap generate controller](./generate-controller) - Controller generation details
- [swap generate resource](./generate-resource) - Combined generation

---
sidebar_position: 1
---

# CLI Overview

Swap CLI creates ASP.NET Core applications with HTMX from templates.

## Command Structure

```bash
swap <command> [subcommand] [arguments] [options]
```

## Available Commands

| Command | Description | Aliases |
|---------|-------------|---------|
| `swap new` | Create a new project from template | - |
| `swap generate htmx-shell` | Generate HTMX shell middleware | `g htmx-shell` |
| `swap events list` | List all event chains in project | - |
| `swap events validate` | Validate event chains for cycles/errors | - |
| `swap events graph` | Generate event chain diagram | - |

## Quick Examples

### Create Project

```bash
swap new MyApp

# Choose template
swap new MyApp --template swap-modular-monolith

# Choose database provider
swap new MyApp --database postgres
```

### Generate HTMX Shell

```bash
swap generate htmx-shell

# Short alias
swap g htmx-shell
```

### Event System Commands

```bash
# List all event chains
swap events list -p .

# Validate chains
swap events validate -p .

# Generate Mermaid diagram
swap events graph -p . --format mermaid

# Generate DOT format
swap events graph -p . --format dot --output diagram.dot
```

## Global Options

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
## Global Options

Common options available across commands:

- `--help, -h` - Show help for a command
- `--version` - Show CLI version

## Next Steps

- [swap new](./new) - Create new projects from templates
- [swap generate htmx-shell](./generate-htmx-shell) - Add HTMX shell middleware
- [swap events](./events) - Event system commands

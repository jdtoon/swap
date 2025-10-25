---
sidebar_position: 1
---

# CLI Overview

Swap CLI (`swap`) is a powerful command-line interface for scaffolding and managing ASP.NET Core applications with the NetMX framework.

## Why "swap"?

The command name `swap` is short, memorable, and represents the idea of rapidly swapping between different development tasks and scaffolding operations.

## Command Structure

All commands follow this pattern:

```bash
swap <command> [subcommand] [arguments] [options]
```

## Available Commands

### Core Commands

| Command | Description | Alias |
|---------|-------------|-------|
| `swap new` | Create a new project | - |
| `swap generate` | Generate code artifacts | `g` |
| `swap --version` | Show CLI version | `-v` |
| `swap --help` | Show help information | `-h`, `-?` |

### Generate Subcommands

| Command | Description | Alias |
|---------|-------------|-------|
| `swap generate model` | Generate an entity model | `g m` |
| `swap generate controller` | Generate a CRUD controller | `g c` |
| `swap generate resource` | Generate model + controller | `g r` |

## Command Aliases

The CLI supports short aliases for faster typing:

```bash
# These are equivalent
swap generate model Product
swap g m Product

# These are equivalent
swap generate controller User
swap g c User
```

## Global Options

Available on all commands:

| Option | Description |
|--------|-------------|
| `--help`, `-h`, `-?` | Display help for command |
| `--version`, `-v` | Display CLI version |
| `--verbose` | Show detailed output |

## Common Patterns

### Check Version

```bash
swap --version
# or
swap -v
```

### Get Help

```bash
# General help
swap --help

# Command-specific help
swap new --help
swap generate model --help
```

### Verbose Output

Get detailed logs for debugging:

```bash
swap new MyApp --verbose
swap generate model Product --fields Name:string --verbose
```

## Project Detection

Most `generate` commands auto-detect the project context:

- Finds `.csproj` file in current directory
- Extracts project name for namespaces
- Locates `AppDbContext.cs` for database updates
- Validates project structure

Always run `generate` commands from your project root:

```bash
cd MyApp
swap generate model Product  # ✅ Correct
```

```bash
cd MyApp/Models
swap generate model Product  # ❌ Wrong - can't find .csproj
```

## Output and Feedback

The CLI provides rich, color-coded output:

- ✅ **Green** - Success messages
- ⚠️ **Yellow** - Warnings (e.g., file exists)
- ❌ **Red** - Errors
- ℹ️ **Blue** - Information

Example output:

```
Generating entity model: Product
Project: MyApp
Fields: 3
  • Name: string (required)
  • Price: decimal
  • Stock: int

✓ Model generated successfully!

Generated files:
  Models/Product.cs
  
Updated:
  Data/AppDbContext.cs
```

## Environment Variables

Configure CLI behavior with environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `NETMX_TEMPLATE_PATH` | Custom template directory | Built-in templates |
| `NETMX_DEFAULT_DB` | Default database provider | `sqlite` |
| `NETMX_VERBOSE` | Always use verbose output | `false` |

Example:

```bash
# Windows PowerShell
$env:NETMX_DEFAULT_DB = "sqlserver"
swap new MyApp  # Uses SQL Server by default

# Linux/macOS
export NETMX_DEFAULT_DB="postgresql"
swap new MyApp  # Uses PostgreSQL by default
```

## Error Handling

The CLI validates inputs and provides helpful error messages:

### Invalid Command

```bash
swap generate unknown
# Error: 'unknown' is not a valid subcommand
# Run 'swap generate --help' for available commands
```

### Missing Required Argument

```bash
swap new
# Error: Required argument 'name' was not provided
# Usage: swap new <name> [options]
```

### Invalid Field Specification

```bash
swap generate model Product --fields Name:invalid
# Error: Unsupported field type 'invalid' for field 'Name'
# Supported types: string, int, bool, decimal, double, float, long, short, byte, datetime, guid
```

### Project Not Found

```bash
swap generate model Product
# Error: No .csproj file found in current directory
# Make sure you're in the project root
```

## Debugging

### Check What Files Would Be Generated

Use `--dry-run` (coming soon):

```bash
swap generate model Product --fields Name:string --dry-run
```

### Verbose Logging

See exactly what the CLI is doing:

```bash
swap generate controller Product --verbose
```

Output includes:
- Template resolution
- File path calculations
- DbContext parsing
- Code generation steps

## Best Practices

### 1. Use Version Control

Always commit before running generate commands:

```bash
git add .
git commit -m "Before generating Product model"
swap generate model Product --fields Name:string,Price:decimal
```

### 2. Review Generated Code

The CLI generates production-ready code, but review and customize:

```bash
swap generate controller Product
# Review Controllers/ProductController.cs
# Add business logic, validation, authorization
```

### 3. Use Short Aliases

Save time with aliases:

```bash
swap g m Product --fields Name:string
swap g c Product
```

### 4. Run from Project Root

Always execute generate commands from the project root:

```bash
cd MyApp       # ✅ Correct
swap g m Product

cd MyApp/Models  # ❌ Wrong
swap g m Product  # Error: No .csproj found
```

### 5. Check Help When Stuck

Every command has built-in help:

```bash
swap generate model --help
```

## Next Steps

Learn about specific commands:

- [swap new](./new) - Create new projects
- [swap generate model](./generate-model) - Generate entity models
- [swap generate controller](./generate-controller) - Generate CRUD controllers
- [swap generate resource](./generate-resource) - Generate complete resources

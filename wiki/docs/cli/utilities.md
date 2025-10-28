---
sidebar_position: 7
---

# Developer Utilities

Commands to check your environment and inspect your project.

## `swap doctor`

Check your development environment and verify all required and optional dependencies are installed.

### Usage

```bash
swap doctor
```

### What It Checks

| Tool | Required? | Purpose |
|------|-----------|---------|
| .NET SDK | ✅ Required | ASP.NET Core development |
| dotnet-ef | ✅ Required | Entity Framework migrations |
| Node.js | ⚠️ Optional | Tailwind CSS compilation |
| npm | ⚠️ Optional | Package management (comes with Node.js) |
| libman | ⚠️ Optional | Client library management (HTMX, DaisyUI) |

### Example Output

```bash
PS C:\MyApp> swap doctor

Checking development environment...

╭───────────┬─────────────┬──────────┬─────────────────────────╮
│ Tool      │ Status      │ Version  │ Notes                   │
├───────────┼─────────────┼──────────┼─────────────────────────┤
│ .NET SDK  │ ✓ Installed │ 9.0.100  │                         │
│ dotnet-ef │ ✓ Installed │ 9.0.10   │                         │
│ Node.js   │ ✓ Installed │ 22.15.1  │ Needed for Tailwind CSS │
│ npm       │ ⚠ Missing   │ N/A      │ Optional - Comes with Node.js │
│ libman    │ ✓ Installed │ 3.0.71   │ Needed for client libraries │
╰───────────┴─────────────┴──────────┴─────────────────────────╯

⚠ 1 optional tool(s) missing

Optional installations:
  Node.js: https://nodejs.org/
  libman: dotnet tool install --global Microsoft.Web.LibraryManager.Cli
```

### Status Indicators

- **✓ Installed** (green) - Tool is installed and working
- **✗ Missing** (red) - Required tool not found
- **⚠ Missing** (yellow) - Optional tool not found

### Installation Help

When tools are missing, `swap doctor` provides installation commands:

**dotnet-ef:**
```bash
dotnet tool install --global dotnet-ef
```

**libman:**
```bash
dotnet tool install --global Microsoft.Web.LibraryManager.Cli
```

**Node.js:**
- Visit https://nodejs.org/ for download

### Use Cases

- **First-time setup** - Verify environment before starting development
- **Troubleshooting** - Check if tools are properly installed
- **Team onboarding** - Quick environment validation for new developers
- **CI/CD** - Verify build agent has necessary tools

## `swap list [--project]`

List all resources (entities) in your project and see which components exist for each.

### Usage

```bash
# List resources in current project
swap list

# List resources in another project
swap list --project path/to/project
```

### Options

- `--project` or `-p` - Path to project directory (default: current directory)

### What It Shows

For each entity in your `DbContext`:
- ✓/✗ **Model** - Does `Models/{Entity}.cs` exist?
- ✓/✗ **Controller** - Does `Controllers/{Entity}Controller.cs` exist?
- ✓/✗ **Seeder** - Does `Data/Seeders/{Entity}Seeder.cs` exist?

### Example Output

```bash
PS C:\MyApp> swap list

Resources in MyApp:

╭──────────┬───────┬────────────┬────────╮
│ Entity   │ Model │ Controller │ Seeder │
├──────────┼───────┼────────────┼────────┤
│ Product  │ ✓     │ ✓          │ ✓      │
│ Customer │ ✓     │ ✓          │ ✗      │
│ Order    │ ✓     │ ✗          │ ✗      │
│ TodoItem │ ✓     │ ✗          │ ✗      │
╰──────────┴───────┴────────────┴────────╯

Found 4 resource(s)
```

### Interpretation

In the example above:
- **Product** - Complete resource (model, controller, seeder all present)
- **Customer** - Has model and controller, but no seeder generated yet
- **Order** - Only has model, missing controller and seeder
- **TodoItem** - Only has model (sample entity from `swap new`)

### Use Cases

- **Project overview** - See which entities are fully scaffolded
- **Gap analysis** - Identify missing controllers or seeders
- **Code coverage** - Verify code generation completeness
- **Planning** - Determine which entities need work

### Common Actions

After running `swap list`, you might:

```bash
# Generate missing controller for Order
swap g c Order --fields "CustomerId:int Total:decimal"

# Generate missing seeder for Customer
swap g s Customer

# Generate seeder for Order
swap g s Order
```

## Common Workflows

### New Developer Onboarding

```bash
# 1. Check environment
swap doctor

# 2. Clone project
git clone https://github.com/myorg/myapp.git
cd myapp

# 3. See what resources exist
swap list

# 4. Check database status
swap db info

# 5. Apply migrations
swap db migrate --apply

# 6. Seed data
swap db seed --count 50
```

### Health Check Before Starting Work

```bash
# Verify environment
swap doctor

# See project status
swap list

# Check database
swap db info
```

### Troubleshooting Build Issues

```bash
# Check if all tools are installed
swap doctor

# Verify project structure
swap list

# Check database migrations
swap db info
```

## Tips

- Run `swap doctor` after updating .NET SDK or global tools
- Run `swap doctor` on CI/CD agents to verify build environment
- Use `swap list` to quickly see project completeness
- Combine with `swap db info` for complete project health check
- Use `swap list --project` to inspect other projects without changing directories

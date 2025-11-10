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

Common options available across commands:

- `--help, -h` - Show help for a command
- `--version` - Show CLI version

## Next Steps

- [swap new](./new) - Create new projects from templates
- [swap generate htmx-shell](./generate-htmx-shell) - Add HTMX shell middleware
- [swap events](./events) - Event system commands

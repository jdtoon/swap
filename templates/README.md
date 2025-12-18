# Swap.Htmx Templates

[![NuGet](https://img.shields.io/nuget/v/Swap.Templates.svg)](https://www.nuget.org/packages/Swap.Templates)

This package contains project templates for building [Swap.Htmx](https://github.com/jdtoon/swap) applications.

## Installation

Install the templates from NuGet:

```bash
dotnet new install Swap.Templates
```

## Quick Start (Recommended)

The **Modular Monolith template** is the best way to start a new Swap.Htmx project. It provides a well-structured foundation that scales from prototypes to production applications.

```bash
dotnet new swap-modular -n MyApp
cd MyApp/src
libman restore
dotnet run
```

Open `https://localhost:5001` and you have a working application with:
- Clean sidebar navigation with SPA-style HTMX routing
- Sample Notes module demonstrating full CRUD operations
- Modular architecture ready for adding your own features
- Docker support for easy deployment

## Available Templates

| Template Name | Short Name | Description |
|--------------|------------|-------------|
| **Swap.Htmx Modular Monolith** | `swap-modular` | **Recommended.** Full-featured modular monolith with SQLite, EF Core, Docker, and a sample Notes module. |
| **Swap.Htmx MVC App** | `swap-mvc` | A clean, minimal MVC project with Swap.Htmx pre-configured. |

## Usage

### Create a Modular Monolith Project (Recommended)

```bash
dotnet new swap-modular -n MyApp
cd MyApp/src
libman restore
dotnet run
```

### Create a Minimal MVC Project

```bash
dotnet new swap-mvc -n MyProject
cd MyProject
libman restore
dotnet run
```

Notes:
- `libman restore` is required to download client libraries (htmx and, if enabled, the htmx SSE extension).
- If you enable `--IncludeSse`, the template also wires up the server-side SSE event bridge.

## Template Options

### swap-modular

| Option | Description | Default |
|--------|-------------|---------|
| `--IncludeSse` | Include SSE real-time support. | `false` |
| `--SkipSampleModule` | Skip the sample Notes module. | `false` |
| `--UseLocalRef` | Use local project references (for Swap development). | `false` |

### swap-mvc

| Option | Description | Default |
|--------|-------------|---------|
| `--IncludeSse` | Include HTMX SSE extension for real-time updates. | `false` |
| `--UseLocalRef` | Use local project references (for Swap development). | `false` |

### Examples

```bash
# Modular monolith with SSE real-time support
dotnet new swap-modular -n RealtimeApp --IncludeSse

# Modular monolith without sample module (clean slate)
dotnet new swap-modular -n CleanStart --SkipSampleModule

# Minimal MVC with SSE
dotnet new swap-mvc -n SimpleApp --IncludeSse
```

## What's Included?

### swap-modular (Recommended)

A production-ready foundation with best practices built in:

**Architecture**
- `Modules/` — Self-contained feature modules with their own Controllers, Models, Views, and Events
- `Infrastructure/` — Cross-cutting concerns (compression, data protection, health checks)
- `Data/` — EF Core DbContext with audit fields and configurations

**Features**
- **`<swap-nav>` Tag Helper** — Clean navigation links without verbose HTMX attributes
- **Auto-Layout Suppression** — HTMX requests get partials, browser requests get full pages
- **Zero-Config Source Generators** — Type-safe `SwapViews` and `SwapElements` auto-generated (no `.csproj` setup needed)
- **SPA-style Navigation** — HTMX-powered routing with URL updates
- **SQLite Database** — Ready for development, easily swap for production DB
- **WebOptimizer** — CSS/JS bundling and minification
- **Docker Support** — Multi-stage Dockerfile and docker-compose.yml
- **Health Checks** — `/health` endpoint for container orchestration

**Sample Module**
- Full CRUD operations (Create, Read, Update, Delete)
- Modal dialogs for forms
- Toast notifications on actions
- Event-driven UI updates
- Demonstrates all core Swap.Htmx patterns

**Testing**
- xUnit test project included
- WebApplicationFactory for integration tests
- Example tests for the sample module

### swap-mvc (Minimal)
- Basic MVC setup with Swap.Htmx
- LibMan for HTMX
- Source generators for type-safe events
- Clean CSS foundation

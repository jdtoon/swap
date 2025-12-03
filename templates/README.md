# Swap.Htmx Templates

[![NuGet](https://img.shields.io/nuget/v/Swap.Templates.svg)](https://www.nuget.org/packages/Swap.Templates)

This package contains project templates for building [Swap.Htmx](https://github.com/jdtoon/swap) applications.

## Installation

Install the templates from NuGet:

```bash
dotnet new install Swap.Templates
```

## Available Templates

| Template Name | Short Name | Description |
|--------------|------------|-------------|
| **Swap.Htmx MVC App** | `swap-mvc` | A clean, minimal MVC project with Swap.Htmx pre-configured. |
| **Swap.Htmx Modular Monolith** | `swap-modular` | A full-featured modular monolith with SQLite, EF Core, Docker, and a sample Notes module. |

## Usage

### Create a Minimal MVC Project

```bash
dotnet new swap-mvc -n MyProject
```

### Create a Modular Monolith Project

```bash
dotnet new swap-modular -n MyApp
cd MyApp/src
libman restore
dotnet ef migrations add InitialCreate
dotnet run
```

## Template Options

### swap-mvc

| Option | Description | Default |
|--------|-------------|---------|
| `--IncludeSse` | Include HTMX SSE extension for real-time updates. | `false` |
| `--UseLocalRef` | Use local project references (for Swap development). | `false` |

### swap-modular

| Option | Description | Default |
|--------|-------------|---------|
| `--IncludeSse` | Include SSE real-time support. | `false` |
| `--SkipSampleModule` | Skip the sample Notes module. | `false` |
| `--UseLocalRef` | Use local project references (for Swap development). | `false` |

### Examples

```bash
# Minimal MVC with SSE
dotnet new swap-mvc -n RealtimeApp --IncludeSse

# Modular monolith without sample module
dotnet new swap-modular -n CleanStart --SkipSampleModule

# Modular monolith with SSE
dotnet new swap-modular -n RealtimeApp --IncludeSse
```

## What's Included?

### swap-mvc (Minimal)
- Basic MVC setup with Swap.Htmx
- LibMan for HTMX
- Source generators for type-safe events
- Clean CSS foundation

### swap-modular (Full-Featured)
- **Modular Architecture**: Self-contained feature modules in `Modules/` folder
- **SQLite + EF Core**: Database with migrations and audit fields
- **Infrastructure Layer**: Compression, data protection, health checks
- **WebOptimizer**: CSS/JS bundling and minification
- **Docker Support**: Dockerfile and docker-compose.yml
- **Sample Notes Module**: Full CRUD demonstrating the architecture
- **Integration Tests**: xUnit test project with WebApplicationFactory

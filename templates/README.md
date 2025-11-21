# Swap.Htmx Templates

This package contains project templates for building [Swap.Htmx](https://github.com/jdtoon/swap) applications.

## Installation

Install the templates from NuGet:

```bash
dotnet new install Swap.Templates
```

## Available Templates

| Template Name | Short Name | Description |
|--------------|------------|-------------|
| **Swap.Htmx MVC App** | `swap-mvc` | A clean, "batteries-included" MVC project with Swap.Htmx pre-configured. |

## Usage

### Create a new MVC Project

```bash
dotnet new swap-mvc -n MyProject
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--IncludeSse` | Include HTMX SSE extension and configuration for real-time updates. | `false` |
| `--UseLocalRef` | Use local project references (for development of Swap itself). | `false` |

### Example with SSE

```bash
dotnet new swap-mvc -n RealtimeApp --IncludeSse
```

## What's Included?

The `swap-mvc` template provides a production-ready starting point:

- **Clean Architecture**: No heavy CSS frameworks (Bootstrap/Tailwind) by default—just clean, custom CSS variables.
- **LibMan Support**: Client-side libraries (HTMX) are managed via `libman.json`.
- **Source Generators**: `[SwapEventSource]` is pre-configured for type-safe events.
- **Best Practices**:
  - Uses `this.SwapView()` and `this.SwapResponse()` extension methods.
  - Includes a `_Message.cshtml` partial for demonstrating partial updates.
  - Structured `Events/AppEvents.cs` for centralized event management.

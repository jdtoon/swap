---
sidebar_position: 1
slug: /
---

# Swap

Build modern, interactive ASP.NET Core apps with HTMX—fast, modular, and testable. No JavaScript frameworks required.

## What is Swap?

Swap has two parts that work together:

- The framework (`Swap.*` packages) — three pillars:
    - `Swap.Htmx`: HTMX ergonomics + event system
    - `Swap.Modularity`: lightweight module system for modular monoliths
    - `Swap.Testing`: fluent, HTMX-aware integration testing
- The CLI (`swap`) that scaffolds complete projects and patterns powered by the framework.

It generates ASP.NET Core projects with HTMX-powered views using proven patterns from real applications. You write business logic; Swap wires the rest.

## Templates

Swap ships with three first-class templates (HTMX-native and event-driven):

- Monolith (single project) — optimized DX, move fast without ceremony
- Layered (Web, Application, Domain, Infrastructure) — clean architecture for teams and long-lived apps
- Modular Monolith (host + modules) — recommended for teams that want clear boundaries within a single deployable

Start here to choose: [Templates](./templates/overview)

**Core Features:**

- **Generate complete projects** with `swap new` — Full ASP.NET Core + HTMX stack (includes the event system)
- **Modular monolith support** — Compose modules deterministically with `Swap.Modularity`
- **Scaffold models** with custom fields and 11 data types
- **Create CRUD controllers** with modals, pagination, sorting, and filtering
- **DaisyUI + Tailwind CSS** for modern, accessible UI components
- **Entity Framework Core** integration included
- **Docker-ready** - Every project includes Dockerfile and docker-compose.yml
- **Event System** - Server-driven, filtered events with chain resolution (zero wasted triggers)
- **Generate integration tests** with `Swap.Testing`

## Event System at a glance

Generated apps include the Swap event system out of the box. Components declare UI listeners in `hx-trigger`, the browser sends active UI subscriptions via `X-Swap-Events`, and the server only emits events that have listeners.

Configure once in Program.cs:

```csharp
builder.Services.AddSwapHtmx(events =>
{
    events.Chain(SwapEvents.Entity.Created("todo"),
                 SwapEvents.UI.RefreshList,
                 SwapEvents.UI.ShowToast);
});
app.UseSwapHtmx();
```

Emit domain events from controllers:

```csharp
await _events.EmitAsync(SwapEvents.Entity.Created("todo"), new { id = 123 });
```

Declare UI listeners in markup:

```html
<div id="todo-list" hx-get="/Todo/List" hx-trigger="load, ui.refreshList from:body" hx-swap="outerHTML"></div>
```

## Quick Start

Install the CLI globally:

```bash
dotnet tool install --global Swap.CLI
```

Create a new project:

```bash
swap new MyApp
cd MyApp
dotnet ef database update
dotnet run
```

Generate a controller with custom fields:

```bash
swap g controller Product --fields "Name:string Price:decimal InStock:bool:f"
```

Navigate to `http://localhost:5000/Product` to see your working CRUD interface with pagination, search, sorting, and filtering.

**Note:** Swap automatically creates migrations after generating controllers. The CLI builds your project first to verify compilation before creating the migration.

## Why HTMX?

HTMX lets you build modern, interactive web applications without complex JavaScript frameworks. The CLI generates views with:

- **Server-rendered HTML** - No JSON APIs, just HTML over the wire
- **HTMX attributes** for dynamic updates without page reloads
- **Partial views** for AJAX responses
- **DaisyUI components** for beautiful, accessible UI
- **Tailwind CSS** utilities for rapid styling

**Generated patterns include:**
- Modal CRUD operations
- Real-time search with debouncing
- Pagination with page size selection
- Sortable columns with visual indicators
- Boolean filtering with dropdowns
- Toast notifications for user feedback

Example generated view:

```html
<div hx-get="/Product/List" hx-trigger="load" hx-target="#product-list">
    <div id="product-list">Loading...</div>
</div>
```

## Commands

- [swap new](./cli/new) - Create new projects
- [swap generate model](./cli/generate-model) - Generate entity models
- [swap generate controller](./cli/generate-controller) - Generate CRUD controllers with HTMX views
- [swap generate pattern](./cli/generate-pattern) - Apply entity patterns (soft delete, auditable, etc.)
- [swap generate auth](./cli/generate-auth) - Scaffold ASP.NET Identity authentication
- [swap generate seed](./cli/seeders) - Generate database seeders
- [swap generate factory](./cli/generate-factory) - Generate test data factories
- [swap generate test](./cli/generate-test) - Generate integration tests
- [swap db](./cli/database) - Database workflow commands

## Next Steps

- [Installation](./getting-started/installation) - Set up Swap CLI
- [Your First Project](./getting-started/first-project) - Build a simple CRUD app
- [CLI Reference](./cli/overview) - Complete command documentation
- [Entity Patterns](./features/patterns) - Soft delete, auditable, sluggable, and more

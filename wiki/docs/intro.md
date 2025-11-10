---
sidebar_position: 1
slug: /
---

# Swap

Build modern, interactive ASP.NET Core apps with HTMX—fast, modular, and testable. No JavaScript frameworks required.

## What is Swap?

Swap is an HTMX-first framework for ASP.NET Core built on three core libraries:

- **`Swap.Htmx`**: HTMX ergonomics + declarative event system
- **`Swap.Modularity`**: Lightweight module system for modular monoliths
- **`Swap.Testing`**: Fluent, HTMX-aware integration testing

The framework includes project templates to get started quickly with proven patterns.

## Templates

Swap ships with three first-class templates (HTMX-native and event-driven):

- **Monolith** (single project) — Optimized DX, move fast without ceremony
- **Layered** (Web, Application, Domain, Infrastructure) — Clean architecture for teams and long-lived apps
- **Modular Monolith** (host + modules) — Recommended for teams that want clear boundaries within a single deployable

Start here to choose: [Templates](./templates/overview)

## Core Features

### Swap.Htmx

- **SwapController base class** — Automatic partial/full-page detection with `IsHtmxRequest` and `IsHtmxPartial`
- **Declarative event system** — Server-driven events with chain resolution and smart filtering
- **Fluent HTMX headers** — Type-safe API for `HX-Trigger`, `HX-Retarget`, `HX-Reswap`, etc.
- **Toast notifications** — Built-in support via `SwapToast()` extension methods

### Swap.Modularity

- **IModule contract** — Define modules with dependencies and service registration
- **Automatic discovery** — Finds and loads modules at startup
- **Dependency ordering** — Deterministic initialization based on module dependencies
- **Modular monolith support** — Clear boundaries within a single deployable

### Swap.Testing

- **HtmxTestClient** — HTMX-aware test client extending WebApplicationFactory
- **Fluent assertions** — Assert on HTMX headers, DOM elements, and response content
- **Snapshot testing** — Verify HTML output with built-in scrubbers for dynamic content

## Event System at a glance

All templates include the Swap event system. Components declare UI listeners in `hx-trigger`, the browser sends active subscriptions via `X-Swap-Events`, and the server only emits events that have listeners.

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

Navigate to `http://localhost:5000` to see your HTMX-first ASP.NET Core application.

## Why HTMX?

HTMX lets you build modern, interactive web applications without complex JavaScript frameworks:

- **Server-rendered HTML** - No JSON APIs, just HTML over the wire
- **HTMX attributes** for dynamic updates without page reloads
- **Partial views** for AJAX responses
- **DaisyUI components** for beautiful, accessible UI
- **Tailwind CSS** utilities for rapid styling

Example HTMX pattern:

```html
<div hx-get="/Todo/List" hx-trigger="load" hx-target="#todo-list">
    <div id="todo-list">Loading...</div>
</div>
```

## Commands

- [swap new](./cli/new) - Create new projects from templates
- [swap generate htmx-shell](./cli/generate-htmx-shell) - Add HTMX shell middleware
- [swap events](./cli/events) - List registered event chains

## Next Steps

- [Installation](./getting-started/installation) - Set up Swap CLI
- [Your First Project](./getting-started/first-project) - Build your first HTMX app
- [CLI Reference](./cli/overview) - Complete command documentation

## Comprehensive Guides

For deeper dives into the framework, check out these comprehensive guides in the main repository:

- **[PRODUCT.md](https://github.com/jdtoon/swap/blob/main/docs/PRODUCT.md)** — Complete framework overview: what is Swap, the three pillars, key features, and why use it
- **[EVENTS.md](https://github.com/jdtoon/swap/blob/main/docs/EVENTS.md)** — Full event system guide: chain resolution modes, UI vs Server Events, RabbitMQ integration, testing
- **[TEMPLATES.md](https://github.com/jdtoon/swap/blob/main/docs/TEMPLATES.md)** — Template comparison, detailed structures, migration paths, and choosing the right template
- **[SECURITY.md](https://github.com/jdtoon/swap/blob/main/SECURITY.md)** — Security best practices for production deployments

# Modular Monolith – Architecture

This demo shows a clean module-first architecture:

- Modules own:
  - Contracts (shared types/interfaces)
  - Services and persistence (DbContext, providers, migrations)
  - UI (Razor Class Library in `<Module>.Web`)
  - UI chains via ISwapUiChainContributor (HTMX event system)
- Host (Web project) owns:
  - MVC shell, routing, static assets
  - Event system runtime (Swap.Htmx) and server event registrar
  - Module discovery and orchestration

## Composition

- Auto-discovery: the host calls `AddSwapModules(configuration)`. Since the host references module projects, their assemblies are already loaded and discovered.
- Endpoints: `MapSwapModuleEndpoints()` invokes each module’s `ConfigureEndpoints`.
- UI discovery: `AddSwapModuleApplicationParts()` scans loaded assemblies ending with `.Web` and adds them as MVC ApplicationParts so controllers/views are found without host wiring.
- UI chains: The host applies all discovered `ISwapUiChainContributor` implementations once at startup to configure Swap.Htmx event chains.

## Persistence pattern

- Each module provides `Add<Module>Persistence(IConfiguration)` to register its DbContext and choose a provider based on `Data:Provider` and `ConnectionStrings:<Module>`.
- Database initialization is handled by a module-hosted service:
  - Sqlite: `EnsureCreated()`
  - Postgres/SqlServer: `Migrate()` if migrations exist; else fall back to `EnsureCreated()` (dev-friendly)

## Event system (HTMX)

- Configure Swap.Htmx in the host once: `builder.Services.AddSwapHtmx(); app.UseSwapHtmxShell(); app.UseSwapHtmx()`.
- Modules contribute UI chains via `ISwapUiChainContributor` found in their `.Web` assemblies.
- Optionally map dev endpoints in Development: `/_swap/dev/events`.

## Architecture rules (must follow)

- Contracts-only dependencies across modules
  - A module may depend on another module’s `*.Contracts` only.
  - Never reference another module’s `*.Module` or `*.Web` from your module. This keeps boundaries clean and prevents tight coupling.
- Events: domain/server vs UI
  - Domain/server events are published on the server via `IEventChainRegistrar`. Use these for cross-module reactions and backend policies.
  - UI events (Swap.Htmx) are best-effort client triggers for partial updates. They should not be used for cross-module business logic.
  - Pattern: after a domain action, emit both:
    - `_bus.Emit(TodoEvents.Domain.Created, ...)` for UI chains
    - `await _events.PublishAsync(TodoEvents.Domain.Created, payload, services)` for server chains
- Persistence is owned by the module
  - Each module defines its own DbContext, provider selection, migrations, and initialization.

These rules are enforced in this repo’s demo by making `Demo` depend only on `Todos.Contracts`, while reacting to `Todos` domain events via server event chains.

## Projects overview

- Web (host): MVC shell, module discovery, event system runtime
- Modules:
  - `<Name>.Contracts` – shared types/interfaces
  - `<Name>.Module` – services, DbContext, hosted init, provider selection
  - `<Name>.Web` – UI (controllers/views), UI chain contributor
  - `<Name>.Migrations.<Provider>` – provider-specific migrations shim (design-time factories)
  - `<Name>.Tests` – unit + integration tests

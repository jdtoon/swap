# Modules – How to build one

A module is a small unit that plugs into the host. Minimal shape:

- `<Name>.Contracts` – shared types/interfaces
- `<Name>.Module` – services, persistence, hosted init
- `<Name>.Web` – UI (controllers/views) + UI chains contributor
- Optional: `<Name>.Migrations.<Provider>` – design-time migrations shim

## Contracts

- Keep types simple and stable. Prefer Dto/Query shapes that other modules may consume.

## Services & persistence

- Provide an entry-point extension like `Add<Module>Persistence(IConfiguration)` that:
  - Picks provider based on `Data:Provider`
  - Uses `ConnectionStrings:<Module>`
  - Points migrations assembly for providers with shims
- Register the module service(s)
- Add a hosted service to initialize the database at startup:
  - Sqlite: `EnsureCreated()`
  - Postgres/SQL Server: `Migrate()` if migrations exist else `EnsureCreated()` (dev-friendly)

## UI (RCL) & UI chains

- Place MVC controllers/views in `<Name>.Web` (RCL)
- Implement `ISwapUiChainContributor` in `<Name>.Web/Infrastructure` and configure your UI chains there
- The host auto-discovers `.Web` assemblies and contributors, no host wiring needed

## Endpoints

- If using MVC: let the host’s default route handle it via ApplicationParts (typical)
- If using minimal endpoints: map them in `IModule.ConfigureEndpoints(IEndpointRouteBuilder)`

## Migrations per provider

- Add shim projects for providers needing migrations (e.g., Postgres/SQLServer)
- Include EFCore.Design and the provider package, plus a design-time DbContext factory
- Generate migrations into the shim project

See `docs/DATABASE-MIGRATIONS.md` for exact commands.

## Domain/server events

- Emit domain events after state changes so other modules can react server-side:
  - From controllers/services in your module, call `await IEventChainRegistrar.PublishAsync(eventKey, payload, services)`.
  - Also emit the corresponding UI event via Swap.Htmx if the UI needs to update.
- React to other modules’ events in your module’s `ConfigureEventChains(IEventChainRegistrar registrar)` by registering handlers.
- Keep cross-module logic in these server-side handlers rather than tight compile-time references.

### Tip: HTMX-only one-shot refresh for distributed mode

When using the RabbitMQ transport, server-event handlers run after the HTTP response, so cross-module panels may lag briefly. You can smooth this with a pure HTMX pattern (no custom JS) by adding a delayed retrigger alongside the immediate one:

```html
<div hx-get="/Demo/ActivityLog"
  hx-trigger="load, ui.activity.append from:body, ui.activity.append from:body delay:400ms"
  hx-target="this"
  hx-swap="innerHTML"></div>
```

This refreshes immediately and once again after ~400ms—usually enough for the distributed handler to complete. In single-process (InMemory) mode, the second refresh is redundant but harmless. See `docs/SERVER-EVENTS.md` for details.

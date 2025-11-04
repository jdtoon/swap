# Modular Monolith Template — Architecture & Plan

This document describes the architecture, defaults, and tooling plan for Swap’s Modular Monolith template (swap-modular). It builds on our monolith and layered templates, keeping the DX high while enabling clean boundaries, event-driven flows, and a smooth extraction path to services.

Status: Design approved; implementation targeted for v0.3.2

## Objectives

- Clear module boundaries with minimal shared coupling
- Event-driven first: declarative chains, typed events, visualizable flows
- Per-module testability (unit + integration) and seeded data where useful
- Shared database by default with one DbContext per module; easy to switch to per-module DBs later
- Contracts-first cross-module communication; easy extraction to microservices via an event bus abstraction
- Strong tooling support: validate modules, events, graphs, migrations, dependencies

## Solution layout

```
MyApp/
├─ MyApp.sln
├─ src/
│  ├─ Web/                          # Host, composition root, shared layout
│  ├─ Modules/
│  │  ├─ Orders/
│  │  │  ├─ Orders.Domain/
│  │  │  ├─ Orders.Application/
│  │  │  ├─ Orders.Infrastructure/
│  │  │  ├─ Orders.Web/            # Optional Razor Area/controllers/views (UI surface)
│  │  │  ├─ Orders.Contracts/      # Public events + DTOs (no infra)
│  │  │  └─ Orders.Module/         # Composition: DI, routes, event chains, depends-on
│  │  └─ Inventory/
│  │     └─ ...
└─ test/
   └─ Modules/
      ├─ Orders/
      │  ├─ Orders.UnitTests/
      │  └─ Orders.IntegrationTests/
      └─ Inventory/
         └─ ...
```

Notes:
- Each module owns its Domain, Application, Infrastructure; UI surface is optional per module via `*.Web` (Razor Area)
- `*.Contracts` is the only cross-module reference allowed (public DTOs/events)
- `*.Module` contains the module’s composition root and metadata (DI, routes, event chains, dependencies)

## Module contract and composition

Each module implements a small contract to plug into the host. The host discovers modules, sorts them by dependency, then configures services, routes, and event chains.

```csharp
// src/Framework/Modularity/IModule.cs (template-owned)
public interface IModule
{
    string Name { get; }
    IReadOnlyList<string> DependsOn { get; }

    void ConfigureServices(IServiceCollection services, IConfiguration config);
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints); // or IMvcBuilder for MVC
    void ConfigureEventChains(ISwapEventRegistry registry);
}
```

Discovery & ordering:
- The host scans `src/Modules/**/**.Module.dll`
- Reads `IModule.DependsOn`
- Validates no cycles and sorts topologically
- Invokes ConfigureServices → ConfigureEndpoints → ConfigureEventChains

## Event system design

Defaults (confirmed):
- Event key: `Module.EventName` (e.g., `Orders.OrderCreated`)
- Typed payloads defined in `*.Contracts` (e.g., `record OrderCreated(Guid OrderId, decimal Total)`)
- Internal events remain inside a module (not in Contracts); public events live in Contracts
- Chain declarations live in `*.Module` (e.g., `Events/OrdersEventChains.cs`) and may subscribe to other modules’ public events by referencing their `*.Contracts`

Enforcement & tooling:
- CLI ensures event key uniqueness across all modules
- CLI prevents referencing other modules’ internal types; only `*.Contracts` allowed
- Dev dashboard shows per-module and cross-module graphs, with an Explain view respecting module boundaries

## Data and migrations

Default:
- One database, one DbContext per module (in `*.Infrastructure`), with migrations per context
- No cross-module foreign keys by default; prefer event-based projections

Options:
- Per-module database toggle via module config (future-proof for service extraction)
- Migrations orchestration via CLI across all contexts in dependency order

CLI (draft):
- `swap db migrate --module Orders`
- `swap db update --module Orders`
- `swap db migrate --all && swap db update --all`

## UI and routing

- Shared layout and shell live in `src/Web`
- Each module can expose a Razor Area via `*.Web` (e.g., `Areas/Orders/...`)
- Host composes cross-module pages with HTMX: panels from different modules render into shared shells using partials

## Testing

- Unit tests per module (Domain + Application)
- Integration tests per module using `Swap.Testing` with three boot modes:
  1) Isolated (only module under test)
  2) With dependencies (module + its DependsOn)
  3) Full system
- Optional per-module seeders controlled by env vars; CLI supports `swap db seed --module Orders`

## Event bus abstraction (extraction-ready)

Introduce an event bus interface used by the Event System under the hood:

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(string eventKey, TEvent payload, CancellationToken ct = default);
}
```

- Default implementation: in-process dispatcher (monolith)
- Optional broker adapter: RabbitMQ/other; swapping changes transport only, preserving event keys/payloads
- Extraction path: move a module out-of-process, point `IEventBus` to broker; keep Contracts stable

## Tooling & CLI

Scaffolding:
- `swap new MyApp --template swap-modular` (coming)
- `swap module add Orders [--with-ui]` → creates `*.Domain`, `*.Application`, `*.Infrastructure`, `*.Contracts`, `*.Module`, optional `*.Web`, and tests
- `swap module remove Orders` (safety checks, dependency guard)

Modules:
- `swap modules list`
- `swap modules validate` (no cycles, dependencies resolved, enforce Contracts-only across modules)

Events:
- `swap events list --module Orders`
- `swap events validate --solution` (unique names, internal-use checks)
- `swap events graph --modules all --format mermaid|dot`

Database:
- `swap db migrate --module <Name>` / `--all`
- `swap db update --module <Name>` / `--all`

## Constraints and guards

- Only `*.Contracts` may be referenced across modules
- Disallow cross-module foreign keys by default (explicit waiver required)
- Detect and fail on event key collisions
- Detect and fail on module dependency cycles

## swap-config.json (future work)

Value: lightweight, optional project manifest for the CLI to speed up validation and keep decisions explicit.

Proposed shape:
```json
{
  "template": "swap-modular",
  "modules": [
    {
      "name": "Orders",
      "dependsOn": ["Inventory"],
      "db": {
        "provider": "sqlite",
        "connectionStringName": "DefaultConnection",
        "separateDatabase": false
      },
      "events": {
        "public": ["Orders.OrderCreated", "Orders.OrderCancelled"],
        "internalPrefix": "Orders._"
      }
    }
  ],
  "rules": {
    "allowCrossModuleFK": false
  }
}
```

Notes:
- Mirrors source-of-truth but enables fast CLI checks and guidance
- Kept minimal; source scanning remains the authority

## Defaults (confirmed)

- DB strategy: Shared DB with one DbContext per module (default). Per-module DB is optional and aligns with service extraction.
- Contracts: One `*.Contracts` project per module; no infra dependencies.
- Event naming: `Module.EventName` string keys; typed payloads in `*.Contracts`.
- UI: Optional `*.Web` per module using Razor Areas.
- Cross-module FKs: Disallowed by default; explicit waiver required.
- Bus abstraction: Ship in-process; have an interface and hooks ready for a broker.

## Success criteria

- Module created in <30s with working UI (optional), events, chains, and tests
- `swap modules validate` and `swap events validate` pass on newly generated systems
- Cross-module event chain linking visualized in dev dashboard
- `swap db migrate --all`/`--module` operate predictably and fast
- A module can be extracted to a service by swapping the bus and moving infra with minimal code changes

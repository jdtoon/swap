---
sidebar_position: 3
---

# Modular Monolith

A single deployable host with clearly bounded modules. Each module owns its contracts, services/endpoints, UI, and database migrations. Built for teams that want modularity without the overhead of microservices.

## Why choose this template?

- Clear module boundaries with deterministic composition and dependency ordering
- Per-module ownership (code + UI + migrations) for autonomy and clean responsibilities
- HTMX-first UI with server-driven events (Swap.Htmx)
- First-class testing (Swap.Testing) across host and modules
- Optional distributed server events via RabbitMQ

## Solution layout

```
MyApp/
├─ MyApp.sln
├─ src/
│  ├─ Web/                         # Host: composition, endpoints, UI shell
│  │  ├─ Controllers/
│  │  ├─ Infrastructure/
│  │  ├─ Views/
│  │  └─ Web.csproj
│  └─ Modules/
│     ├─ Example/
│     │  ├─ Example.Contracts/     # Public contracts & shared types
│     │  ├─ Example.Module/        # IModule implementation (services/endpoints)
│     │  ├─ Example.Web/           # RCL UI (controllers/views)
│     │  ├─ Example.Migrations.SqlServer/
│     │  └─ Example.Migrations.Postgres/
│     └─ ...
└─ tests/
   ├─ MyApp.UnitTests/
   └─ MyApp.IntegrationTests/
```

## Quickstart

```bash
swap new MyApp --template swap-modular-monolith
cd MyApp

# Optional: bring up infra (Postgres + RabbitMQ)
docker-compose up -d

# Run the host
dotnet run --project src/Web/Web.csproj
```

For framework developers using local packages:

```bash
swap new MyApp --template swap-modular-monolith --local-nuget
```

## Module model (Swap.Modularity)

The host discovers `IModule` implementations, validates dependencies, and composes modules in topological order.

- `AddSwapModules(configuration)` — discovers modules, validates, and calls `ConfigureServices` per module
- `MapSwapModuleEndpoints()` — calls `ConfigureEndpoints` per module (ordered)
- `AddSwapModuleApplicationParts()` — auto-loads any loaded `*.Web` RCLs for MVC discovery

Guardrails:
- Missing dependencies and cycles throw clear exceptions
- UI chain contributors (`ISwapUiChainContributor`) discovered via reflection when present

## Per-module migrations

Each module owns its migrations in provider-specific projects:

- `<Name>.Migrations.SqlServer`
- `<Name>.Migrations.Postgres`

Design-time factories are included to generate migrations without coupling to the host. Apply migrations at runtime or via EF tooling as needed.

## Server-driven events (Swap.Htmx)

Modules can emit domain/UI events during request handling; chains map domain events to UI reactions.

- Configure event chains once in startup
- Merge to `HX-Trigger` before the response starts; safe merge with existing headers
- Dev endpoints in Development:
  - `/_swap/dev/events` — dashboard + Mermaid graph
  - `/_swap/dev/events.json` — chains JSON
  - `/_swap/dev/explain.json?event=...` — resolution preview

### Distributed server events (optional)

Choose transport via configuration and wire with a single call:

```csharp
builder.Services.AddSwapServerEventChainsFromConfiguration(
    builder.Configuration,
    "Swap:ServerEvents" // picks in-memory or RabbitMQ based on config
);
```

## Packages vs project references

This template uses NuGet packages (`Swap.Htmx`, `Swap.Modularity`, `Swap.Testing`) instead of project references to keep module boundaries clean and upgrades simple.

## Docker

`docker-compose.yml` includes Postgres and RabbitMQ for local development. Bring them up with:

```bash
docker-compose up -d
```

## Next steps

- Add your own module under `src/Modules/`
- Add chains/UI listeners to connect domain actions to UX
- Write integration tests with `Swap.Testing`

See also: /docs/features/event-system and /docs/features/testing-framework

# Modular Monolith Demo

A production-friendly, module-first MVC + HTMX app using Swap.Modularity and Swap.Htmx.

- Host: `src/Web` – MVC shell, event system runtime, module discovery
- Modules:
	- `Todos` – Contracts, Module (services + persistence), Web (RCL UI), provider shims for migrations
	- `Demo` – Contracts, Module, Web (RCL UI)

Read the in-depth docs:
- `docs/ARCHITECTURE.md` – overall design and composition
- `docs/WEB-HOST.md` – host responsibilities and Program.cs wiring
- `docs/MODULES.md` – how to author modules (persistence, UI, chains)
- `docs/DATABASE-MIGRATIONS.md` – per-provider migrations for modules
 - `docs/SERVER-EVENTS.md` – server vs UI events, distributed transport
	- Tip: see “Smoothing eventual consistency (HTMX-only one-shot refresh)” in that doc for a clean UX pattern when using RabbitMQ.

## Prerequisites

- .NET 9 SDK
- Node.js (for Tailwind build)
- (Optional) Docker Desktop (for Postgres stack)

## Run locally (SQLite)

```pwsh
# From repo root
dotnet build demo/ModularMonolithDemo/ModularMonolithDemo.sln
dotnet run --project demo/ModularMonolithDemo/src/Web/Web.csproj
```

Open http://localhost:5000

Notes:
- SQLite schema is created automatically via EnsureCreated().
- HTMX event system is active; dev endpoints are enabled in Development.

## Run with Postgres (Docker)

```pwsh
cd demo/ModularMonolithDemo
docker compose up -d --build --wait
```

Open http://localhost:8080

Notes:
- Compose sets `Data__Provider=Postgres` and applies migrations at startup. If no migrations existed yet, it would fall back to EnsureCreated() for dev convenience.

## Run with RabbitMQ (Docker)

The included compose file also starts RabbitMQ and configures the app to use the distributed server event registrar via RabbitMQ transport.

Services:
- Web app on http://localhost:8080
- RabbitMQ Management UI on http://localhost:15672 (guest/guest)
- RabbitMQ broker on amqp://localhost:5672

Environment keys set for the web service:
- `ServerEvents__Transport=RabbitMq`
- `ServerEvents__RabbitMq__HostName=rabbitmq`
- `ServerEvents__RabbitMq__UserName=guest`
- `ServerEvents__RabbitMq__Password=guest`
- `ServerEvents__RabbitMq__VirtualHost=/`
- `ServerEvents__RabbitMq__ExchangeName=swap.events`

### Diagnostics (Development)

- Check which server-events implementation is active:
	- `GET http://localhost:8080/dev/server-events/info` → shows registrar and transport types.
- Explore UI chains via Swap HTMX dev endpoints (see `docs/WEB-HOST.md`).

## Switch database providers

- Configure in `appsettings.*.json` or env vars:
	- `Data:Provider` = `Sqlite` | `Postgres` | `SqlServer`
	- `ConnectionStrings:Todos` = provider-specific connection
	- `Data:MigrateOnStartup` = `true` to auto-apply migrations

## How modules plug in

- The host calls `AddSwapModules(configuration)` – modules are discovered automatically since the host references them.
- MVC discovers RCLs ending with `.Web` via `AddSwapModuleApplicationParts()`.
- Each module’s `.Web` project can contribute UI chains by implementing `ISwapUiChainContributor`.
- Persistence is module-owned; per-provider migrations live in `<Module>.Migrations.<Provider>`.

## Tests

```pwsh
dotnet test demo/ModularMonolithDemo/ModularMonolithDemo.sln
```

Includes module unit/integration tests and a small host smoke test suite.

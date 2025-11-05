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

# Layered Template Guide

The layered template is a multi-project solution that enforces clear separation of concerns and ships with the HTMX-native event system wired end-to-end.

## When to use it

- You want a clean architecture with distinct Web, Application, Domain, and Infrastructure layers
- You want Swap.Htmx event chains, dev endpoints, session, Tailwind/DaisyUI, and demos pre-wired
- You plan to scale features with testable application services and domain events

## Project layout

```
MyApp/
├─ MyApp.sln
├─ Web/               # Presentation (MVC + HTMX)
├─ Application/       # Use cases/services and abstractions
├─ Domain/            # Entities and domain events
└─ Infrastructure/    # EF Core DbContext and repositories
```

### Web
- Registers Swap.Htmx and the event system chains (`SwapEventChains.Configure(events)`)
- Uses session and dev endpoints in Development
- Contains UI event constants (ui.*) and controllers/views
- Includes an invariant decimal model binder and Tailwind/DaisyUI setup

### Application
- Contains services (Todos, Stats, Components, Notes, Bulk, DemoQueries)
- Emits domain events via `IEventBus`
- No infrastructure dependencies

### Domain
- Entities and domain event constants (single source for names)

### Infrastructure
- EF Core `AppDbContext` and repositories
- Provider-specific packages included (Sqlite/SqlServer/Postgres)

## Create a new layered app

```
swap new MyApp --template layered --database sqlite
```

Aliases: `--template layered` and `--template swap-layered` are equivalent.

## Post-create steps (automated by CLI unless you pass --skip-setup)

Inside `Web/`:
- `npm install`
- `libman restore`
- `npm run build:css`

Migrations from solution root:
- `dotnet ef migrations add InitialCreate -p Infrastructure -s Web`
- `dotnet ef database update -p Infrastructure -s Web`

## Run the app

```
cd MyApp/Web
 dotnet run
```

Then open http://localhost:5000.

## Demos included
- Todos (HTMX-native refresh patterns)
- Stats panel
- Components (refreshable tiles)
- Dynamic (notes/details/summary)
- Bulk operations

## Notes
- In Development, dev endpoints are available via Swap.Htmx to inspect event chains
- Controllers depend only on Application services; no DbContext in Web
- UI communicates via ui.* events; server responds with X-Swap-Events headers

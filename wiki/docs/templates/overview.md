---
sidebar_position: 2
---

# Templates

Swap ships three first-class templates. All are HTMX-native and include the Swap Event System with real integration tests using Swap.Testing.

- Monolith (`swap-monolith`) — Single project optimized for developer experience; move fast without ceremony.
- Layered (`swap-layered`) — Multi-project architecture (Web, Application, Domain, Infrastructure) ready to scale.
- Modular Monolith (`swap-modular-monolith`) — Single deployable with clear module boundaries and per-module ownership.

## Quick compare

| Capability | Monolith | Layered | Modular Monolith |
| --- | --- | --- | --- |
| Project layout | Single Web project under `src/` | Web, Application, Domain, Infrastructure | Host Web + Modules (`Contracts`, `Module`, `Web RCL`) + per-module migrations |
| Event system | Included | Included | Included; module UI chains supported |
| HTMX + DaisyUI | Included | Included | Included |
| EF Core | Included | In Infrastructure | Per-module migrations (SqlServer/Postgres) |
| Tests | `test/Unit`, `test/Integration` | `test/Unit`, `test/Integration` | Unit + Integration per module + host tests |
| Docker | Dockerfile + compose in `src/` | Dockerfile + compose in `src/Web` | Compose with Postgres; RabbitMQ optional |
| Best for | MVPs, prototypes, small/medium apps | Larger teams, long-lived apps | Growing teams needing module boundaries; single deployable |

## Choose a template

```bash
# Monolith (default)
swap new MyApp

# Explicit monolith
swap new MyApp --template swap-monolith

# Layered
swap new MyApp --template swap-layered

# Modular Monolith
swap new MyApp --template swap-modular-monolith
```

Database options:

```bash
# SQLite (default)
swap new MyApp --database sqlite

# SQL Server
swap new MyApp --database sqlserver

# PostgreSQL
swap new MyApp --database postgres
```

## Modular Monolith quickstart

```bash
# Create the modular monolith
swap new MyApp --template swap-modular-monolith

cd MyApp
# Optional: bring up infra (Postgres + RabbitMQ)
docker-compose up -d

# Run the host
dotnet run --project src/Web/Web.csproj
```

## What’s included (both)

- ASP.NET Core 9 + EF Core 9
- HTMX 2.x via LibMan; DaisyUI + Tailwind via npm
- Swap Event System configured in Program.cs
- Sample Todo flow wired to events and HTMX
- Real integration tests using Swap.Testing
- Docker support

## Monolith layout

```
MyApp/
├─ MyApp.sln
├─ src/ (web app)
└─ test/
```

Run (from `src/`):

```bash
npm install
libman restore
npm run build:css

dotnet ef database update

dotnet run
```

## Layered layout

```
MyApp/
├─ MyApp.sln
├─ src/
│  ├─ Web/ Application/ Domain/ Infrastructure/
└─ test/
```

Run:

```bash
# from solution root
npm --prefix src/Web install
libman restore --working-directory src/Web
npm --prefix src/Web run build:css

# migrations
dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Web
dotnet ef database update -p src/Infrastructure -s src/Web

# run
cd src/Web
dotnet run
```

## Modular Monolith layout

```
MyApp/
├── MyApp.sln
├── docker-compose.yml
├── src/
│   ├── MyApp.Host/                      # Host Application
│   │   ├── MyApp.Host.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── wwwroot/
│   └── Modules/
│       ├── Products/
│       │   ├── Products.Contracts/      # Public contracts
│       │   ├── Products/                # Module implementation
│       │   └── Products.Web/            # Razor Class Library (UI)
│       └── Orders/
│           ├── Orders.Contracts/
│           ├── Orders/
│           └── Orders.Web/
└── tests/
    └── MyApp.Tests/
```

Run:

```bash
# Start infrastructure (PostgreSQL, RabbitMQ)
docker-compose up -d

# Run migrations (per module)
dotnet ef database update --project src/Modules/Products/Products --startup-project src/MyApp.Host --context ProductsDbContext
dotnet ef database update --project src/Modules/Orders/Orders --startup-project src/MyApp.Host --context OrdersDbContext

# Run host
dotnet run --project src/MyApp.Host
```

## Testing with Swap.Testing

Both templates generate a working HTMX integration test using Swap.Testing. The library provides an HTMX-aware client and fluent assertions on partials, DOM, and HX headers.

```csharp
public class HtmxSmokeTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;
    public HtmxSmokeTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task Todo_Htmx_Flow_Adds_And_Shows_Item()
    {
        var list = await _client.HtmxGetAsync("/Home/TodoList");
        list.AssertSuccess();
        await list.AssertPartialViewAsync();

        var added = await _client.HtmxPostAsync("/Home/AddTodo", new Dictionary<string,string>
        {
            ["title"] = "HTMX integration test"
        });
        added.AssertSuccess();

        var listAfter = await _client.HtmxGetAsync("/Home/TodoList");
        await listAfter.AssertContainsAsync("HTMX integration test");
    }
}
```

Docs: /docs/features/testing-framework

## Docker

Each template includes Docker support with different configurations:

**Monolith:**
- Dockerfile + docker-compose.yml in `src/`
- Includes optional database service (SQL Server or PostgreSQL)
- Single-container deployment

**Layered:**
- Dockerfile + docker-compose.yml in `src/Web/`
- Multi-stage build with layer-specific compilation
- Includes optional database service

**Modular Monolith:**
- docker-compose.yml at solution root
- PostgreSQL service (required for per-module databases)
- RabbitMQ service (optional, for distributed events)
- Multi-module database support

See [Docker deployment guide](/docs/deployment/docker) for details.

## Event System

Templates wire server-side chains and HTMX UI listeners for you. Inspect and modify chains in your Program.cs and event chain files, and visit `/_swap/dev/events` (Development) to visualize emissions and active chains.

See /docs/features/event-system.

Learn more: /docs/templates/modular-monolith

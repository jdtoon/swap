---
sidebar_position: 2
---

# Templates

Swap ships two first-class templates. Both are HTMX-native and include the Swap Event System and real integration tests using Swap.Testing.

- Monolith (swap-monolith) — Single project optimized for developer experience; move fast without ceremony.
- Layered (swap-layered) — Multi-project architecture (Web, Application, Domain, Infrastructure) ready to scale.

## Quick compare

| Capability | Monolith | Layered |
| --- | --- | --- |
| Project layout | Single web project under `src/` | 4 projects under `src/` |
| Event system | Included | Included |
| HTMX + DaisyUI | Included | Included |
| EF Core | Included | DbContext/Repos in Infrastructure |
| Tests | `test/Unit`, `test/Integration` | `test/Unit`, `test/Integration` |
| Docker | Dockerfile + compose under `src/` | Dockerfile + compose under `src/Web` |
| Best for | MVPs, prototypes, small/medium apps | Larger teams, long-lived apps, clear boundaries |

## Choose a template

```bash
# Monolith (default)
swap new MyApp

# Explicit monolith
swap new MyApp --template swap-monolith

# Layered
swap new MyApp --template swap-layered
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

- Monolith: Dockerfile + docker-compose live in `src/`
- Layered: Dockerfile + docker-compose live in `src/Web`

See /docs/deployment/docker for details.

## Event System

Templates wire server-side chains and HTMX UI listeners for you. Inspect and modify chains in your Program.cs and event chain files, and visit `/_swap/dev/events` (Development) to visualize emissions and active chains.

See /docs/features/event-system.

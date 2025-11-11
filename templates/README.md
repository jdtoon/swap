# Templates

Project templates for Swap CLI. Used by `swap new` to create new applications.

## Available Templates

```
templates/
├── swap-minimal/           # Minimal starter (Bulma, zero-NPM)
├── swap-monolith/          # Single-project app (default)
├── swap-layered/           # Multi-project layered architecture
├── swap-modular-monolith/  # Modular monolith with per-module ownership
└── generate/               # Code generation templates
    └── htmx-shell/         # HTMX shell middleware
```

### swap-minimal ⚡

Minimal ASP.NET Core app with HTMX, Bulma CSS, and toast notifications. Zero NPM dependencies.

```bash
swap new MyApp --template swap-minimal
```

**Includes:** Single controller, toast demo, Bulma 1.0.4 via LibMan, HTMX 2.0.7, SwapController pattern, minimal setup (25-line Program.cs).

**Perfect for:** Learning Swap.Htmx, prototypes, small projects, reference implementation.

### swap-monolith (Default)

Single-project ASP.NET Core app with HTMX, Tailwind CSS, and Swap event system.

```bash
swap new MyApp
```

**Includes:** Program.cs, Controllers, Views, EF Core DbContext, Swap.Htmx event chains, Docker Compose, Tailwind/DaisyUI setup, example CRUD.

**Template variables:** `{{ProjectName}}`, `{{DatabaseProvider}}` (sqlite/sqlserver/postgres), `{{UseLocalNuget}}`

### swap-layered

Multi-project clean architecture: Web, Application, Domain, Infrastructure.

```bash
swap new MyApp --template swap-layered
```

**Includes:** Clean layers, Swap.Htmx integration, provider-specific EF Core, session support, example demos, full test suite.

**Projects:** Web (presentation), Application (services), Domain (entities), Infrastructure (EF Core).

### swap-modular-monolith ⭐

Single deployable with module boundaries. Each module owns contracts, services, UI, and migrations.

```bash
swap new MyApp --template swap-modular-monolith
```

**Includes:** Host app, independent modules (Contracts/Module/Web RCL), per-module migrations, Swap.Modularity composition, Docker Compose (Postgres/RabbitMQ), distributed events, production-ready structure.

**Structure:** `src/Web/` (host), `src/Modules/<Name>/` (per module), `tests/`, `docs/`

## Template Variables

**Common:**
- `{{ProjectName}}` — project/namespace name
- `{{DatabaseProvider}}` — sqlite, sqlserver, postgres
- `{{UseLocalNuget}}` — true/false for local package feed

**Conditional blocks:**
```
{{#if_sqlite}} ... {{/if_sqlite}}
{{#if_sqlserver}} ... {{/if_sqlserver}}
{{#if_postgres}} ... {{/if_postgres}}
{{#if_local_nuget}} ... {{/if_local_nuget}}
```

Templates use plain C#/Razor with `{{variable}}` placeholders. Processed by `tools/Swap.CLI/Infrastructure/TemplateEngine.cs`.

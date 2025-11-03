---
sidebar_position: 3
---

# Layered Solution Template

The layered template generates a clean, multi-project solution with HTMX-native event system wiring.

## When to use it

- You want clear separation of concerns (Web, Application, Domain, Infrastructure)
- You want Swap.Htmx event chains, dev endpoints, session, and Tailwind/DaisyUI pre-wired
- You plan to scale with testable application services and domain events

## Project layout

```
MyApp/
├─ MyApp.sln
├─ Web/               # Presentation (MVC + HTMX + Swap chains)
├─ Application/       # Use cases/services, abstractions, DTOs
├─ Domain/            # Entities and domain events (single source of names)
└─ Infrastructure/    # EF Core DbContext and repositories
```

## Create a new layered app

```
swap new MyApp --template layered --database sqlite
```

Aliases: `--template layered` and `--template swap-layered` are equivalent.

## Post-create steps (automated unless you pass --skip-setup)

- In `Web/`: `npm install`, `libman restore`, `npm run build:css`
- Migrations at solution root: `dotnet ef migrations add InitialCreate -p Infrastructure -s Web` then `dotnet ef database update -p Infrastructure -s Web`

## Run the app

```
cd MyApp/Web
dotnet run
```

Then open http://localhost:5000.

## What’s included

- HTMX-native ui.* events and server-side Swap event chains
- Session and dev endpoints (Development only)
- Demos: Todos, Stats, Components, Dynamic, Bulk operations
- Invariant decimal model binder registered in MVC options

## Notes

- Controllers depend only on Application services (no DbContext in Web)
- Domain defines event names; Web defines UI event names; Swap chains map domain → ui.*
- The CLI prints correct run instructions for layered (run from Web folder)

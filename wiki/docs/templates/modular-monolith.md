---
sidebar_position: 3
---

# Modular Monolith (Coming Soon)

We’re preparing a modular monolith template that combines the simplicity of a single deployable with well-defined internal module boundaries.

## Goals

- Clear module boundaries (Domain + Application per module)
- Internal HTTP endpoints (or mediator) with compile-time safety between modules
- Shared Infrastructure with per-module persistence migrations
- Event-driven UI via Swap.Htmx across module boundaries
- First-class testing via Swap.Testing (unit + integration per module)

## Planned layout

```
MyApp/
├─ MyApp.sln
├─ src/
│  ├─ Web/                     # Host, routing, composition, UI
│  ├─ Modules/
│  │  ├─ Orders/               # Example module
│  │  │  ├─ Orders.Domain/
│  │  │  ├─ Orders.Application/
│  │  │  └─ Orders.Web/        # Optional UI surface for module
│  │  └─ Inventory/
│  └─ Infrastructure/          # Shared infra (EF, integrations)
└─ test/
   ├─ Orders/
   │  ├─ Orders.UnitTests/
   │  └─ Orders.IntegrationTests/
   └─ Inventory/
      ├─ Inventory.UnitTests/
      └─ Inventory.IntegrationTests/
```

## CLI scaffolding (draft)

We’ll ship module scaffolding alongside the template.

```bash
# Create a new modular monolith (coming soon)
swap new MyApp --template swap-modular

# Add a new module (draft)
swap module add Orders

# Add resources into a module (draft)
swap generate resource Order --module Orders
```

## Status

- Template is in active design and prototyping
- Target release: 0.3.2

Track progress and weigh in on design in the roadmap: /docs/IMPLEMENTATION-ROADMAP.md

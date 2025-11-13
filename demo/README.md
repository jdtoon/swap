# Swap Demo Applications

This folder contains **two comprehensive demo applications** showcasing different architectural patterns and features of the Swap framework.

---

## 🎯 ProjectHub — Modular Monolith Demo

**Location:** [`demo/projecthub/`](./projecthub/)

A production-ready **modular monolith** demonstrating advanced Swap.Modularity and Swap.Htmx patterns. This is a project/task management system built with independent, pluggable modules.

### Architecture

- **3 Independent Modules:** Workspaces, Projects, Tasks
- Each module has: Contracts, Module (services + persistence), Web (RCL UI)
- Clean module boundaries with cross-module communication via contracts
- Per-module EF Core migrations (Postgres + SQLite)
- Docker Compose stack (Postgres + RabbitMQ)
- Integration tests covering HTMX flows

### Key Features Demonstrated

✅ **Swap.Modularity** — Auto-discovery, RCL composition, per-module migrations  
✅ **Server-Sent Events (SSE)** — Live dashboard with real-time metrics streaming  
✅ **Advanced Event System** — Cross-module domain events + UI refresh chains  
✅ **SwapRedirectToAction** — Internal action invocation preserving ViewData  
✅ **Fluent OOB API** — `.WithOobSwap()` for out-of-band updates  
✅ **Production Patterns** — Health checks, Docker, distributed events

### Run It

```bash
cd demo/projecthub
docker compose up -d --build --wait
dotnet run --project src/Web
```

Open **http://localhost:5073** — Full modular monolith with live SSE dashboard!

---

## ⚡ TaskFlow — HTMX Patterns Demo

**Location:** [`demo/taskflow/`](./taskflow/)

A **focused demonstration** of Swap.Htmx patterns and event-driven UI. Simple task management app showcasing the framework's core capabilities.

### Architecture

- **Single monolith** with clean MVC structure
- EF Core with SQLite
- Declarative event chains
- Comprehensive HTMX integration tests

### Key Features Demonstrated

✅ **SwapController Pattern** — Automatic partial vs. full page detection  
✅ **Event Chains** — Declarative domain event → UI refresh mapping  
✅ **Toast Notifications** — Server-triggered client toasts  
✅ **HTMX Integration Tests** — Test your HTMX flows properly  
✅ **Clean CRUD Patterns** — Server-driven forms with validation  
✅ **Enhanced SSE Integration** — Event-driven real-time updates

### Run It

```bash
cd demo/taskflow/src
libman restore
dotnet run
```

Open **http://localhost:5000** — Pure HTMX velocity!

---

## 🤔 Which Demo Should I Explore?

| If you want to see...                  | Check out... |
| -------------------------------------- | ------------ |
| **Modular architecture at scale**      | ProjectHub   |
| **Server-Sent Events (SSE) streaming** | ProjectHub   |
| **Cross-module communication**         | ProjectHub   |
| **Per-module migrations**              | ProjectHub   |
| **Docker/Postgres/RabbitMQ setup**     | ProjectHub   |
| **Core HTMX patterns**                 | TaskFlow     |
| **Event system fundamentals**          | TaskFlow     |
| **Simple, clean starting point**       | TaskFlow     |
| **Integration testing patterns**       | Both         |

---

## 📚 Learn More

Both demos include extensive inline documentation:

- **ProjectHub:** [`demo/projecthub/README.md`](./projecthub/README.md) + architecture docs
- **TaskFlow:** [`demo/taskflow/README.md`](./taskflow/README.md) + pattern explanations

For framework-level docs, see:

- **Main README:** [`README.md`](../README.md)
- **Framework Docs:** [`framework/README.md`](../framework/README.md)
- **Templates:** [`templates/README.md`](../templates/README.md)
- **Wiki:** [`wiki/docs/`](../wiki/docs/)

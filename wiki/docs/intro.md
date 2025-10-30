---
sidebar_position: 1
slug: /
---

# Swap CLI

A command-line tool for generating production-ready ASP.NET Core applications with HTMX.

## What is Swap?

Swap generates ASP.NET Core projects with HTMX-powered views using proven patterns from real production applications. It handles the project setup and boilerplate so you can focus on building features.

**Core Features:**

- **Generate complete projects** with `swap new` - Full ASP.NET Core + HTMX stack
- **Scaffold models** with custom fields and 11 data types
- **Create CRUD controllers** with modals, pagination, sorting, and filtering
- **DaisyUI + Tailwind CSS** for modern, accessible UI components
- **Entity Framework Core** integration included
- **Docker-ready** - Every project includes Dockerfile and docker-compose.yml
- **Pattern-driven** - Every generated file uses battle-tested patterns

## Quick Start

Install the CLI globally:

```bash
dotnet tool install --global Swap.CLI
```

Create a new project:

```bash
swap new MyApp
cd MyApp
dotnet ef database update
dotnet run
```

Generate a controller with custom fields:

```bash
swap g controller Product --fields "Name:string Price:decimal InStock:bool:f"
```

Navigate to `http://localhost:5000/Product` to see your working CRUD interface with pagination, search, sorting, and filtering.

**Note:** Swap automatically creates migrations after generating controllers. The CLI builds your project first to verify compilation before creating the migration.

## Why HTMX?

HTMX lets you build modern, interactive web applications without complex JavaScript frameworks. The CLI generates views with:

- **Server-rendered HTML** - No JSON APIs, just HTML over the wire
- **HTMX attributes** for dynamic updates without page reloads
- **Partial views** for AJAX responses
- **DaisyUI components** for beautiful, accessible UI
- **Tailwind CSS** utilities for rapid styling

**Generated patterns include:**
- Modal CRUD operations
- Real-time search with debouncing
- Pagination with page size selection
- Sortable columns with visual indicators
- Boolean filtering with dropdowns
- Toast notifications for user feedback

Example generated view:

```html
<div hx-get="/Product/List" hx-trigger="load" hx-target="#product-list">
    <div id="product-list">Loading...</div>
</div>
```

## Commands

- [swap new](./cli/new) - Create new projects
- [swap generate model](./cli/generate-model) - Generate entity models
- [swap generate controller](./cli/generate-controller) - Generate CRUD controllers with HTMX views
- [swap generate pattern](./cli/generate-pattern) - Apply entity patterns (soft delete, auditable, etc.)
- [swap generate auth](./cli/generate-auth) - Scaffold ASP.NET Identity authentication
- [swap generate seed](./cli/seeders) - Generate database seeders
- [swap generate factory](./cli/generate-factory) - Generate test data factories
- [swap generate test](./cli/generate-test) - Generate integration tests
- [swap db](./cli/database) - Database workflow commands

## Next Steps

- [Installation](./getting-started/installation) - Set up Swap CLI
- [Your First Project](./getting-started/first-project) - Build a simple CRUD app
- [CLI Reference](./cli/overview) - Complete command documentation
- [Entity Patterns](./features/patterns) - Soft delete, auditable, sluggable, and more

---
sidebar_position: 1
slug: /
---

# Swap CLI

A command-line tool for scaffolding ASP.NET Core applications with HTMX.

## What is Swap?

Swap generates ASP.NET Core projects with HTMX-powered views. It handles the project setup and boilerplate so you can focus on building features.

**Core Features:**

- Generate complete projects with `swap new`
- Scaffold models with custom fields
- Create CRUD controllers with HTMX views
- Entity Framework Core integration included

## Quick Start

Install the CLI globally:

```bash
dotnet tool install -g Swap.CLI
```

Create a new project:

```bash
swap new MyApp
cd MyApp
dotnet run
```

Generate a resource with custom fields:

```bash
swap g r Product --fields Name:string,Price:decimal,Stock:int
dotnet ef migrations add AddProduct
dotnet ef database update
```

Navigate to `http://localhost:5000/Product` to see your working CRUD interface.

## Why HTMX?

HTMX lets you build modern, interactive web applications without writing JavaScript. The CLI generates views with:

- Server-rendered HTML
- HTMX attributes for dynamic updates
- Partial views for AJAX responses
- Bootstrap 5 styling

Example generated view:

```html
<div hx-get="/Product/List" hx-trigger="load" hx-target="#product-list">
    <div id="product-list">Loading...</div>
</div>
```

## Commands

- [swap new](./cli/new) - Create new projects
- [swap generate model](./cli/generate-model) - Generate entity models
- [swap generate controller](./cli/generate-controller) - Generate controllers with HTMX views
- [swap generate resource](./cli/generate-resource) - Generate model + controller together

## Next Steps

- [Installation](./getting-started/installation) - Set up Swap CLI
- [Your First Project](./getting-started/first-project) - Build a simple CRUD app
- [CLI Reference](./cli/overview) - Complete command documentation

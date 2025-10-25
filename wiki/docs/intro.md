---------------

sidebar_position: 1

slug: /sidebar_position: 1

---

slug: /sidebar_position: 1

# Swap CLI

---

A command-line tool for scaffolding ASP.NET Core applications with HTMX.

slug: /sidebar_position: 1sidebar_position: 1

## What is Swap?

# Swap CLI

Swap generates ASP.NET Core projects with HTMX-powered views. It handles the boring stuff so you can focus on building features.

---

**Core Features:**

A command-line tool for scaffolding ASP.NET Core applications with HTMX.

- Generate complete projects with `swap new`

- Scaffold models with custom fieldsslug: /---

- Create CRUD controllers with HTMX views

- Entity Framework Core setup included## What is Swap?



## Quick Start# Welcome to Swap CLI



Install globally:Swap generates ASP.NET Core projects with HTMX-powered views. It handles the boring stuff so you can focus on building features.



```bash---

dotnet tool install -g Swap.CLI

```**Core Features:**



Create a project:- Generate complete projects with `swap new`**The Rails of .NET** - Build modular ASP.NET Core applications faster with our powerful command-line interface.



```bash- Scaffold models with custom fields

swap new MyApp

cd MyApp- Create CRUD controllers with HTMX views# Tutorial Intro

dotnet run

```- Entity Framework Core setup included



Generate a resource:## What is Swap?



```bash## Quick Start

swap g r Product --fields Name:string,Price:decimal,Stock:int

dotnet ef migrations add AddProduct# Welcome to NetMX CLI

dotnet ef database update

```Install globally:



Navigate to `http://localhost:5000/Product` and you have a working CRUD interface.Swap is the command-line tool for NetMX, a comprehensive framework for building modular, enterprise-grade ASP.NET Core applications. It combines:



## Why HTMX?```bash



HTMX lets you build modern, interactive web apps without writing JavaScript. The CLI generates:dotnet tool install -g NetMX.CLILet's discover **Docusaurus in less than 5 minutes**.



- Server-rendered HTML```

- HTMX attributes for dynamic updates

- Partial views for AJAX responses- **Modular Architecture** - Domain-driven design with clean boundaries

- Bootstrap 5 styling

Create a project:

Example generated view:

- **Rapid Scaffolding** - Generate projects, models, controllers, and more**The Rails of .NET** - Build modular ASP.NET Core applications faster with our powerful command-line interface.

```html

<div hx-get="/Product/List" hx-trigger="load" hx-target="#product-list">```bash

    <div id="product-list">Loading...</div>

</div>swap new MyApp- **Built-in Patterns** - Event-driven architecture, CQRS, DDD patterns

```

cd MyApp

## Commands

dotnet run- **Production-Ready** - Identity, authorization, auditing out of the box## Getting Started

- [swap new](./cli/new) - Create new projects

- [swap generate model](./cli/generate-model) - Generate entity models```

- [swap generate controller](./cli/generate-controller) - Generate controllers with HTMX views

- [swap generate resource](./cli/generate-resource) - Generate model + controller together



## Next StepsGenerate a resource:



- [Installation](./getting-started/installation) - Set up Swap CLI## Quick Start## What is NetMX?

- [Your First Project](./getting-started/first-project) - Build a simple CRUD app

- [CLI Reference](./cli/overview) - Complete command documentation```bash


swap g r Product --fields Name:string,Price:decimal,Stock:int

dotnet ef migrations add AddProduct

dotnet ef database updateInstall the CLI globally:Get started by **creating a new site**.

```



Navigate to `http://localhost:5000/Product` and you have a working CRUD interface.

```bashNetMX is a comprehensive framework and CLI toolset for building modular, enterprise-grade ASP.NET Core applications. It combines:

## Why HTMX?

dotnet tool install -g NetMX.CLI

HTMX lets you build modern, interactive web apps without writing JavaScript. The CLI generates:

```Or **try Docusaurus immediately** with **[docusaurus.new](https://docusaurus.new)**.

- Server-rendered HTML

- HTMX attributes for dynamic updates

- Partial views for AJAX responses

- Bootstrap 5 stylingCreate a new project:- **Modular Architecture** - Domain-driven design with clean boundaries



Example generated view:



```html```bash- **Rapid Scaffolding** - Generate projects, models, controllers, and more### What you'll need

<div hx-get="/Product/List" hx-trigger="load" hx-target="#product-list">

    <div id="product-list">Loading...</div>swap new MyApp

</div>

```cd MyApp- **Built-in Patterns** - Event-driven architecture, CQRS, DDD patterns



## Commandsdotnet run



- [swap new](./cli/new) - Create new projects```- **Production-Ready** - Identity, authorization, auditing out of the box- [Node.js](https://nodejs.org/en/download/) version 20.0 or above:

- [swap generate model](./cli/generate-model) - Generate entity models

- [swap generate controller](./cli/generate-controller) - Generate controllers with HTMX views

- [swap generate resource](./cli/generate-resource) - Generate model + controller together

## What Can You Build?  - When installing Node.js, you are recommended to check all checkboxes related to dependencies.

## Next Steps



- [Installation](./getting-started/installation) - Set up Swap CLI

- [Your First Project](./getting-started/first-project) - Build a simple CRUD app- **Modular Applications** - Multi-tenant SaaS platforms with isolated modules## Quick Start

- [CLI Reference](./cli/overview) - Complete command documentation

- **Monolithic Applications** - Traditional layered applications with clean architecture

- **Microservices** - Service-oriented architecture with shared contracts## Generate a new site

- **E-commerce Platforms** - Full-featured shopping systems with inventory, orders, payments

- **Identity Management** - OAuth2/OpenID Connect providersInstall the CLI globally:

- **Content Management** - Headless CMS with API-first design

Generate a new Docusaurus site using the **classic template**.

## Core Features

```bash

### 🚀 Rapid Scaffolding

dotnet tool install -g NetMX.CLIThe classic template will automatically be added to your project after you run the command:

Generate entire project structures in seconds with `swap new`, including:

- ASP.NET Core web projects (modular or monolith)```

- Entity Framework Core with SQLite, SQL Server, or PostgreSQL

- Pre-configured Identity and Authorization```bash

- Docker support for containerized deployment

Create a new project:npm init docusaurus@latest my-website classic

### 🎨 Code Generation

```

Create models, controllers, and views with intelligent defaults:

```bash

```bash

# Generate a model with custom fieldsswap new MyAppYou can type this command into Command Prompt, Powershell, Terminal, or any other integrated terminal of your code editor.

swap generate model Product --fields Name:string,Price:decimal,Stock:int

cd MyApp

# Generate a complete CRUD controller

swap generate controller Productdotnet runThe command also installs all necessary dependencies you need to run Docusaurus.



# Generate a resource (model + controller)```

swap generate resource Order --fields CustomerId:int,Total:decimal

```## Start your site



### 🏗️ Modular Architecture## What Can You Build?



Build applications as a collection of self-contained modules:Run the development server:

- Independent database contexts

- Module-specific migrations- **Modular Applications** - Multi-tenant SaaS platforms with isolated modules

- Cross-module communication via events

- Clean dependency management- **Monolithic Applications** - Traditional layered applications with clean architecture```bash



### 📦 Pre-built Modules- **Microservices** - Service-oriented architecture with shared contractscd my-website



Get started faster with battle-tested modules:- **E-commerce Platforms** - Full-featured shopping systems with inventory, orders, paymentsnpm run start

- **Identity** - User management, authentication, multi-tenancy

- **Authorization** - Role-based and permission-based access control- **Identity Management** - OAuth2/OpenID Connect providers```

- **Audit** - Automatic change tracking and audit logs

- **Content Management** - Headless CMS with API-first design

## Philosophy

The `cd` command changes the directory you're working with. In order to work with your newly created Docusaurus site, you'll need to navigate the terminal there.

Swap follows the "convention over configuration" principle pioneered by Ruby on Rails, adapted for the .NET ecosystem:

## Core Features

1. **Sensible Defaults** - Projects work out of the box with minimal configuration

2. **Easy to Override** - Everything can be customized when you need itThe `npm run start` command builds your website locally and serves it through a development server, ready for you to view at http://localhost:3000/.

3. **Don't Repeat Yourself** - Code generation eliminates boilerplate

4. **Developer Happiness** - Clear patterns and great tooling### 🚀 Rapid Scaffolding



## Next StepsOpen `docs/intro.md` (this page) and edit some lines: the site **reloads automatically** and displays your changes.



- [Installation Guide](./getting-started/installation) - Set up your development environmentGenerate entire project structures in seconds with `swap new`, including:

- [Your First Project](./getting-started/first-project) - Create your first application- ASP.NET Core web projects (modular or monolith)

- [CLI Reference](./cli/overview) - Complete command reference- Entity Framework Core with SQLite, SQL Server, or PostgreSQL

- [Architecture Guide](./architecture/overview) - Understand the modular architecture- Pre-configured Identity and Authorization

- Docker support for containerized deployment

### 🎨 Code Generation

Create models, controllers, and views with intelligent defaults:

```bash
# Generate a model with custom fields
swap generate model Product --fields Name:string,Price:decimal,Stock:int

# Generate a complete CRUD controller
swap generate controller Product

# Generate a resource (model + controller)
swap generate resource Order --fields CustomerId:int,Total:decimal
```

### 🏗️ Modular Architecture

Build applications as a collection of self-contained modules:
- Independent database contexts
- Module-specific migrations
- Cross-module communication via events
- Clean dependency management

### 📦 Pre-built Modules

Get started faster with battle-tested modules:
- **Identity** - User management, authentication, multi-tenancy
- **Authorization** - Role-based and permission-based access control
- **Audit** - Automatic change tracking and audit logs

## Philosophy

NetMX follows the "convention over configuration" principle pioneered by Ruby on Rails, adapted for the .NET ecosystem:

1. **Sensible Defaults** - Projects work out of the box with minimal configuration
2. **Easy to Override** - Everything can be customized when you need it
3. **Don't Repeat Yourself** - Code generation eliminates boilerplate
4. **Developer Happiness** - Clear patterns and great tooling

## Next Steps

- [Installation Guide](./getting-started/installation) - Set up your development environment
- [Your First Project](./getting-started/first-project) - Create your first NetMX application
- [CLI Reference](./cli/overview) - Complete command reference
- [Architecture Guide](./architecture/overview) - Understand the modular architecture

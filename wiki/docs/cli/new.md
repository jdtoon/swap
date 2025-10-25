---
sidebar_position: 2
---

# swap new

Create a new NetMX application from scratch with a complete project structure, database setup, and sample code.

## Synopsis

```bash
swap new <name> [options]
```

## Description

The `swap new` command scaffolds a complete ASP.NET Core application with:

- Project structure (controllers, models, views, data layer)
- Entity Framework Core with your choice of database provider
- Sample entity and CRUD controller
- Application configuration (appsettings.json, Program.cs)
- Docker support (optional)
- README with getting started instructions

## Arguments

### `<name>`

**Required.** The name of the project to create.

- Must be a valid C# identifier (letters, numbers, underscores)
- Will be used as the root namespace
- Creates a folder with this name

Examples:
```bash
swap new BlogApp
swap new ECommerce
swap new MyAwesomeProject
```

## Options

### `--template <type>`

Choose the project template type.

**Values:**
- `monolith` - Traditional layered application (default)
- `modular` - Multi-module application with DDD boundaries
- `minimal` - Minimal API-based application

**Default:** `monolith`

**Examples:**
```bash
swap new MyApp --template monolith
swap new MyModularApp --template modular
swap new MyApi --template minimal
```

### `--database <provider>` (alias: `--db`)

Select the database provider.

**Values:**
- `sqlite` - SQLite (file-based, great for development)
- `sqlserver` - SQL Server / LocalDB
- `postgresql` - PostgreSQL
- `postgres` - Alias for `postgresql`

**Default:** `sqlite`

**Examples:**
```bash
swap new MyApp --database sqlite
swap new MyApp --db sqlserver
swap new MyApp --db postgresql
```

### `--include-docker`

Include Docker and Docker Compose configuration files.

**Examples:**
```bash
swap new MyApp --include-docker
```

Generates:
- `Dockerfile` - Container image definition
- `docker-compose.yml` - Multi-container orchestration
- `.dockerignore` - Files to exclude from Docker context

### `--include-tests`

Generate a companion test project with sample tests.

**Examples:**
```bash
swap new MyApp --include-tests
```

Creates:
```
MyApp/
MyApp.Tests/
  ├── MyApp.Tests.csproj
  ├── UnitTest1.cs
  └── Integration/
```

### `--use-https`

Configure the project to use HTTPS by default.

**Default:** `true`

**Examples:**
```bash
swap new MyApp --use-https false
```

### `--auth <type>`

Include authentication and authorization.

**Values:**
- `none` - No authentication (default)
- `identity` - ASP.NET Core Identity
- `identityserver` - IdentityServer4 integration
- `jwt` - JWT bearer authentication

**Examples:**
```bash
swap new MyApp --auth identity
swap new MyApi --auth jwt
```

## Examples

### Basic Monolith Application

Create a simple application with SQLite:

```bash
swap new BlogApp
cd BlogApp
dotnet run
```

### SQL Server Application

Create an app using SQL Server:

```bash
swap new EnterpriseApp --database sqlserver
```

Update connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnterpriseApp;Trusted_Connection=true"
  }
}
```

### PostgreSQL Application

```bash
swap new DataPlatform --database postgresql
```

### Modular Application

Create a multi-module application:

```bash
swap new ECommerce --template modular --database sqlserver
```

Generated structure:

```
ECommerce/
├── src/
│   ├── ECommerce.Web/              # Web host
│   ├── ECommerce.Core/             # Shared kernel
│   └── modules/
│       ├── Catalog/                # Product catalog module
│       ├── Orders/                 # Order management module
│       └── Identity/               # User management module
├── ECommerce.sln
└── README.md
```

### Full-Featured Application

Create an application with all features:

```bash
swap new MyApp \
  --template modular \
  --database postgresql \
  --auth identity \
  --include-docker \
  --include-tests
```

### API-Only Application

Create a minimal API project:

```bash
swap new MyApi --template minimal --database sqlite
```

## Generated Project Structure

### Monolith Template

```
MyApp/
├── Controllers/
│   ├── HomeController.cs
│   └── TodoController.cs
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Models/
│   └── Todo.cs
├── Views/
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Todo/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Delete.cshtml
│   │   └── Details.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── MyApp.csproj
└── README.md
```

### Modular Template

```
MyApp/
├── src/
│   ├── MyApp.Web/
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── wwwroot/
│   │   ├── Program.cs
│   │   └── MyApp.Web.csproj
│   ├── MyApp.Core/
│   │   ├── Events/
│   │   ├── Interfaces/
│   │   └── MyApp.Core.csproj
│   └── modules/
│       └── Catalog/
│           ├── Controllers/
│           ├── Models/
│           ├── Data/
│           └── Catalog.csproj
├── tests/
│   └── MyApp.Tests/
├── MyApp.sln
└── README.md
```

## Sample Code

The CLI generates a working Todo application:

### Model (`Models/Todo.cs`)

```csharp
namespace MyApp.Models;

public class Todo
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
}
```

### DbContext (`Data/AppDbContext.cs`)

```csharp
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Todo> Todos { get; set; }
}
```

### Controller (`Controllers/TodoController.cs`)

Complete CRUD controller with:
- `Index()` - List all todos
- `Create()` - Show create form (GET) and save (POST)
- `Edit(id)` - Show edit form (GET) and update (POST)
- `Delete(id)` - Show delete confirmation (GET) and remove (POST)
- `Details(id)` - View single todo

### Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using MyApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

## Post-Creation Steps

After creating a project, follow these steps:

### 1. Navigate to Project

```bash
cd MyApp
```

### 2. Create Initial Migration

```bash
dotnet ef migrations add InitialCreate
```

### 3. Apply Migration

```bash
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

Or use the development server:

```bash
dotnet watch run
```

### 5. Open in Browser

Navigate to `https://localhost:5001` (or the URL shown in terminal).

## Common Workflows

### Change Database Provider

After project creation, you can change database providers:

**1. Update Package Reference:**

```bash
# Remove old provider
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite

# Add new provider
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

**2. Update Program.cs:**

```csharp
// Change from:
options.UseSqlite(...)

// To:
options.UseSqlServer(...)
```

**3. Update Connection String:**

Edit `appsettings.json` with the new connection string.

**4. Recreate Migrations:**

```bash
rm -rf Data/Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Add Authentication Later

To add authentication to an existing project:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

Then follow the [Identity Setup Guide](../authentication/identity).

## Troubleshooting

### Port Already in Use

If default ports (5000/5001) are occupied:

```bash
dotnet run --urls "http://localhost:5002;https://localhost:5003"
```

### .NET SDK Not Found

Ensure .NET 9.0+ is installed:

```bash
dotnet --version
```

### Template Not Found

Update the CLI:

```bash
dotnet tool update -g NetMX.CLI
```

### Connection String Errors

Verify your connection string in `appsettings.json` matches your database provider and server setup.

## Next Steps

- [Generate Models](./generate-model) - Add entities to your project
- [Generate Controllers](./generate-controller) - Create CRUD controllers
- [Modular Architecture](../architecture/modular) - Learn about modular apps

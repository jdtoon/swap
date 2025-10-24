# NetMX Monolith Template# NetMX Modular Template



**Single-Project Architecture** for simpler applicationsA starter template for building modular monolith applications with NetMX framework.



## Overview## Features



This template provides a **traditional monolithic application** structure where all features live within a single project. It's ideal for:- **ASP.NET Core 9.0** - Latest .NET stack

- **PostgreSQL Database** - Production-ready relational database

- ✅ Small to medium-sized applications- **Entity Framework Core** - Code-first migrations and LINQ queries

- ✅ Teams preferring simpler project structure- **ASP.NET Core Identity** - Complete authentication and authorization

- ✅ Applications not requiring module reusability- **Docker Support** - Containerized PostgreSQL for development

- ✅ Rapid prototyping and MVP development- **HTMX-First UI** - Server-rendered HTML with HTMX for interactivity

- **Bulma CSS** - Clean, modern UI framework

## Structure- **Modular Architecture** - Clean separation of concerns

- **🎯 HTMX Showcase** - 8+ interactive examples demonstrating HTMX patterns

```

MonolithApp/## 🎯 HTMX Showcase

├── MonolithApp.sln

├── src/This template includes a comprehensive **HTMX Showcase** at `/Demo` with real-world examples:

│   └── MonolithApp.Web/           # All code in one project

│       ├── Features/              # Feature folders (not modules)1. **Click to Edit** - Inline editing without page reload

│       │   ├── Users/2. **Delete with Confirmation** - Surgical DOM updates

│       │   ├── Products/3. **Infinite Scroll** - Auto-load content when scrolling

│       │   └── Orders/4. **Search with Debounce** - Live search with 500ms delay

│       ├── Data/                  # Database context5. **Tab Switching** - Dynamic tab content loading

│       ├── Program.cs6. **Form Validation** - Server-side validation with inline errors

│       └── appsettings.json7. **Out-of-Band Updates** - Update multiple page sections at once

├── nuget.config                   # NuGet sources8. **Lazy Loading** - Load expensive content when visible

└── docker-compose.yml             # Database services

```👉 **See it live**: Navigate to `/Demo` after starting the app  

📖 **Learn the patterns**: See [HTMX Patterns Guide](../../docs/HTMX-PATTERNS.md)

## Key Differences from Modular Template

## Quick Start

| Aspect | Monolith Template | Modular Template |

|--------|------------------|------------------|### Prerequisites

| **Project Count** | 1 (Web project) | 1 Web + Multiple module projects |

| **Organization** | Feature folders | Module projects (4-layer each) |- .NET 9.0 SDK

| **Reusability** | Low (copy-paste) | High (NuGet packages) |- Docker Desktop (for PostgreSQL)

| **Complexity** | Low | Medium |- Your favorite IDE (Visual Studio 2022, VS Code, or Rider)

| **CLI Support** | `netmx generate feature` | `netmx add module`, `netmx generate feature -m Module` |

| **Best For** | Simple apps, MVPs | Complex apps, teams, multi-project |### 1. Start the Database



## Getting Started```bash

cd templates/modular

### 1. Prerequisitesdocker-compose up -d db

```

- .NET 9 SDK

- PostgreSQL or DockerThis will start PostgreSQL on `localhost:5432` with:

- NetMX CLI (`dotnet tool install --global NetMX.CLI`)- Database: `netmx_db`

- Username: `postgres`

### 2. Create Your Application- Password: `postgres`



```bash### 2. Run the Application

# Clone this template

git clone <template-url> MyMonolithApp```bash

cd MyMonolithAppcd src/NetMXApp.Web

dotnet run

# Update solution/project names```

# (Replace "MonolithApp" with your app name)

The application will:

# Start database- Automatically apply database migrations on startup

docker-compose up -d- Create Admin and User roles

```- Seed an admin user account

- Start listening on `http://localhost:5263`

### 3. Generate Features

### 3. Login

```bash

# Generate a new feature (entity with CRUD)Navigate to `http://localhost:5263` and login with:

cd src/MonolithApp.Web

netmx generate feature Product- **Email**: `admin@netmx.dev`

- **Password**: `Admin123!`

# This creates:

# - Models/Product.cs## Project Structure

# - Services/IProductService.cs + ProductService.cs

# - Controllers/ProductController.cs```

# - Views/Product/ (Index, _List, _Form)templates/modular/

```├── docker-compose.yml           # PostgreSQL service definition

├── NetMXApp.sln                 # Solution file

### 4. Add DbSet and Migrate└── src/

    └── NetMXApp.Web/           # Main web application

```bash        ├── Data/               # Database context

# 1. Add DbSet to Data/AppDbContext.cs        ├── Migrations/         # EF Core migrations

public DbSet<Product> Products => Set<Product>();        │   ├── *.cs           # AppDbContext migrations

        │   └── IdentityDb/    # Identity migrations (separate)

# 2. Create migration        ├── Properties/         # Launch settings

dotnet ef migrations add AddProduct        ├── wwwroot/           # Static files (CSS, JS, images)

        ├── Program.cs         # Application startup

# 3. Apply migration        └── appsettings*.json  # Configuration

dotnet ef database update```

```

## Configuration

### 5. Run Your Application

### Connection String

```bash

dotnet runThe default connection string in `appsettings.Development.json`:

```

```json

Navigate to `http://localhost:5263/Product`{

  "ConnectionStrings": {

## When to Choose Monolith Template    "DefaultConnection": "Host=localhost;Port=5432;Database=netmx_db;Username=postgres;Password=postgres"

  }

**Choose Monolith if:**}

- ✅ Building a small to medium application```

- ✅ Team prefers simplicity over modularity

- ✅ Features won't be reused in other projectsFor production, update `appsettings.json` or use environment variables:

- ✅ You're building an MVP or prototype

- ✅ You want faster development with less overhead```bash

export ConnectionStrings__DefaultConnection="Host=prod-server;Port=5432;Database=netmx_prod;Username=user;Password=securepass"

**Choose Modular if:**```

- ✅ Building a large, complex application

- ✅ Features need to be reusable (multiple apps)### Identity Settings

- ✅ Team needs clear boundaries between features

- ✅ You want to distribute modules as NuGet packagesIdentity is configured in `Program.cs` with:

- ✅ You have multiple teams working on different features

**Password Requirements:**

## Support- Minimum 8 characters

- Requires uppercase letter

- **Documentation**: [docs/](../../docs/)- Requires lowercase letter

- **Issues**: [GitHub Issues](https://github.com/toonjd/netmx/issues)- Requires digit

- Requires non-alphanumeric character

---

**Lockout Settings:**

**Remember**: Start simple (monolith), scale when needed (modular). Both templates use the same NetMX framework and CLI!- 5 failed attempts allowed

- 15 minute lockout duration
- Enabled for new users

**User Settings:**
- Unique email required
- Email confirmation optional (disabled by default)

## Database Migrations

### Viewing Current Migrations

```bash
# List AppDbContext migrations
dotnet ef migrations list --context AppDbContext

# List Identity migrations
dotnet ef migrations list --context IdentityDbContext
```

### Adding New Migrations

```bash
# For application entities
dotnet ef migrations add YourMigrationName --context AppDbContext

# For Identity changes (usually not needed)
dotnet ef migrations add YourMigrationName --context IdentityDbContext
```

### Applying Migrations

Migrations are **automatically applied** on application startup. To apply manually:

```bash
# Apply AppDbContext migrations
dotnet ef database update --context AppDbContext

# Apply Identity migrations
dotnet ef database update --context IdentityDbContext
```

### Reverting Migrations

```bash
# Revert to previous migration
dotnet ef database update PreviousMigrationName --context AppDbContext

# Remove last migration (if not applied)
dotnet ef migrations remove --context AppDbContext
```

## Identity Module

This template includes the NetMX Identity module, which provides:

### Features

- User management (create, update, delete, list)
- Role management (Admin, User roles)
- Profile management (first name, last name, phone)
- Email confirmation support
- Two-factor authentication ready
- Account lockout protection
- Multi-tenant support (optional)

### Database Tables

The Identity module creates these tables:

- `Users` - User accounts
- `Roles` - Application roles
- `UserRoles` - User-role assignments
- `UserClaims` - Custom user claims
- `UserLogins` - External login providers
- `UserTokens` - Authentication tokens
- `RoleClaims` - Custom role claims

All tables are in the main database but use a separate migration history table (`__EFMigrationsHistory_Identity`).

### Default Accounts

On first run, these accounts are created:

| Email | Password | Roles |
|-------|----------|-------|
| admin@netmx.dev | Admin123! | Admin |

### Customizing Identity

To modify Identity behavior, edit `Program.cs`:

```csharp
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
})
```

## Adding New Entities

1. **Create the entity** in `Data/` folder
2. **Add DbSet** to `AppDbContext`
3. **Configure entity** in `OnModelCreating`
4. **Generate migration**: `dotnet ef migrations add AddYourEntity --context AppDbContext`
5. **Run the app** - migration applies automatically

Example:

```csharp
// Data/Product.cs
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Data/AppDbContext.cs
public DbSet<Product> Products { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<Product>(b =>
    {
        b.ToTable("Products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        b.Property(x => x.Price).HasPrecision(18, 2);
    });
}
```

## Docker Deployment

### Building the Application

```bash
# From the template root
docker-compose build
```

### Running with Docker Compose

```bash
# Start both database and application
docker-compose up -d

# View logs
docker-compose logs -f web

# Stop all services
docker-compose down

# Stop and remove volumes (fresh database)
docker-compose down -v
```

The application will be available at `http://localhost:8080`.

## Development Workflow

### Hot Reload

The template supports .NET hot reload for faster development:

```bash
dotnet watch run
```

Changes to C# files will be applied without restarting the application.

### Database Reset

To start with a fresh database:

```bash
# Stop and remove database
docker-compose down -v

# Restart database
docker-compose up -d db

# Run application (migrations apply automatically)
dotnet run
```

## Troubleshooting

### "Failed to connect to 127.0.0.1:5432"

**Problem**: PostgreSQL is not running.

**Solution**:
```bash
docker-compose up -d db
```

### "The model for context 'AppDbContext' has pending changes"

**Problem**: Entity changes haven't been migrated.

**Solution**:
```bash
dotnet ef migrations add YourMigrationName --context AppDbContext
```

### "Login failed"

**Problem**: Admin user might not be seeded.

**Solution**: Check console output for "✓ Created admin user" message. If missing, delete the database and restart:

```bash
docker-compose down -v
docker-compose up -d db
dotnet run
```

### Port Already in Use

**Problem**: Port 5263 or 5432 is already in use.

**Solution**: Change ports in `launchSettings.json` (app) or `docker-compose.yml` (database).

## Next Steps

- **Add Features**: Implement your domain entities and business logic
- **Add UI**: Create controllers and views using HTMX patterns
- **Add Tests**: Set up xUnit tests for your application services
- **Add Modules**: Integrate additional NetMX modules (Audit, Background Jobs, etc.)
- **Deploy**: Configure for your production environment

## Resources

- [NetMX Documentation](../../README.md)
- [ASP.NET Core Identity Docs](https://docs.microsoft.com/aspnet/core/security/authentication/identity)
- [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
- [HTMX Documentation](https://htmx.org/docs/)
- [Bulma Documentation](https://bulma.io/documentation/)

## License

This template is part of the NetMX framework and follows the same license.

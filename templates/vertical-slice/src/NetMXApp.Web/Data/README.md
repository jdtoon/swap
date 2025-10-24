# NetMX Application Data Layer

This folder contains the data access layer for the NetMX application.

## Overview

The data layer is built on **Entity Framework Core** with **PostgreSQL** as the database provider. It inherits from the NetMX framework's `NetMXDbContext` to gain enterprise-grade features automatically.

## Architecture

```
NetMXApp.Web/Data/
├── AppDbContext.cs                  # Main application DbContext
├── AppDbContextFactory.cs           # Design-time factory for EF Core tools
└── README.md                        # This file

NetMXApp.Web/Migrations/             # EF Core migrations (auto-generated)
└── YYYYMMDDHHMMSS_InitialCreate.cs
```

## AppDbContext

The `AppDbContext` is the concrete implementation of your application's database context. It inherits from `NetMXDbContext<TContext>` provided by the framework.

### Framework Features (Inherited)

By inheriting from `NetMXDbContext`, you automatically get:

1. **Soft Delete Filtering**: Entities implementing `ISoftDelete` are automatically excluded from queries
2. **Multi-Tenancy Support**: Automatic tenant isolation (when enabled)
3. **Audit Logging Integration**: Tracks who created/modified entities and when
4. **Concurrency Checking**: Optimistic concurrency control via `IHasConcurrencyStamp`

### Adding Entities

To add entities to your database:

```csharp
public class AppDbContext : NetMXDbContext<AppDbContext>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) { }

    // Add your DbSets here
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

### Configuring Entity Mappings

Use the `OnModelCreating` method to configure your entities:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder); // IMPORTANT: Call base first!

    modelBuilder.Entity<Product>(b =>
    {
        b.ToTable("Products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        b.Property(x => x.Price).HasColumnType("decimal(18,2)");
    });
}
```

## AppDbContextFactory

The `AppDbContextFactory` is used by EF Core command-line tools at design time. It allows commands like `dotnet ef migrations add` to create an instance of your DbContext without running the full application.

**You don't need to modify this file unless you change the connection string configuration.**

## Database Migrations

### Creating a Migration

```bash
# From the NetMXApp.Web directory
dotnet ef migrations add MigrationName

# Example:
dotnet ef migrations add AddProductsTable
```

This will create a new migration file in the `Migrations/` folder.

### Applying Migrations

#### Development (Automatic)
The application is configured to automatically apply pending migrations on startup in development mode (see `Program.cs`).

#### Production (Manual)
```bash
dotnet ef database update

# Or update to a specific migration:
dotnet ef database update MigrationName
```

### Removing a Migration

```bash
# Remove the last migration (if not yet applied to database)
dotnet ef migrations remove
```

### Viewing Migration SQL

```bash
# See the SQL that will be executed
dotnet ef migrations script

# Or for a specific migration:
dotnet ef migrations script PreviousMigration TargetMigration
```

## Connection String Configuration

The connection string is configured in `appsettings.json` and can be overridden per environment:

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=netmxapp;Username=postgres;Password=postgres"
  }
}
```

**Docker Compose Override:**
The `docker-compose.yml` file overrides this with environment variables for containerized environments.

## Database Provider

NetMX uses **PostgreSQL** as the primary database provider via the `Npgsql.EntityFrameworkCore.PostgreSQL` package.

**Why PostgreSQL?**
- Open-source and free
- Enterprise-grade reliability
- Excellent JSON support for flexible schemas
- Great performance
- Wide hosting support

## Best Practices

### 1. Always Call base.OnModelCreating()
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder); // Framework magic happens here!
    
    // Your configuration below
}
```

### 2. Use Explicit Configuration
Don't rely on EF Core conventions alone. Explicitly configure:
- Table names: `b.ToTable("Products")`
- Max lengths: `HasMaxLength(256)`
- Required fields: `IsRequired()`
- Column types: `HasColumnType("decimal(18,2)")`

### 3. Use Migrations for All Schema Changes
Never modify the database schema manually. Always create a migration.

### 4. Name Migrations Descriptively
```bash
# Good
dotnet ef migrations add AddProductCategoryRelationship

# Bad
dotnet ef migrations add Update1
```

### 5. Test Migrations Both Up and Down
```bash
# Apply migration
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigration
```

## Module Integration

When you add application modules (like Identity, SaaS, etc.), they will define their own entities. You have two options:

### Option A: Single DbContext (Recommended for Modular Monolith)
Add module entities to `AppDbContext`:

```csharp
// Identity module entities
public DbSet<AppUser> Users { get; set; }
public DbSet<AppRole> Roles { get; set; }

// Your application entities
public DbSet<Product> Products { get; set; }
```

### Option B: Separate DbContext per Module
Each module maintains its own `ModuleDbContext`. This is more complex but provides better separation for microservice scenarios.

For Phase 1 MVP, we'll use **Option A** (single DbContext).

## Troubleshooting

### "No DbContext was found"
Ensure you're running commands from the `NetMXApp.Web` directory.

### "Build failed"
Run `dotnet build` first to ensure the project compiles.

### "Unable to create an object of type 'AppDbContext'"
Check that `AppDbContextFactory` exists and your connection string is valid in `appsettings.json`.

### Migration fails to apply
1. Check PostgreSQL is running: `docker ps`
2. Check connection string is correct
3. Check database exists (it will be created automatically on first run)

## References

- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [NetMX Framework Documentation](../../../../framework/NetMX.EntityFrameworkCore/README.md)

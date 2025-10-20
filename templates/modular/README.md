# NetMX Modular Template

A starter template for building modular monolith applications with NetMX framework.

## Features

- **ASP.NET Core 9.0** - Latest .NET stack
- **PostgreSQL Database** - Production-ready relational database
- **Entity Framework Core** - Code-first migrations and LINQ queries
- **ASP.NET Core Identity** - Complete authentication and authorization
- **Docker Support** - Containerized PostgreSQL for development
- **HTMX-First UI** - Server-rendered HTML with HTMX for interactivity
- **Bulma CSS** - Clean, modern UI framework
- **Modular Architecture** - Clean separation of concerns
- **🎯 HTMX Showcase** - 8+ interactive examples demonstrating HTMX patterns

## 🎯 HTMX Showcase

This template includes a comprehensive **HTMX Showcase** at `/Demo` with real-world examples:

1. **Click to Edit** - Inline editing without page reload
2. **Delete with Confirmation** - Surgical DOM updates
3. **Infinite Scroll** - Auto-load content when scrolling
4. **Search with Debounce** - Live search with 500ms delay
5. **Tab Switching** - Dynamic tab content loading
6. **Form Validation** - Server-side validation with inline errors
7. **Out-of-Band Updates** - Update multiple page sections at once
8. **Lazy Loading** - Load expensive content when visible

👉 **See it live**: Navigate to `/Demo` after starting the app  
📖 **Learn the patterns**: See [HTMX Patterns Guide](../../docs/HTMX-PATTERNS.md)

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop (for PostgreSQL)
- Your favorite IDE (Visual Studio 2022, VS Code, or Rider)

### 1. Start the Database

```bash
cd templates/modular
docker-compose up -d db
```

This will start PostgreSQL on `localhost:5432` with:
- Database: `netmx_db`
- Username: `postgres`
- Password: `postgres`

### 2. Run the Application

```bash
cd src/NetMXApp.Web
dotnet run
```

The application will:
- Automatically apply database migrations on startup
- Create Admin and User roles
- Seed an admin user account
- Start listening on `http://localhost:5263`

### 3. Login

Navigate to `http://localhost:5263` and login with:

- **Email**: `admin@netmx.dev`
- **Password**: `Admin123!`

## Project Structure

```
templates/modular/
├── docker-compose.yml           # PostgreSQL service definition
├── NetMXApp.sln                 # Solution file
└── src/
    └── NetMXApp.Web/           # Main web application
        ├── Data/               # Database context
        ├── Migrations/         # EF Core migrations
        │   ├── *.cs           # AppDbContext migrations
        │   └── IdentityDb/    # Identity migrations (separate)
        ├── Properties/         # Launch settings
        ├── wwwroot/           # Static files (CSS, JS, images)
        ├── Program.cs         # Application startup
        └── appsettings*.json  # Configuration
```

## Configuration

### Connection String

The default connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=netmx_db;Username=postgres;Password=postgres"
  }
}
```

For production, update `appsettings.json` or use environment variables:

```bash
export ConnectionStrings__DefaultConnection="Host=prod-server;Port=5432;Database=netmx_prod;Username=user;Password=securepass"
```

### Identity Settings

Identity is configured in `Program.cs` with:

**Password Requirements:**
- Minimum 8 characters
- Requires uppercase letter
- Requires lowercase letter
- Requires digit
- Requires non-alphanumeric character

**Lockout Settings:**
- 5 failed attempts allowed
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

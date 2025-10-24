# NetMX Modular Architecture

**Understanding Module-Based Design in NetMX**

---

## Overview

NetMX uses a **modular architecture** where features are organized into self-contained, reusable modules. This document explains the design principles, structure, and benefits of this approach.

## Core Concepts

### What is a Module?

A **module** is a self-contained package containing related features that can be:
- ✅ Developed independently
- ✅ Tested in isolation
- ✅ Reused across multiple applications
- ✅ Packaged as NuGet packages
- ✅ Versioned separately

**Examples**:
- **Identity Module**: User authentication, registration, profile management
- **Authorization Module**: Permissions, roles, policies
- **Audit Module**: Audit logging, change tracking, compliance

### What is a Feature?

A **feature** is a single business entity with complete CRUD operations:
- Entity (domain model)
- DTOs (data transfer objects)
- Service interface + implementation
- Controller with HTMX support
- Views (Index, List, Form)

**Examples** (in Identity module):
- User feature (AppUser entity)
- Role feature (AppRole entity)

---

## Module Structure

### 4-Layer Architecture

Every module follows a **4-layer clean architecture** pattern:

```
ModuleName/
├── ModuleName.Core/              # Domain Layer (entities, value objects)
├── ModuleName.Contracts/         # Contracts Layer (DTOs, interfaces)
├── ModuleName.Application/       # Application Layer (services, use cases)
└── ModuleName.Web/              # Presentation Layer (controllers, views)
```

### Layer Responsibilities

#### 1. Core Layer (Domain)
**Purpose**: Contains domain entities, value objects, and domain logic

**Contains**:
- Entities (`AppUser`, `Permission`, `Role`)
- Value objects (`Email`, `PhoneNumber`)
- Domain events (`UserCreatedEvent`)
- Domain services (complex business logic)
- Repository interfaces (`IUserRepository`)

**Dependencies**: None (pure domain logic)

**Example**:
```csharp
// Identity.Core/Entities/AppUser.cs
public class AppUser : Entity<Guid>
{
    public string UserName { get; private set; }
    public string Email { get; private set; }
    
    private AppUser() { } // EF Core
    
    public AppUser(Guid id, string userName, string email) : base(id)
    {
        UserName = Guard.NotNullOrEmpty(userName, nameof(userName));
        Email = Guard.Email(email, nameof(email));
    }
    
    public void UpdateEmail(string email)
    {
        Email = Guard.Email(email, nameof(email));
        AddDomainEvent(new UserEmailChangedEvent(Id, email));
    }
}
```

#### 2. Contracts Layer (DTOs & Interfaces)
**Purpose**: Defines contracts for communication between layers

**Contains**:
- DTOs (`UserDto`, `CreateUserDto`, `UpdateUserDto`)
- Service interfaces (`IUserService`)
- Event definitions (if shared)

**Dependencies**: NetMX.Ddd.Application.Contracts

**Example**:
```csharp
// Identity.Contracts/Dtos/UserDto.cs
public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Identity.Contracts/Services/IUserService.cs
public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task DeleteAsync(Guid id);
}
```

#### 3. Application Layer (Services)
**Purpose**: Implements business logic and use cases

**Contains**:
- Service implementations (`UserService`)
- Use case handlers
- Validators
- Mappers (entity ↔ DTO)

**Dependencies**: Core, Contracts, NetMX.Ddd.Application, NetMX.EntityFrameworkCore

**Example**:
```csharp
// Identity.Application/Services/UserService.cs
public class UserService : IUserService, IScopedDependency
{
    private readonly IRepository<AppUser, Guid> _userRepository;
    
    public UserService(IRepository<AppUser, Guid> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new AppUser(Guid.NewGuid(), dto.UserName, dto.Email);
        await _userRepository.InsertAsync(user);
        return MapToDto(user);
    }
}
```

#### 4. Web Layer (Presentation)
**Purpose**: Handles HTTP requests and renders views

**Contains**:
- Controllers (`UsersController`)
- Views (`.cshtml` files)
- View models (if needed)
- HTMX helpers
- Module registration (`NetMXIdentityWebModule`)

**Dependencies**: Core, Contracts, Application, NetMX.AspNetCore.Mvc, NetMX.Htmx

**Example**:
```csharp
// Identity.Web/Controllers/UsersController.cs
[Route("Users")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return PartialView("_Form", dto);
            
        var user = await _userService.CreateAsync(dto);
        
        // Type-safe HTMX event
        this.HxTrigger(Events.User.Created, new { userId = user.Id });
        
        return Ok();
    }
}
```

---

## Module Descriptor (module.json)

Every module has a `module.json` file describing its structure:

```json
{
  "name": "NetMX.Identity",
  "version": "1.0.0",
  "description": "Authentication and authorization module",
  "author": "NetMX Team",
  "category": "security",
  "tags": ["authentication", "authorization", "identity"],
  "homepage": "https://github.com/toonjd/netmx",
  
  "dependencies": [
    "NetMX.Core >= 1.0.0",
    "NetMX.Ddd.Domain >= 1.0.0",
    "NetMX.Ddd.Application >= 1.0.0",
    "NetMX.EntityFrameworkCore >= 1.0.0"
  ],
  
  "projects": [
    {
      "name": "Identity.Core",
      "path": "Identity.Core/Identity.Core.csproj",
      "type": "domain"
    },
    {
      "name": "Identity.Contracts",
      "path": "Identity.Contracts/Identity.Contracts.csproj",
      "type": "contracts"
    },
    {
      "name": "Identity.Application",
      "path": "Identity.Application/Identity.Application.csproj",
      "type": "application"
    },
    {
      "name": "Identity.Web",
      "path": "Identity.Web/Identity.Web.csproj",
      "type": "web"
    }
  ],
  
  "services": [
    {
      "type": "module",
      "class": "NetMX.Identity.Web.NetMXIdentityWebModule",
      "description": "Registers Identity module services"
    },
    {
      "type": "dbcontext",
      "class": "NetMX.Identity.Core.Data.IdentityDbContext",
      "connectionStringName": "DefaultConnection",
      "migrationHistoryTable": "__IdentityMigrationsHistory",
      "description": "Identity module database context"
    }
  ],
  
  "routes": [
    {
      "pattern": "/account/*",
      "description": "Account management (login, register, profile)"
    },
    {
      "pattern": "/identity/*",
      "description": "Identity administration (users, roles)"
    }
  ],
  
  "middleware": [
    {
      "type": "Microsoft.AspNetCore.Authentication.AuthenticationMiddleware",
      "order": 100,
      "description": "Authentication middleware"
    },
    {
      "type": "Microsoft.AspNetCore.Authorization.AuthorizationMiddleware",
      "order": 110,
      "description": "Authorization middleware"
    }
  ],
  
  "migrations": {
    "enabled": true,
    "autoApply": false,
    "contextName": "IdentityDbContext",
    "migrationHistoryTable": "__IdentityMigrationsHistory"
  },
  
  "configuration": {
    "identity": {
      "requireConfirmedEmail": false,
      "passwordRequireDigit": true,
      "passwordRequireUppercase": true,
      "passwordMinLength": 8,
      "lockoutEnabled": true,
      "maxFailedAttempts": 5,
      "lockoutDuration": "00:15:00"
    }
  },
  
  "features": [
    {
      "name": "Users",
      "enabled": true,
      "description": "User management (create, edit, delete users)"
    },
    {
      "name": "Roles",
      "enabled": true,
      "description": "Role management (create, edit, delete roles)"
    },
    {
      "name": "TwoFactorAuthentication",
      "enabled": false,
      "description": "Two-factor authentication support"
    }
  ]
}
```

---

## Module Communication

Modules communicate through **well-defined contracts**:

### 1. Type-Safe Events (Recommended)

**Benefits**:
- Compile-time safety
- IntelliSense support
- Loose coupling
- Easy to test

**Example**:
```csharp
// In controller (any module)
this.HxTrigger(Events.User.Created, new { userId = user.Id });

// In view (any module)
<div hx-get="/stats" 
     hx-trigger="@Events.User.Created from:body">
    <!-- Auto-refreshes when user created -->
</div>
```

### 2. Service Interfaces

**Example**:
```csharp
// Module A exposes service
public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
}

// Module B uses service
public class OrderService
{
    private readonly IUserService _userService;
    
    public OrderService(IUserService userService)
    {
        _userService = userService;
    }
    
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var user = await _userService.GetByIdAsync(dto.UserId);
        // Use user data...
    }
}
```

### 3. Domain Events

**Example**:
```csharp
// Identity module raises event
public class AppUser : AggregateRoot<Guid>
{
    public void UpdateEmail(string email)
    {
        Email = email;
        AddDomainEvent(new UserEmailChangedEvent(Id, email));
    }
}

// Audit module handles event
public class UserEmailChangedEventHandler : IDomainEventHandler<UserEmailChangedEvent>
{
    private readonly IAuditLogService _auditLogService;
    
    public async Task HandleAsync(UserEmailChangedEvent @event)
    {
        await _auditLogService.LogAsync(
            entityId: @event.UserId,
            action: "EmailChanged",
            newValue: @event.NewEmail
        );
    }
}
```

---

## Module Registration

Modules are registered in the host application's `Program.cs`:

```csharp
// Program.cs (in application, not module)
var builder = WebApplication.CreateBuilder(args);

// Register Identity module (auto-discovered services)
builder.Services.AddIdentity(); // From NetMXIdentityWebModule

// Register Authorization module
builder.Services.AddAuthorization(); // From NetMXAuthorizationWebModule

// Register Audit module
builder.Services.AddAudit(); // From NetMXAuditWebModule

var app = builder.Build();

// Middleware registered by modules
app.UseAuthentication(); // From Identity module
app.UseAuthorization();  // From Authorization module

app.MapControllers();
app.Run();
```

**Module Extension Method** (in module):
```csharp
// Identity.Web/NetMXIdentityWebModule.cs
public static class NetMXIdentityWebModule
{
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        
        // Register ASP.NET Core Identity
        services.AddIdentity<AppUser, AppRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();
        
        // Register DbContext
        services.AddDbContext<IdentityDbContext>(...);
        
        return services;
    }
}
```

---

## Module Isolation

Each module is **independently testable** and **deployable**:

### 1. Own Database Context
```csharp
// Identity.Core/Data/IdentityDbContext.cs
public class IdentityDbContext : NetMXDbContext<IdentityDbContext>
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    
    // Separate migration history table
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMigrationsHistoryTable("__IdentityMigrationsHistory");
    }
}
```

### 2. Own Migrations
```bash
# Module-specific migrations
dotnet ef migrations add InitialCreate \
  --context IdentityDbContext \
  --output-dir Data/Migrations
```

### 3. Own Solution File
```
modules/Identity/
├── Identity.sln              # Module can be built independently
├── Identity.Core/
├── Identity.Contracts/
├── Identity.Application/
├── Identity.Web/
├── Identity.Tests/           # Unit tests
├── Identity.Web.Tests/       # Integration tests
└── Identity.E2E.Tests/       # E2E tests with Playwright
```

---

## Benefits of Modular Architecture

### 1. Reusability
- Modules can be used in multiple applications
- Package as NuGet packages
- Share across team/organization

### 2. Maintainability
- Clear boundaries between modules
- Changes in one module don't affect others
- Easier to understand and navigate

### 3. Testability
- Test modules in isolation
- Mock dependencies easily
- Unit, integration, and E2E tests

### 4. Scalability
- Add/remove modules as needed
- Scale teams (one team per module)
- Gradual feature additions

### 5. Flexibility
- Mix and match modules
- Replace modules with custom implementations
- Enable/disable features per module

---

## CLI Workflow

### Creating Modules
```bash
# Create new module (in framework/ directory)
cd framework
netmx create module MyModule

# Result: modules/MyModule/ with 4 layers + 3 test projects
```

### Adding Modules to Applications
```bash
# Add existing module to application
cd MyApp
netmx add module Identity --source "../modules/Identity"

# Result: 4 project references added, Program.cs updated
```

### Generating Features in Modules
```bash
# Generate feature in module
cd modules/MyModule/MyModule.Web
netmx generate feature Product -m MyModule

# Result: Product entity + CRUD in module
```

---

## Migration Path

### From Monolith → Modular

**Step 1**: Identify feature boundaries
- Group related entities together
- Look for bounded contexts (DDD)

**Step 2**: Extract into modules
```bash
netmx create module Catalog
# Move Product, Category entities to Catalog.Core
# Move services to Catalog.Application
# Move controllers to Catalog.Web
```

**Step 3**: Update references
- Replace `using MyApp.Models` with `using Catalog.Core.Entities`
- Update DI registrations

**Step 4**: Test thoroughly
- Unit tests still pass?
- Integration tests still pass?
- E2E tests still pass?

**Time**: 2-4 hours per module

---

## Best Practices

### 1. Module Naming
- ✅ Use descriptive names: `Identity`, `Authorization`, `Audit`
- ❌ Avoid generic names: `Helpers`, `Utilities`, `Common`

### 2. Module Boundaries
- ✅ Clear responsibility (single bounded context)
- ❌ Modules depending on many other modules

### 3. Dependencies
- ✅ Depend on framework packages (`NetMX.Core`, `NetMX.Ddd.Domain`)
- ✅ Depend on other module contracts (via interfaces)
- ❌ Depend on other module implementations

### 4. Communication
- ✅ Use events for loose coupling
- ✅ Use interfaces for service contracts
- ❌ Direct entity references across modules

### 5. Testing
- ✅ Write unit tests for services
- ✅ Write integration tests for controllers
- ✅ Write E2E tests for critical flows

---

## Examples

### Small Application (1-3 Modules)
```
MyApp/
├── src/MyApp.Web/           # Main application
└── modules/
    ├── Identity/            # User management
    └── Audit/               # Audit logging
```

### Medium Application (4-10 Modules)
```
MyApp/
├── src/MyApp.Web/
└── modules/
    ├── Identity/
    ├── Authorization/
    ├── Audit/
    ├── Catalog/             # Products, categories
    ├── Orders/              # Order management
    ├── Customers/           # Customer management
    └── Notifications/       # Email, SMS
```

### Large Application (10+ Modules)
```
MyApp/
├── src/MyApp.Web/
└── modules/
    ├── Identity/
    ├── Authorization/
    ├── Audit/
    ├── Settings/
    ├── Catalog/
    ├── Orders/
    ├── Customers/
    ├── Notifications/
    ├── Payments/
    ├── Shipping/
    ├── Inventory/
    ├── Reporting/
    └── CMS/
```

---

## Common Questions

**Q: When should I create a new module vs add to existing?**  
A: Create new module if:
- Different bounded context (DDD)
- Could be reused in other apps
- Different team owns it
- Different deployment schedule

**Q: Can modules have dependencies on other modules?**  
A: Yes, but:
- Depend on **contracts** (interfaces), not implementations
- Keep dependencies minimal
- Avoid circular dependencies

**Q: Should I use modules for small apps?**  
A: Not necessarily. For simple apps, use the **monolith template**. Migrate to modules when:
- App grows beyond 50-100 entities
- Features need to be reused
- Multiple teams working on it

**Q: How do I version modules?**  
A: Use **SemVer** (semantic versioning):
- Major version: Breaking changes
- Minor version: New features (backward compatible)
- Patch version: Bug fixes

**Q: Can I have multiple modules in one NuGet package?**  
A: Not recommended. One module = one NuGet package (or 4, one per layer).

---

## See Also

- [TERMINOLOGY.md](TERMINOLOGY.md) - Module vs Feature vs Component
- [QUICK-START.md](QUICK-START.md) - Getting started guide
- [CLI-IMPLEMENTATION.md](CLI-IMPLEMENTATION.md) - CLI command reference
- [HTMX-PATTERNS.md](HTMX-PATTERNS.md) - HTMX patterns in modules

---

**Remember**: Modules are about **reusability** and **clear boundaries**. If you don't need reusability, start with a monolith!

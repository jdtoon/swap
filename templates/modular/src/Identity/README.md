# Identity Module

A complete, HTMX-first identity management module for NetMX applications. This module provides user and role management with a clean separation of concerns following Domain-Driven Design (DDD) principles.

## 🏗️ Architecture

The Identity module follows the modular monolith pattern with four distinct layers:

```
Identity/
├── Identity.Core/                    # Domain Layer
│   └── Entities/
│       ├── AppUser.cs               # User aggregate root
│       ├── AppRole.cs               # Role entity  
│       └── AppUserRole.cs           # Many-to-many relationship
│
├── Identity.Application.Contracts/   # Application Contracts Layer
│   ├── Users/
│   │   ├── UserDto.cs
│   │   ├── CreateUserDto.cs
│   │   └── UpdateUserDto.cs
│   └── Roles/
│       ├── RoleDto.cs
│       ├── CreateRoleDto.cs
│       └── UpdateRoleDto.cs
│
├── Identity.Application/             # Application Layer
│   ├── Users/
│   │   └── UserAppService.cs        # User CRUD operations
│   └── Roles/
│       └── RoleAppService.cs        # Role CRUD operations
│
└── Identity.Web/                     # Presentation Layer
    ├── Controllers/
    │   ├── UsersController.cs       # HTMX endpoints for users
    │   └── RolesController.cs       # HTMX endpoints for roles
    └── Views/
        ├── Users/
        │   ├── Index.cshtml         # Main user management page
        │   ├── _UserList.cshtml     # User table partial
        │   ├── _UserRow.cshtml      # Single user row partial
        │   ├── _UserForm.cshtml     # Create user form partial
        │   └── _UserEditForm.cshtml # Edit user form partial
        └── Roles/
            ├── Index.cshtml
            ├── _RoleList.cshtml
            ├── _RoleRow.cshtml
            ├── _RoleForm.cshtml
            └── _RoleEditForm.cshtml
```

## ✨ Features

### User Management
- ✅ Create users with email validation
- ✅ Update user profiles (name, phone)
- ✅ Activate/Deactivate users
- ✅ Delete users (with cascade delete of role assignments)
- ✅ Email and phone confirmation tracking
- ✅ Assign multiple roles to users
- ✅ Password management (change password)
- ✅ Login tracking (last login date)

### Role Management
- ✅ Create custom roles
- ✅ Update role details
- ✅ Delete roles (with user assignment validation)
- ✅ System role protection (prevents modification/deletion)
- ✅ Role-based user organization

### HTMX Integration
- ✅ Zero full page reloads
- ✅ Instant UI updates via partials
- ✅ Optimistic UI patterns
- ✅ Inline editing
- ✅ Delete confirmations
- ✅ Form validation with server-side errors

## 🚀 Usage

### Accessing the Module

Navigate to **Identity → Users** or **Identity → Roles** from the main navigation menu.

### Creating a User

```csharp
// In your code
var userService = serviceProvider.GetRequiredService<UserAppService>();

var newUser = await userService.CreateAsync(new CreateUserDto
{
    Email = "john.doe@example.com",
    Password = "SecurePassword123!",
    FullName = "John Doe",
    PhoneNumber = "+1234567890",
    EmailConfirmed = false,
    PhoneNumberConfirmed = false,
    IsActive = true,
    RoleIds = new List<Guid> { adminRoleId }
});
```

### Creating a Role

```csharp
var roleService = serviceProvider.GetRequiredService<RoleAppService>();

var newRole = await roleService.CreateAsync(new CreateRoleDto
{
    Name = "Administrator",
    Description = "Full system access"
});
```

### Activating/Deactivating Users

```csharp
await userService.ActivateAsync(userId);
await userService.DeactivateAsync(userId);
```

## 🔌 Integration

### Database Configuration

The Identity module entities are automatically registered in `AppDbContext`:

```csharp
public class AppDbContext : NetMXDbContext<AppDbContext>
{
    public DbSet<AppUser> Users { get; set; }
    public DbSet<AppRole> Roles { get; set; }
    public DbSet<AppUserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Entity configurations with indexes and constraints
        // ... (see Data/AppDbContext.cs for full configuration)
    }
}
```

### Service Registration

Register Identity services in `Program.cs`:

```csharp
// Register repositories
builder.Services.AddScoped<IQueryableRepository<AppUser, Guid>>(sp => 
    new EfCoreRepository<AppDbContext, AppUser, Guid>(sp.GetRequiredService<AppDbContext>()));
builder.Services.AddScoped<IQueryableRepository<AppRole, Guid>>(sp => 
    new EfCoreRepository<AppDbContext, AppRole, Guid>(sp.GetRequiredService<AppDbContext>()));
builder.Services.AddScoped<IQueryableRepository<AppUserRole, Guid>>(sp => 
    new EfCoreRepository<AppDbContext, AppUserRole, Guid>(sp.GetRequiredService<AppDbContext>()));

// Register application services
builder.Services.AddScoped<UserAppService>();
builder.Services.AddScoped<RoleAppService>();
```

### Migrations

Apply the Identity module migration:

```bash
cd src/NetMXApp.Web
dotnet ef migrations add AddIdentityModule
dotnet ef database update
```

## 📊 Database Schema

### Users Table
| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier | Primary key |
| Email | nvarchar(256) | Unique, indexed |
| FullName | nvarchar(256) | Optional |
| PhoneNumber | nvarchar(20) | Optional |
| PasswordHash | nvarchar(512) | Required |
| EmailConfirmed | bit | Confirmation status |
| PhoneNumberConfirmed | bit | Confirmation status |
| IsActive | bit | Active/inactive flag |
| LastLoginDate | datetime | Last successful login |

### Roles Table
| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier | Primary key |
| Name | nvarchar(256) | Unique, indexed |
| Description | nvarchar(1000) | Optional |
| IsSystemRole | bit | Protection flag |

### UserRoles Table
| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier | Primary key |
| UserId | uniqueidentifier | FK to Users |
| RoleId | uniqueidentifier | FK to Roles |

**Unique Constraint:** (`UserId`, `RoleId`)

## 🎨 UI Components

### HTMX Patterns

The Identity module demonstrates key HTMX patterns:

1. **Partial Swapping**
   ```html
   <button hx-get="/Users/Edit/123" 
           hx-target="#user-form-container"
           hx-swap="innerHTML">
       Edit
   </button>
   ```

2. **Form Submission**
   ```html
   <form hx-post="/Users/Create" 
         hx-swap="none">
       <!-- Form fields -->
   </form>
   ```

3. **Delete with Confirmation**
   ```html
   <button hx-delete="/Users/Delete/123"
           hx-confirm="Delete this user?"
           hx-target="#user-row-123"
           hx-swap="delete">
       Delete
   </button>
   ```

4. **Response Targeting**
   ```csharp
   // Server-side response headers
   Response.Headers.Add("HX-Retarget", "#user-list-container");
   Response.Headers.Add("HX-Reswap", "innerHTML");
   return PartialView("_UserList", users);
   ```

## 🔐 Security Notes

> ⚠️ **WARNING:** The current implementation stores passwords in plain text for demonstration purposes only!

### Production Recommendations

1. **Password Hashing**
   ```csharp
   // Replace plain password storage with:
   using BCrypt.Net;
   
   var passwordHash = BCrypt.HashPassword(plainTextPassword);
   var isValid = BCrypt.Verify(plainTextPassword, passwordHash);
   ```

2. **Authentication Integration**
   - This module provides user/role management only
   - Task 4.6 will integrate Logto for authentication
   - Consider ASP.NET Core Identity for built-in auth features

3. **Authorization**
   ```csharp
   // Add authorization attributes to controllers
   [Authorize(Roles = "Administrator")]
   public class UsersController : Controller { }
   ```

## 🧪 Testing

### Example Unit Test

```csharp
[Fact]
public void AppUser_Create_WithInvalidEmail_ThrowsException()
{
    // Arrange
    var invalidEmail = "not-an-email";
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => 
        AppUser.Create(invalidEmail, "hash123", "John Doe"));
}
```

### Testing Application Services

```csharp
[Fact]
public async Task CreateAsync_WithDuplicateEmail_ThrowsException()
{
    // Arrange
    var userService = new UserAppService(
        userRepository, roleRepository, userRoleRepository);
    
    // Create first user
    await userService.CreateAsync(new CreateUserDto 
    { 
        Email = "test@example.com",
        Password = "password123"
    });
    
    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        userService.CreateAsync(new CreateUserDto 
        { 
            Email = "test@example.com",
            Password = "password456"
        }));
}
```

## 📝 Best Practices

1. **Always use factory methods** for entity creation
   ```csharp
   // Good
   var user = AppUser.Create(email, passwordHash, fullName);
   
   // Avoid
   var user = new AppUser { Email = email, ... };
   ```

2. **Validate business rules** in entities
   ```csharp
   public void Update(string name, string? description)
   {
       if (IsSystemRole)
           throw new InvalidOperationException("System roles cannot be modified");
       
       Name = name;
       Description = description;
   }
   ```

3. **Use DTOs** for API boundaries
   - Never expose entities directly
   - DTOs provide clear contracts
   - Enable validation attributes

4. **Leverage HTMX headers** for flexible responses
   ```csharp
   Response.Headers.Add("HX-Retarget", "#specific-element");
   Response.Headers.Add("HX-Reswap", "outerHTML");
   ```

## 🛠️ CLI Blueprint

This module serves as the reference implementation for the `netmx add module` command. The CLI will replicate this structure when generating new modules.

### Future CLI Command

```bash
netmx add module Identity

# Will generate:
# ✓ Core layer with entities
# ✓ Application.Contracts layer with DTOs
# ✓ Application layer with services
# ✓ Web layer with controllers and views
# ✓ Database configuration and migration
# ✓ Service registration
# ✓ Navigation menu integration
```

## 📚 Related Documentation

- [NetMX Framework Documentation](../../framework/README.md)
- [HTMX Best Practices](../NetMXApp.Web/wwwroot/README.md)
- [Database Migrations Guide](../NetMXApp.Web/Data/README.md)
- [DDD Patterns](../../framework/NetMX.Ddd.Domain/README.md)

## 🎯 Next Steps

After completing the Identity module:

1. **Task 4.6:** Integrate Logto for authentication
2. **Add Authorization:** Implement role-based access control
3. **Enhance Security:** Add password hashing and policies
4. **Extend Features:** Add user groups, permissions, claims
5. **CLI Automation:** Codify this pattern in `netmx` CLI

---

**Version:** 1.0.0 (Phase 1 MVP - Task 4.5 Complete)  
**Last Updated:** October 2025  
**Author:** NetMX Team

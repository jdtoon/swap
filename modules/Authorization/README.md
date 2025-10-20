# Authorization Module

**Production-ready permission-based authorization for NetMX applications.**

## 🚀 Features

- **Permission-Based Authorization** - Fine-grained access control using permissions
- **Role-Based Access Control (RBAC)** - Group permissions into roles
- **ASP.NET Core Integration** - Seamless integration with ASP.NET Core authorization
- **Attributes** - `[RequirePermission]`, `[RequireAllPermissions]`, `[RequireAnyPermissions]`
- **Full Observability** - Distributed tracing, structured logging, performance metrics
- **Performance** - 15-minute memory cache, efficient EF Core queries
- **Flexible** - Check permissions in code, attributes, or policies

## 📦 Structure

- **Authorization.Core** - Domain entities (Permission, Role, RolePermission)
- **Authorization.Contracts** - DTOs and service interfaces (IPermissionChecker, ICurrentUser)
- **Authorization.Application** - Service implementations with observability
- **Authorization.Web** - Controllers, views, attributes, authorization handlers

## 🔧 Installation

### 1. Add Module to Your Application

```bash
cd src/YourApp.Web
netmx add module Authorization
```

### 2. Configure Services in `Program.cs`

```csharp
using Authorization.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add permission-based authorization
builder.Services.AddPermissionAuthorization();

// Add memory cache (required for PermissionChecker)
builder.Services.AddMemoryCache();

// Add your ICurrentUser implementation
builder.Services.AddScoped<ICurrentUser, YourCurrentUserImplementation>();

var app = builder.Build();

// Enable authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

### 3. Run Migrations

```bash
dotnet ef migrations add AddAuthorization
dotnet ef database update
```

## 💡 Usage

### Using Attributes on Controllers

```csharp
using Authorization.Web.Attributes;

[RequirePermission("Users.View")]
public class UsersController : Controller
{
    // All actions require "Users.View" permission
    
    public async Task<IActionResult> Index()
    {
        // Users with "Users.View" can access
    }
    
    [RequirePermission("Users.Create")]
    public async Task<IActionResult> Create()
    {
        // Users need BOTH "Users.View" AND "Users.Create"
    }
    
    [RequirePermission("Users.Delete")]
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Users need "Users.View" AND "Users.Delete"
    }
}
```

### Using Multiple Permission Attributes

```csharp
// Require ALL permissions (AND logic)
[RequireAllPermissions("Users.View", "Users.Edit")]
public async Task<IActionResult> Edit(Guid id)
{
    // User must have BOTH permissions
}

// Require ANY permission (OR logic)
[RequireAnyPermissions("Users.View", "Users.Edit", "Users.Delete")]
public async Task<IActionResult> UserManagement()
{
    // User must have AT LEAST ONE permission
}
```

### Using Permission Checker in Code

```csharp
using Authorization.Contracts.Services;

public class ProductService
{
    private readonly IPermissionChecker _permissionChecker;
    
    public ProductService(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }
    
    public async Task<bool> CanUserEditProduct()
    {
        // Check single permission
        return await _permissionChecker.IsGrantedAsync("Products.Edit");
    }
    
    public async Task<bool> CanUserManageProducts()
    {
        // Check if user has ALL permissions
        return await _permissionChecker.IsGrantedAllAsync(
            "Products.View", 
            "Products.Edit", 
            "Products.Delete");
    }
    
    public async Task<bool> CanUserAccessProducts()
    {
        // Check if user has ANY permission
        return await _permissionChecker.IsGrantedAnyAsync(
            "Products.View", 
            "Products.Edit");
    }
    
    public async Task<List<string>> GetUserPermissions()
    {
        // Get all permissions for current user
        return await _permissionChecker.GetGrantedPermissionsAsync();
    }
}
```

### Using in Views (Razor)

```html
@inject IPermissionChecker PermissionChecker

@if (await PermissionChecker.IsGrantedAsync("Users.Create"))
{
    <a href="/users/create" class="button is-primary">
        <i class="fas fa-plus"></i> Create User
    </a>
}

@if (await PermissionChecker.IsGrantedAsync("Users.Delete"))
{
    <button hx-delete="/users/@user.Id" class="button is-danger">
        <i class="fas fa-trash"></i> Delete
    </button>
}
```

## 🏗️ Domain Model

### Permission Entity

```csharp
Permission
├── Id: Guid
├── Name: string (e.g., "Users.View")
├── DisplayName: string (e.g., "View Users")
├── Group: string (e.g., "Users")
├── Description: string?
├── IsActive: bool
├── IsSystemPermission: bool
├── CreatedAt: DateTime
└── UpdatedAt: DateTime?
```

**Naming Convention**: `{Resource}.{Action}`
- ✅ `Users.View`, `Products.Edit`, `Orders.Delete`
- ❌ `ViewUsers`, `EditProduct` (no dot separator)

### Role Entity

```csharp
Role
├── Id: Guid
├── Name: string (e.g., "Admin", "Manager")
├── Description: string?
├── IsActive: bool
├── IsSystemRole: bool (can't be deleted)
├── IsDefault: bool (assigned to new users)
├── RolePermissions: ICollection<RolePermission>
├── CreatedAt: DateTime
└── UpdatedAt: DateTime?
```

### RolePermission Entity (Join Table)

```csharp
RolePermission
├── Id: Guid
├── RoleId: Guid
├── PermissionId: Guid
├── GrantedAt: DateTime
└── GrantedBy: Guid (user who granted)
```

## 📊 Observability

### Distributed Tracing

All authorization operations are traced with OpenTelemetry:

```
Activity Source: "NetMX.Authorization"
Activities:
  - CheckPermission
  - GetGrantedPermissions
  - CheckAllPermissions
  - CheckAnyPermissions

Activity Source: "NetMX.Authorization.Handler"
Activities:
  - HandlePermissionRequirement
  - HandleAllPermissionsRequirement
  - HandleAnyPermissionsRequirement

Tags:
  - permission.name
  - user.id
  - cache.hit
  - permissions.count
  - duration.ms
  - authorization.result
```

### Structured Logging

```csharp
// Permission check logs
LogInformation: "Permission {Permission} granted in {DurationMs}ms"
LogWarning: "Permission {Permission} denied: User does not have permission"
LogDebug: "Retrieved {Count} permissions from cache for user {UserId}"
LogError: "Permission check failed for {Permission} after {DurationMs}ms"
```

### Performance Metrics

- Cache hit/miss rate
- Permission check duration
- Authorization handler duration
- Failed authorization attempts
- Permissions per user

## ⚡ Performance

### Caching Strategy

- **Duration**: 15 minutes per user
- **Scope**: All permissions for user loaded in single query
- **Invalidation**: Automatic on cache expiration
- **Storage**: In-memory cache (IMemoryCache)

### Query Optimization

```csharp
// Single efficient query for all user permissions
var permissions = await queryable
    .Include(rp => rp.Permission)
    .Where(rp => roleNames.Contains(rp.Role.Name) && rp.Permission.IsActive)
    .Select(rp => rp.Permission.Name)
    .Distinct()
    .ToListAsync();
```

## 🔒 Security

### Fail-Closed Approach

- If permission check throws exception → Access denied
- If user not authenticated → Access denied
- If permission not found → Access denied

### Business Rules

- System permissions cannot be deleted or deactivated
- System roles cannot be deleted or deactivated
- Default roles cannot be deactivated
- Permission names must follow `Resource.Action` format

## 🧪 Testing

### Unit Test Example

```csharp
[Fact]
public void Permission_Constructor_ValidatesNameFormat()
{
    // Arrange & Act & Assert
    var ex = Assert.Throws<ArgumentException>(() => 
        new Permission(Guid.NewGuid(), "InvalidFormat", "Display", "Group"));
    
    Assert.Contains("Resource.Action", ex.Message);
}

[Fact]
public async Task PermissionChecker_IsGrantedAsync_ReturnsTrueForGrantedPermission()
{
    // Arrange
    var checker = new PermissionChecker(...);
    
    // Act
    var result = await checker.IsGrantedAsync("Users.View");
    
    // Assert
    Assert.True(result);
}
```

## 📚 API Reference

### IPermissionChecker

```csharp
Task<bool> IsGrantedAsync(string permissionName)
Task<bool> IsGrantedForUserAsync(Guid userId, string permissionName)
Task<List<string>> GetGrantedPermissionsAsync()
Task<List<string>> GetGrantedPermissionsForUserAsync(Guid userId)
Task<bool> IsGrantedAllAsync(params string[] permissionNames)
Task<bool> IsGrantedAnyAsync(params string[] permissionNames)
```

### ICurrentUser

```csharp
Guid? Id { get; }
bool IsAuthenticated { get; }
string? UserName { get; }
string[] Roles { get; }
```

## 🎯 Best Practices

### 1. Use Descriptive Permission Names

```csharp
✅ "Users.View", "Products.Edit", "Orders.Delete"
✅ "Reports.Export", "Settings.Manage", "Audit.Read"

❌ "View", "Edit", "Delete" (too generic)
❌ "UsersView", "ProductsEdit" (no dot separator)
```

### 2. Group Permissions by Resource

```csharp
Users:
  - Users.View
  - Users.Create
  - Users.Edit
  - Users.Delete

Products:
  - Products.View
  - Products.Create
  - Products.Edit
  - Products.Delete
```

### 3. Use Attributes for Controller Authorization

```csharp
✅ [RequirePermission("Users.View")]
✅ [RequireAllPermissions("Users.View", "Users.Edit")]

❌ Checking permissions manually in every action
```

### 4. Use IPermissionChecker for Complex Logic

```csharp
✅ if (await _permissionChecker.IsGrantedAsync("Products.Edit"))
✅ var canExport = await _permissionChecker.IsGrantedAnyAsync(
      "Reports.Export", "Reports.Admin");

❌ Checking roles directly
❌ Checking claims directly
```

## 🚧 Roadmap

- [x] Permission entities
- [x] Role entities
- [x] PermissionChecker service
- [x] Authorization attributes
- [x] Policy-based authorization
- [x] Observability
- [ ] Permission seeding
- [ ] UI for permission management
- [ ] Unit tests (80%+ coverage)
- [ ] Integration with Identity module
- [ ] Permission hierarchy (parent permissions)
- [ ] Dynamic permission registration

## 📄 License

MIT License - Free tier module
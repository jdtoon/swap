# NetMX Extensibility Principles

**Date**: October 22, 2025  
**Philosophy**: "Framework as foundation, not prison"  
**Core Principle**: Every core element should be extendable by developers

---

## 🎯 The Extensibility Mindset

### What We Mean
Every module, service, and component should follow:
- **Open/Closed Principle** - Open for extension, closed for modification
- **Interface-based** - Depend on abstractions, not implementations
- **Partial classes** - Allow seamless extension
- **Virtual methods** - Enable override when needed
- **Event hooks** - Intercept at key points

### Why This Matters
- **Developers own their code** - NetMX is a tool, not a dictator
- **No fork required** - Extend, don't modify
- **Future-proof** - Add features without breaking changes
- **Community-driven** - Anyone can extend and share

---

## 🏗️ Extensibility Patterns

### 1. Domain Events (Partial Classes) ✅ IMPLEMENTED

**Pattern**: Partial classes allow modules to extend framework events

**Framework** (NetMX.Events):
```csharp
namespace NetMX.Events;

/// <summary>
/// Domain events for the NetMX framework.
/// Modules can extend this class using partial classes.
/// </summary>
public static partial class DomainEvents
{
    // Framework events
    public static class User
    {
        public const string Created = "user.created";
        public const string Updated = "user.updated";
        public const string Deleted = "user.deleted";
    }
}
```

**Module Extension** (Authorization.Web):
```csharp
namespace NetMX.Events;

/// <summary>
/// Authorization module events extending DomainEvents
/// </summary>
public static partial class DomainEvents
{
    // Module adds its own events
    public static class Permission
    {
        public const string Created = "permission.created";
        public const string Updated = "permission.updated";
        public const string Deleted = "permission.deleted";
    }
}
```

**Developer Extension** (User's app):
```csharp
namespace NetMX.Events;

/// <summary>
/// My app's custom events
/// </summary>
public static partial class DomainEvents
{
    public static class Order
    {
        public const string Placed = "order.placed";
        public const string Shipped = "order.shipped";
        public const string Completed = "order.completed";
    }
}
```

**Result**: One unified API, all events discoverable via IntelliSense!

---

### 2. Service Replacement (Interface-based)

**Pattern**: All services are interface-based, easily replaceable

**Framework Provides**:
```csharp
public interface IPermissionChecker
{
    Task<bool> IsGrantedAsync(string permissionName);
    Task<bool> IsGrantedAsync(string[] permissionNames);
}

public class PermissionChecker : IPermissionChecker
{
    // Default implementation with caching
}
```

**Developer Extends**:
```csharp
public class MyCustomPermissionChecker : IPermissionChecker
{
    private readonly IPermissionChecker _inner;
    private readonly IMyCustomCache _cache;
    
    public MyCustomPermissionChecker(
        IPermissionChecker inner, 
        IMyCustomCache cache)
    {
        _inner = inner;
        _cache = cache;
    }
    
    public async Task<bool> IsGrantedAsync(string permissionName)
    {
        // Custom logic: Check in my cache first
        if (_cache.TryGet(permissionName, out bool result))
            return result;
        
        // Fallback to default implementation
        result = await _inner.IsGrantedAsync(permissionName);
        _cache.Set(permissionName, result);
        return result;
    }
}
```

**Registration** (Decorator Pattern):
```csharp
services.AddSingleton<IPermissionChecker, PermissionChecker>();

// Developer decorates with their own implementation
services.Decorate<IPermissionChecker, MyCustomPermissionChecker>();
```

---

### 3. Virtual Methods (Override-Friendly)

**Pattern**: Key methods are virtual, allowing override

**Framework Base Class**:
```csharp
public class ApplicationService : IApplicationService
{
    protected virtual async Task<TDto> MapToDto<TEntity, TDto>(TEntity entity)
    {
        // Default: Use AutoMapper or manual mapping
        return ObjectMapper.Map<TEntity, TDto>(entity);
    }
    
    protected virtual async Task<bool> CanDeleteAsync<TEntity>(TEntity entity)
    {
        // Default: Check if soft-deletable
        return entity is ISoftDelete;
    }
}
```

**Developer Extends**:
```csharp
public class ProductService : ApplicationService
{
    protected override async Task<bool> CanDeleteAsync<Product>(Product product)
    {
        // Custom logic: Can't delete if in active orders
        var hasActiveOrders = await _orderRepository
            .AnyAsync(o => o.ProductId == product.Id && o.Status == OrderStatus.Active);
        
        return !hasActiveOrders;
    }
}
```

---

### 4. Event Hooks (Before/After)

**Pattern**: Expose events at critical points

**Framework**:
```csharp
public class UnitOfWork : IUnitOfWork
{
    public event Func<Task>? OnBeforeSaveChanges;
    public event Func<Task>? OnAfterSaveChanges;
    
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        // Before hook
        if (OnBeforeSaveChanges != null)
            await OnBeforeSaveChanges();
        
        // Core logic
        await _dbContext.SaveChangesAsync(ct);
        
        // After hook
        if (OnAfterSaveChanges != null)
            await OnAfterSaveChanges();
    }
}
```

**Developer Uses**:
```csharp
public class MyService
{
    private readonly IUnitOfWork _uow;
    
    public MyService(IUnitOfWork uow)
    {
        _uow = uow;
        
        // Hook into save process
        _uow.OnBeforeSaveChanges += LogChangesAsync;
    }
    
    private async Task LogChangesAsync()
    {
        _logger.LogInformation("About to save changes...");
        // Custom logic here
    }
}
```

---

### 5. Middleware Pipeline (Composable)

**Pattern**: Allow insertion of custom middleware

**Framework**:
```csharp
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNetMX(this IApplicationBuilder app)
    {
        app.UseNetMXExceptionHandling();
        app.UseNetMXUnitOfWork();
        app.UseNetMXEventBus();
        
        return app;
    }
}
```

**Developer Extends**:
```csharp
app.UseNetMXExceptionHandling();
app.UseMyCustomLogging();        // Custom middleware
app.UseNetMXUnitOfWork();
app.UseMyCustomCache();          // Custom middleware
app.UseNetMXEventBus();
```

---

### 6. DbContext Extension (Partial + Virtual)

**Pattern**: DbContext is partial and has virtual methods

**Framework**:
```csharp
public abstract partial class NetMXDbContext<TDbContext> : DbContext
    where TDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Allow override
        ConfigureEntities(modelBuilder);
        ConfigureGlobalFilters(modelBuilder);
    }
    
    protected virtual void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Default configuration
    }
    
    protected virtual void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // Soft delete filter, multi-tenancy, etc.
    }
}
```

**Developer Extends**:
```csharp
public partial class AppDbContext : NetMXDbContext<AppDbContext>
{
    protected override void ConfigureEntities(ModelBuilder modelBuilder)
    {
        base.ConfigureEntities(modelBuilder);
        
        // Custom configuration
        modelBuilder.Entity<Product>(b =>
        {
            b.HasIndex(x => x.Sku).IsUnique();
            b.Property(x => x.Price).HasPrecision(18, 2);
        });
    }
}
```

---

### 7. Module Configuration (Options Pattern)

**Pattern**: Every module exposes configuration options

**Framework**:
```csharp
public class AuthorizationOptions
{
    public int CacheDurationMinutes { get; set; } = 15;
    public bool EnablePermissionCaching { get; set; } = true;
    public string[] SystemPermissions { get; set; } = Array.Empty<string>();
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configure = null)
    {
        var options = new AuthorizationOptions();
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<IPermissionChecker, PermissionChecker>();
        
        return services;
    }
}
```

**Developer Customizes**:
```csharp
services.AddAuthorization(options =>
{
    options.CacheDurationMinutes = 30;  // Custom cache duration
    options.EnablePermissionCaching = false;  // Disable caching
    options.SystemPermissions = new[] { "Admin.All" };  // Custom system permissions
});
```

---

### 8. Repository Pattern (Queryable + Customizable)

**Pattern**: Repository exposes IQueryable for flexibility

**Framework**:
```csharp
public interface IQueryableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
{
    IQueryable<TEntity> AsQueryable();
}
```

**Developer Uses**:
```csharp
public class ProductService
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public async Task<List<ProductDto>> SearchAsync(string query)
    {
        // Full query flexibility
        return await _repository.AsQueryable()
            .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .Select(p => new ProductDto { /* ... */ })
            .ToListAsync();
    }
}
```

---

### 9. HTMX Patterns (Override Views)

**Pattern**: All module views can be overridden

**Framework/Module View** (`Authorization.Web/Views/Permission/Index.cshtml`):
```html
@model List<PermissionDto>
<div class="permissions-list">
    <!-- Default implementation -->
</div>
```

**Developer Overrides** (`MyApp.Web/Views/Permission/Index.cshtml`):
```html
@model List<PermissionDto>
<div class="my-custom-permissions-list">
    <!-- Custom implementation -->
    @foreach (var permission in Model)
    {
        <partial name="_MyCustomPermissionCard" model="permission" />
    }
</div>
```

**Razor View Resolution**: Developer's view takes precedence!

---

### 10. CLI Templates (Customizable)

**Pattern**: CLI uses templates that can be overridden

**Framework Templates**:
```
~/.netmx/templates/
├── entity.cs.liquid
├── controller.cs.liquid
├── service.cs.liquid
└── view.cshtml.liquid
```

**Developer Customizes**:
```
MyApp/.netmx/templates/
└── entity.cs.liquid   # Custom entity template (takes precedence)
```

**CLI Usage**:
```bash
netmx generate feature Product --template custom
```

---

## 🎯 Extensibility Checklist

When adding new features, ensure:

- [ ] **Interface-based** - Core logic behind interfaces
- [ ] **Virtual methods** - Key methods are virtual
- [ ] **Partial classes** - Where applicable (events, DbContext)
- [ ] **Options pattern** - Configurable via options
- [ ] **Event hooks** - Before/After events exposed
- [ ] **IQueryable** - Repositories expose queryable
- [ ] **View override** - Razor views can be replaced
- [ ] **Middleware** - Can be inserted in pipeline
- [ ] **Decorator-friendly** - Services can be decorated
- [ ] **Template-based** - CLI uses overridable templates

---

## 📚 Real-World Examples

### Example 1: Custom Permission Storage

**Scenario**: Store permissions in Redis instead of database

**Developer Solution**:
```csharp
public class RedisPermissionChecker : IPermissionChecker
{
    private readonly IDistributedCache _redis;
    private readonly ICurrentUser _currentUser;
    
    public async Task<bool> IsGrantedAsync(string permissionName)
    {
        var key = $"permissions:{_currentUser.Id}";
        var permissions = await _redis.GetStringAsync(key);
        
        if (permissions == null)
        {
            // Load from database, cache in Redis
            // ...
        }
        
        return permissions.Contains(permissionName);
    }
}

// Registration
services.AddSingleton<IPermissionChecker, RedisPermissionChecker>();
```

---

### Example 2: Custom Audit Logging

**Scenario**: Send audit logs to external service instead of database

**Developer Solution**:
```csharp
public class MyCustomAuditLogger : IAuditLogger
{
    private readonly HttpClient _httpClient;
    
    public async Task LogAsync(AuditEntry entry)
    {
        // Send to external API
        await _httpClient.PostAsJsonAsync("https://audit-api.com/log", entry);
    }
}

// Registration
services.AddSingleton<IAuditLogger, MyCustomAuditLogger>();
```

---

### Example 3: Custom HTMX Swap Strategy

**Scenario**: Custom fade-in animation for HTMX swaps

**Developer Solution**:
```csharp
public static class MyHtmxExtensions
{
    public static void HxFadeIn(this Controller controller, int durationMs = 300)
    {
        controller.Response.Headers["HX-Reswap"] = $"innerHTML transition:true";
        controller.Response.Headers["HX-Transition-Duration"] = $"{durationMs}ms";
    }
}

// Usage in controller
public IActionResult Create(CreateProductDto dto)
{
    var product = _service.Create(dto);
    this.HxFadeIn(500);  // Custom extension!
    return PartialView("_ProductCard", product);
}
```

---

## 🚨 Anti-Patterns (Avoid These)

### ❌ Sealed Classes
```csharp
public sealed class PermissionChecker { }  // BAD - can't extend!
```

### ❌ Internal Interfaces
```csharp
internal interface IPermissionChecker { }  // BAD - can't implement!
```

### ❌ Static Methods Only
```csharp
public static class PermissionHelper
{
    public static bool IsGranted(string permission) { }  // BAD - can't mock/replace!
}
```

### ❌ Hard-Coded Values
```csharp
public class PermissionChecker
{
    private const int CACHE_DURATION = 15;  // BAD - not configurable!
}
```

### ❌ Private Methods (No Override)
```csharp
public class ApplicationService
{
    private Task<bool> CanDelete() { }  // BAD - can't override!
}
```

---

## ✅ Best Practices

### 1. Always Provide Default Implementation
```csharp
// Provide sensible defaults
services.AddSingleton<IPermissionChecker, PermissionChecker>();

// Developer can replace
services.AddSingleton<IPermissionChecker, MyCustomPermissionChecker>();
```

### 2. Use Configuration Options
```csharp
public class AuthorizationOptions
{
    public int CacheDuration { get; set; } = 15;  // Default with option to change
}
```

### 3. Document Extension Points
```csharp
/// <summary>
/// Permission checker service.
/// 
/// **Extension Point**: Implement <see cref="IPermissionChecker"/> to customize permission logic.
/// </summary>
public interface IPermissionChecker { }
```

### 4. Use Partial Classes for Shared Concepts
```csharp
// Events, DbContext, Options - all partial
public static partial class DomainEvents { }
public abstract partial class NetMXDbContext<T> { }
public partial class AuthorizationOptions { }
```

### 5. Expose IQueryable (Don't Hide)
```csharp
// Good - exposes query capabilities
public IQueryable<TEntity> AsQueryable();

// Bad - hides query capabilities
public Task<List<TEntity>> GetAllAsync();  // No filtering, sorting, paging!
```

---

## 📊 Impact on NetMX

### Framework
- ✅ **All services** interface-based
- ✅ **All base classes** have virtual methods
- ✅ **All events** use partial classes
- ✅ **All options** configurable
- ✅ **All repositories** expose IQueryable

### Modules
- ✅ **All views** can be overridden
- ✅ **All controllers** virtual methods
- ✅ **All services** replaceable
- ✅ **All events** extendable

### CLI
- ✅ **All templates** customizable
- ✅ **All commands** extensible
- ✅ **All generators** configurable

---

## 🎓 Developer Experience

### Before (Rigid Framework):
```csharp
// Can't change anything, must fork framework
public sealed class PermissionChecker { }
```

### After (Extensible Framework):
```csharp
// Can replace, decorate, extend - no fork needed
services.Decorate<IPermissionChecker, MyCustomPermissionChecker>();
```

**Result**: Developers stay in control, framework stays simple!

---

## 📅 Implementation Roadmap

### ✅ Already Implemented
- Partial class domain events
- Interface-based services
- Repository pattern with IQueryable
- Virtual methods in base classes

### 🔄 In Progress
- Options pattern for all modules
- Event hooks (UnitOfWork, EventBus)
- HTMX view override system

### ⏸️ Future
- CLI template customization
- Middleware extension points
- Plugin system for advanced scenarios

---

## 💡 Key Insight

**"Framework provides the 80%, developer customizes the 20%"**

NetMX gives you:
- Solid foundation (DDD, repositories, events)
- Best practices (HTMX patterns, observability)
- Productive tooling (CLI, templates)

But **you** control:
- Business logic
- Custom implementations
- UI/UX decisions
- Performance optimizations

**You're not locked in - you're empowered!** 🚀

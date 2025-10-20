# Phase 2: Essential Infrastructure (Months 1-3)

**Goal**: Reach 20% of ABP Framework feature parity  
**Focus**: Make NetMX viable for real production applications  
**Status**: 🚧 In Progress (Started: October 20, 2025)

---

## 🎯 Vision

Build the **essential infrastructure** needed for production applications:
- Authorization (permissions, roles, policies)
- Settings management (global, user, tenant-ready)
- Complete audit logging (entity changes, actions)
- Background jobs (scheduled tasks, queues)
- Caching (in-memory, distributed)

**Why Phase 2**: Phase 1 proved the architecture works. Phase 2 makes it production-ready.

---

## 📋 Phase 2 Modules

### 1. Authorization Module (CRITICAL) ⏳ In Progress

**Priority**: 🔥🔥🔥 **Must Have**  
**Effort**: 2 weeks  
**Status**: Structure created

#### Features
- [x] Module structure created
- [ ] Permission system
  - [ ] Permission definitions
  - [ ] Permission groups
  - [ ] Permission checker service
  - [ ] Dynamic permission loading
- [ ] Role-based access control (RBAC)
  - [ ] Role entity
  - [ ] User-role associations
  - [ ] Role-permission associations
  - [ ] Role hierarchy support
- [ ] Policy-based authorization
  - [ ] Custom policy handlers
  - [ ] Resource-based authorization
  - [ ] Claim-based policies
- [ ] Claims management
  - [ ] Custom claims
  - [ ] Claims transformation
  - [ ] Claims validation
- [ ] Authorization UI
  - [ ] Permission management page
  - [ ] Role management page
  - [ ] User permission assignment
  - [ ] HTMX-powered permission tree

#### Technical Approach
```csharp
// Permission Definition
public static class AppPermissions
{
    public const string Users_View = "Users.View";
    public const string Users_Create = "Users.Create";
    public const string Users_Edit = "Users.Edit";
    public const string Users_Delete = "Users.Delete";
}

// Permission Checker
public interface IPermissionChecker
{
    Task<bool> IsGrantedAsync(string permission);
    Task<bool> IsGrantedAsync(Guid userId, string permission);
}

// Usage in Controller
[Authorize(Policy = AppPermissions.Users_View)]
public async Task<IActionResult> Index()
{
    var canEdit = await _permissionChecker.IsGrantedAsync(AppPermissions.Users_Edit);
    // ...
}
```

#### CLI Integration
```bash
# Generate permission-aware feature
netmx generate feature Product -m Catalog --with-permissions

# Generates:
# - Product entity
# - ProductDto with permission checks
# - ProductService with [Authorize] attributes
# - ProductController with permission-based UI
```

---

### 2. Settings Module

**Priority**: 🔥🔥 **High**  
**Effort**: 1 week  
**Status**: Not started

#### Features
- [ ] Setting providers
  - [ ] Database provider
  - [ ] JSON file provider
  - [ ] Environment variable provider
  - [ ] Encrypted settings support
- [ ] Setting scopes
  - [ ] Global settings (application-wide)
  - [ ] User settings (per-user preferences)
  - [ ] Tenant settings (multi-tenancy ready)
- [ ] Setting definitions
  - [ ] Strongly-typed setting classes
  - [ ] Default values
  - [ ] Validation rules
  - [ ] Setting metadata
- [ ] Settings UI
  - [ ] Global settings management page
  - [ ] User preferences page
  - [ ] Setting search/filtering
  - [ ] HTMX real-time updates

#### Technical Approach
```csharp
// Setting Definition
public class AppSettings
{
    public const string MaxUploadSize = "App.MaxUploadSize";
    public const string MaintenanceMode = "App.MaintenanceMode";
}

// Setting Manager
public interface ISettingManager
{
    Task<T> GetAsync<T>(string name, T defaultValue = default);
    Task SetAsync(string name, object value);
    Task SetForUserAsync(Guid userId, string name, object value);
}

// Usage
var maxSize = await _settings.GetAsync<int>(AppSettings.MaxUploadSize, 10485760);
```

---

### 3. Audit Logging Module (Complete)

**Priority**: 🔥🔥🔥 **Must Have**  
**Effort**: 2 weeks  
**Status**: Empty scaffold exists, needs implementation

#### Features (Beyond Current Scaffold)
- [ ] Entity change tracking
  - [ ] Automatic change detection (EF Core interceptor)
  - [ ] Property-level changes (old value → new value)
  - [ ] Ignored properties (passwords, etc.)
  - [ ] Change aggregation
- [ ] Action audit logging
  - [ ] HTTP request logging
  - [ ] User action tracking
  - [ ] Exception logging
  - [ ] Performance metrics
- [ ] Audit log querying
  - [ ] Entity-specific audit trail
  - [ ] User activity history
  - [ ] Time-range filtering
  - [ ] Change comparison UI
- [ ] Retention policies
  - [ ] Automatic cleanup (90 days default)
  - [ ] Archive to BLOB storage
  - [ ] Configurable per entity type
- [ ] Audit log UI
  - [ ] Audit trail viewer
  - [ ] Change history comparison
  - [ ] Export to CSV/PDF
  - [ ] Real-time audit stream (HTMX)

#### Technical Approach
```csharp
// Automatic Entity Change Tracking
public class AuditingInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        var auditEntries = CreateAuditEntries(context);
        await _auditLogRepository.InsertManyAsync(auditEntries);
        return await base.SavingChangesAsync(...);
    }
}

// Action Audit Logging
[AuditLog] // Attribute for explicit logging
public async Task<IActionResult> DeleteUser(Guid id)
{
    // Automatically logged: user, action, timestamp, result
}

// Audit Trail Query
var userChanges = await _auditLogService.GetEntityChangesAsync<User>(userId);
// Returns: List of all changes to this user
```

---

### 4. Background Jobs Module

**Priority**: 🔥🔥 **High**  
**Effort**: 1 week  
**Status**: Not started

#### Features
- [ ] Job scheduling
  - [ ] Hangfire integration
  - [ ] One-time jobs
  - [ ] Recurring jobs (cron expressions)
  - [ ] Delayed jobs
- [ ] Job monitoring
  - [ ] Job dashboard
  - [ ] Job history
  - [ ] Failed job tracking
  - [ ] Performance metrics
- [ ] Job management
  - [ ] Queue management
  - [ ] Priority queues
  - [ ] Job retry policies
  - [ ] Dead letter queue
- [ ] Built-in jobs
  - [ ] Email sending job
  - [ ] Cleanup job (old audit logs)
  - [ ] Report generation job
  - [ ] Data export job

#### Technical Approach
```csharp
// Job Definition
public class SendEmailJob : IBackgroundJob<SendEmailJobArgs>
{
    public async Task ExecuteAsync(SendEmailJobArgs args)
    {
        await _emailService.SendAsync(args.To, args.Subject, args.Body);
    }
}

// Enqueue Job
await _backgroundJobManager.EnqueueAsync<SendEmailJob>(new SendEmailJobArgs
{
    To = "user@example.com",
    Subject = "Welcome!",
    Body = "Thanks for signing up!"
});

// Recurring Job
RecurringJob.AddOrUpdate<CleanupJob>(
    "cleanup-old-logs",
    x => x.ExecuteAsync(),
    Cron.Daily
);
```

---

### 5. Caching Module

**Priority**: 🔥🔥 **High**  
**Effort**: 1 week  
**Status**: Not started

#### Features
- [ ] Cache providers
  - [ ] In-memory cache (IMemoryCache)
  - [ ] Distributed cache (Redis)
  - [ ] Hybrid cache (local + distributed)
- [ ] Cache patterns
  - [ ] Cache-aside
  - [ ] Write-through
  - [ ] Read-through
  - [ ] Refresh-ahead
- [ ] Cache management
  - [ ] Cache key management
  - [ ] TTL strategies
  - [ ] Cache invalidation
  - [ ] Cache stampede prevention
- [ ] Cache monitoring
  - [ ] Hit/miss rate
  - [ ] Memory usage
  - [ ] Eviction tracking

#### Technical Approach
```csharp
// Distributed Cache
public interface IDistributedCache<TItem>
{
    Task<TItem?> GetOrAddAsync(string key, Func<Task<TItem>> factory, TimeSpan? expiration = null);
    Task SetAsync(string key, TItem value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}

// Usage with Auto-Invalidation
[CacheOutput(Duration = 300)] // Cache for 5 minutes
public async Task<List<ProductDto>> GetAllProductsAsync()
{
    return await _productService.GetAllAsync();
}

[InvalidateCache(nameof(GetAllProductsAsync))]
public async Task UpdateProductAsync(UpdateProductDto dto)
{
    // Cache automatically invalidated after update
}
```

---

## 🎯 Success Criteria (Phase 2 Complete)

### Must Have ✅
- [ ] Authorization system working (permissions, roles, policies)
- [ ] Settings management (global, user scopes)
- [ ] Complete audit logging (entity changes + actions)
- [ ] Background jobs (Hangfire integrated)
- [ ] Caching (in-memory + Redis)
- [ ] All modules documented
- [ ] All modules tested (80%+ coverage)
- [ ] CLI generates permission-aware code
- [ ] Example app using all modules

### Quality Gates
- [ ] Zero warnings in all builds
- [ ] 80%+ test coverage per module
- [ ] All public APIs documented (XML comments)
- [ ] Performance benchmarks passed
- [ ] Security audit passed

---

## 📊 Phase 2 Timeline

```
Week 1-2: Authorization Module
├─ Week 1: Permission system, RBAC
└─ Week 2: Policy-based auth, UI

Week 3: Settings Module
└─ All setting providers + UI

Week 4-5: Audit Logging (Complete)
├─ Week 4: Entity change tracking
└─ Week 5: Action logging, UI

Week 6: Background Jobs
└─ Hangfire integration + monitoring

Week 7: Caching
└─ All cache providers + patterns

Week 8-9: Integration & Testing
├─ Week 8: Integration testing
└─ Week 9: Documentation, examples

Week 10-12: Buffer (Polish & Issues)
```

**Target Completion**: January 20, 2026 (3 months)

---

## 🚀 Getting Started (For Contributors)

### Prerequisites
- .NET 9 SDK
- PostgreSQL (or Docker)
- Redis (optional, for distributed cache)
- Hangfire Dashboard (included)

### Development Workflow

1. **Pick a module** from Phase 2 list
2. **Create feature branch**: `git checkout -b feature/authorization-permissions`
3. **Generate entities**: `netmx generate feature Permission -m Authorization`
4. **Implement business logic**
5. **Write tests** (xUnit)
6. **Update documentation**
7. **Submit PR** to develop branch

### Module Development Guidelines

See [CONTRIBUTING.md](../CONTRIBUTING.md) for:
- Code style
- Testing requirements
- Documentation standards
- PR process

---

## 📚 Resources

### Similar Frameworks (For Inspiration)
- [ABP Framework](https://abp.io) - Our benchmark
- [Orchard Core](https://orchardcore.net) - CMS patterns
- [NopCommerce](https://www.nopcommerce.com) - E-commerce patterns
- [ASP.NET Boilerplate](https://aspnetboilerplate.com) - ABP predecessor

### Technical References
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [HTMX Documentation](https://htmx.org)
- [Hangfire Documentation](https://docs.hangfire.io)

---

## 🎉 Join Us!

Phase 2 is where NetMX becomes production-ready. We need contributors for:
- ✅ Module development
- ✅ Testing & QA
- ✅ Documentation
- ✅ Examples & tutorials
- ✅ Performance optimization

**Let's build something great together!**

---

**Last Updated**: October 20, 2025  
**Phase Status**: 🚧 Week 1 - Authorization Module  
**Next Milestone**: Authorization RBAC complete (Week 2)

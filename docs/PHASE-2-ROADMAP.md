# Phase 2: Essential Infrastructure (Months 1-3)

**Goal**: Reach 20% of ABP Framework feature parity  
**Focus**: Make NetMX viable for real production applications  
**Status**: 🚧 In Progress (Started: October 20, 2025)

---

## 🎯 Vision

Build the **essential infrastructure** needed for production applications with **observability-first approach**:
- Authorization (permissions, roles, policies)
- Settings management (global, user, tenant-ready)
- Complete audit logging (entity changes, actions)
- **Observability & Monitoring** (metrics, tracing, health checks)
- **Testing Infrastructure** (unit, integration, E2E)

**Platform Strategy**: 
- **Free Tier**: Core framework + essential modules (Identity, Auth, Settings, Basic Audit)
- **Standard Tier**: Advanced modules (Multi-tenancy, Jobs, Cache, Email, BLOB, CMS)
- **Enterprise Tier**: Observability dashboards, distributed tracing, analytics, premium support

**Why Phase 2**: Phase 1 proved the architecture works. Phase 2 makes it production-ready with enterprise-grade observability.

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

### 5. Observability & Monitoring Module (FREE - Core Feature)

**Priority**: 🔥🔥🔥 **Must Have** (Built into every module)  
**Effort**: 2 weeks  
**Status**: Not started

#### Why Observability Is Core
- ✅ Production-ready requires visibility
- ✅ Debug issues faster
- ✅ Performance optimization
- ✅ Enterprise users demand it
- ✅ Competitive advantage over ABP

#### Features
- [ ] Health checks
  - [ ] Liveness checks (is service alive?)
  - [ ] Readiness checks (can handle traffic?)
  - [ ] Database connectivity
  - [ ] External dependency checks
  - [ ] Health check UI (HTMX dashboard)
- [ ] Metrics (Prometheus format)
  - [ ] Request/response metrics
  - [ ] Database query metrics
  - [ ] Cache hit/miss rates
  - [ ] Business metrics (e.g., users created, orders placed)
  - [ ] Custom metric decorators
- [ ] Distributed tracing (OpenTelemetry)
  - [ ] Request tracing across services
  - [ ] Database query tracing
  - [ ] External API call tracing
  - [ ] Trace correlation
  - [ ] Jaeger/Zipkin integration
- [ ] Structured logging (Serilog)
  - [ ] Request/response logging
  - [ ] Exception logging with context
  - [ ] Audit action logging
  - [ ] Log enrichment (user, tenant, correlation)
  - [ ] Multiple sinks (Console, File, Seq, Elasticsearch)
- [ ] Performance monitoring
  - [ ] Slow query detection
  - [ ] Memory usage tracking
  - [ ] CPU usage tracking
  - [ ] Request duration percentiles (p50, p95, p99)

#### Technical Approach
```csharp
// Health Checks (Built-in)
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<CustomHealthCheck>("custom");

// Metrics (Prometheus)
[Metrics("netmx.users")]
public class UserService
{
    [CounterMetric("users.created")]
    public async Task<User> CreateAsync(CreateUserDto dto)
    {
        // Automatically increments counter
    }
    
    [DurationMetric("users.query.duration")]
    public async Task<List<User>> GetAllAsync()
    {
        // Automatically records duration
    }
}

// Distributed Tracing (OpenTelemetry)
using var activity = ActivitySource.StartActivity("CreateUser");
activity?.SetTag("user.email", dto.Email);
activity?.SetTag("user.role", dto.RoleName);

// Structured Logging (Serilog)
_logger.LogInformation(
    "User {UserId} created by {CreatorId} with role {Role}",
    user.Id, _currentUser.Id, dto.RoleName);
```

#### Observability UI (FREE)
- [ ] Health check dashboard (`/health-ui`)
- [ ] Metrics endpoint (`/metrics` - Prometheus format)
- [ ] Basic request logging view
- [ ] Error log viewer

#### Enterprise Observability (PAID - Enterprise Tier)
- [ ] Real-time metrics dashboard
- [ ] Custom dashboards (Grafana-like)
- [ ] Alert configuration UI
- [ ] Performance insights & recommendations
- [ ] Automatic anomaly detection
- [ ] Cost analysis (cloud resource usage)

---

### 6. Testing Infrastructure (FREE - Core Feature)

**Priority**: 🔥🔥🔥 **Must Have**  
**Effort**: 2 weeks  
**Status**: Not started

#### Features
- [ ] Unit testing helpers
  - [ ] Test base classes
  - [ ] Mock repository builder
  - [ ] Test data builders
  - [ ] In-memory database setup
- [ ] Integration testing
  - [ ] WebApplicationFactory setup
  - [ ] Database seeding
  - [ ] Authentication helpers
  - [ ] HTMX response assertions
- [ ] E2E testing (Playwright)
  - [ ] Page object models
  - [ ] HTMX interaction helpers
  - [ ] Screenshot on failure
  - [ ] Test data cleanup
- [ ] Performance testing
  - [ ] Load testing templates (k6)
  - [ ] Benchmark helpers
  - [ ] Performance regression detection
- [ ] CLI testing support
  - [ ] `netmx generate test Feature -m Module`
  - [ ] Generates unit + integration tests
  - [ ] Includes test data builders

#### Technical Approach
```csharp
// Unit Test with Helpers
public class UserServiceTests : NetMXTestBase
{
    [Fact]
    public async Task CreateUser_ShouldSucceed()
    {
        // Arrange
        var service = GetRequiredService<IUserService>();
        var dto = new CreateUserDtoBuilder()
            .WithEmail("test@test.com")
            .Build();
        
        // Act
        var user = await service.CreateAsync(dto);
        
        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be("test@test.com");
    }
}

// Integration Test
public class UsersControllerTests : NetMXWebApplicationTest
{
    [Fact]
    public async Task GetUsers_ShouldReturnHtmxPartial()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");
        
        // Act
        var response = await client.GetAsync("/Users");
        
        // Assert
        response.Should().BeSuccessful();
        response.Should().HaveHeader("HX-Trigger");
    }
}

// E2E Test (Playwright)
[Test]
public async Task UserCanLogin()
{
    await Page.GotoAsync("/login");
    await Page.FillAsync("#email", "admin@test.com");
    await Page.FillAsync("#password", "Test123!");
    await Page.ClickAsync("button[type=submit]");
    
    await Expect(Page).ToHaveURLAsync("/dashboard");
}
```

---

### 7. Background Jobs Module (STANDARD TIER - Paid)

**Priority**: 🔥🔥 **High**  
**Effort**: 1 week  
**Status**: Not started
**Tier**: 💰 **Standard** ($99/month or $999/year)

#### Why Paid?
- Advanced feature for production apps
- Requires Hangfire Pro license (we absorb cost)
- Ongoing maintenance & support
- Enterprise job monitoring features

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

# NetMX Development Guidelines

This file provides context for GitHub Copilot when working with the NetMX framework.

## Architecture Overview

NetMX is a modular, HTMX-first framework for building web applications with ASP.NET Core.

### Core Principles

1. **Pure Framework** - The framework itself (`framework/`) contains zero features, only infrastructure
2. **Everything Optional** - All features are optional modules in `modules/`
3. **HTMX-First** - Server-rendered HTML with HTMX for interactivity, no heavy JS frameworks
4. **Event-Driven Components** - Use HTMX events for component communication (monolith-first, extensible to distributed)
5. **Domain-Driven Design** - Clean architecture with clear separation of concerns

### Directory Structure

```
framework/          # Core infrastructure packages (DDD, EF Core, ASP.NET extensions)
modules/            # Optional feature modules (Identity, CMS, Audit, etc.)
templates/          # Starter templates using different combinations of modules
  modular/          # Modular monolith template (minimal by default)
  pro/              # Future: SaaS/multi-tenant template
  src/              # Future: Microservices template
tools/              # CLI and other development tools
```

## Development Workflow

### Before Every Commit

1. **Build the entire solution** - Ensure no compilation errors
   ```bash
   dotnet build framework/NetMX.sln
   ```

2. **Run tests** - Verify all tests pass (when tests exist)
   ```bash
   dotnet test
   ```

3. **Check for errors** - Use IDE error checking or `dotnet build` output

### When Creating New Modules

1. Place module in `modules/<ModuleName>/`
2. Follow 4-layer structure:
   - `<Module>.Core` - Domain entities, value objects
   - `<Module>.Contracts` - DTOs, interfaces for application services
   - `<Module>.Application` - Application services, use cases
   - `<Module>.Web` - Controllers, views (Razor class library)

3. Reference the Identity module as a template

### When Creating New Features

1. **Start with the domain** - Entities and aggregates in Core layer
2. **Define contracts** - DTOs and service interfaces in Contracts layer
3. **Implement application logic** - Services in Application layer
4. **Build UI** - Controllers and views in Web layer with HTMX

## HTMX Patterns

### Views: Raw HTMX Attributes

Keep Razor views clean with standard HTMX syntax:

```html
<button hx-delete="/api/users/@user.Id" 
        hx-target="#user-row-@user.Id"
        hx-swap="outerHTML"
        hx-confirm="Are you sure?">
    Delete
</button>
```

### Controllers: Strongly-Typed Helpers

Use `NetMX.Htmx` package for type safety:

```csharp
using NetMX.Htmx;

[HttpDelete("/api/users/{id}")]
public IActionResult Delete(Guid id)
{
    _userService.Delete(id);
    
    // Strongly typed, IntelliSense-friendly
    HtmxResponse.Trigger(this, "userDeleted", new { userId = id });
    HtmxResponse.Reswap(this, HtmxSwap.Delete);
    
    return Ok();
}
```

### Event-Driven Components

Use HTMX events for loose coupling between components:

**Trigger from controller:**
```csharp
HtmxResponse.Trigger(this, "user:created", new { 
    userId = newUser.Id 
});
```

**Listen in view:**
```html
<div hx-get="/api/stats" 
     hx-trigger="user:created from:body">
    <!-- Auto-refreshes when user created -->
</div>
```

### Partial vs Full Responses

Check if request is from HTMX to return appropriate response:

```csharp
public IActionResult Index()
{
    var users = _userService.GetAll();
    
    if (Request.IsHtmx())
    {
        return PartialView("_UserList", users);  // Just the content
    }
    
    return View(users);  // Full page with layout
}
```

## Database & Migrations

### Using EF Core Migrations

Always use migrations for database changes:

```bash
# Add migration (from NetMXApp.Web directory)
dotnet ef migrations add MigrationName

# Apply migrations (automatic in dev via Program.cs)
dotnet ef database update
```

### DbContext Guidelines

1. Inherit from `NetMXDbContext<TContext>` to get:
   - Soft delete filtering
   - Multi-tenancy support
   - Concurrency checking
   - Audit logging integration

2. Configure entities explicitly:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<MyEntity>(b =>
    {
        b.ToTable("MyEntities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
    });
}
```

## Dependency Injection

### Automatic Registration

Use marker interfaces for automatic DI registration:

```csharp
public class MyService : ITransientDependency  // Auto-registered as transient
public class MyService : IScopedDependency     // Auto-registered as scoped
public class MyService : ISingletonDependency  // Auto-registered as singleton
```

### Repository Pattern

Use `IQueryableRepository<TEntity, TKey>` for data access:

```csharp
public class UserAppService
{
    private readonly IQueryableRepository<AppUser, Guid> _userRepository;
    
    public UserAppService(IQueryableRepository<AppUser, Guid> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<List<UserDto>> GetAllAsync()
    {
        return await _userRepository.AsQueryable()
            .Where(x => !x.IsDeleted)
            .Select(x => new UserDto { /* ... */ })
            .ToListAsync();
    }
}
```

## Testing Strategy

### Unit Tests

- Test application services with mocked repositories
- Test domain logic in isolation
- Use xUnit as the testing framework

### Integration Tests

- Test full HTTP request/response pipeline
- Use `WebApplicationFactory<TProgram>`
- Test HTMX interactions with response headers

## Documentation Standards

### README Files

Every module and package should have a README.md with:
- Overview of the module/package
- Key features
- Usage examples
- Integration instructions

### Code Comments

- Use XML documentation comments for public APIs
- Explain "why" not "what" in implementation comments
- Document non-obvious behavior

### Architecture Decisions

Document significant architectural decisions in `/docs/` folder with:
- Context - Why was this decision needed?
- Decision - What did we decide?
- Consequences - What are the implications?

## Package Versioning

### Current Versions (as of 2025-01-19)

- .NET: 9.0 (LTS)
- EF Core: 9.0.10
- Npgsql: 9.0.2
- HTMX: 2.0.4 (via LibMan)
- Bulma: 1.0.4 (via LibMan)

### Updating Packages

1. Update all related packages together (e.g., all EF Core packages)
2. Test thoroughly after updates
3. Update this file with new versions

## Common Patterns

### Creating a New Entity

```csharp
public class MyEntity : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    
    private MyEntity() { } // EF Core
    
    public MyEntity(Guid id, string name) : base(id)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
    }
    
    public void UpdateName(string name)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
    }
}
```

### Creating a DTO

```csharp
public class MyEntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

### Creating an Application Service

```csharp
public class MyEntityAppService : IScopedDependency
{
    private readonly IQueryableRepository<MyEntity, Guid> _repository;
    
    public MyEntityAppService(IQueryableRepository<MyEntity, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<MyEntityDto> GetAsync(Guid id)
    {
        var entity = await _repository.FirstOrDefaultAsync(x => x.Id == id);
        return ObjectMapper.Map<MyEntity, MyEntityDto>(entity);
    }
}
```

## Troubleshooting

### Common Issues

1. **Views not found in module** - Razor class libraries require special configuration for view discovery
2. **Migration conflicts** - Always pull latest before creating migrations
3. **Connection string issues** - Ensure PostgreSQL container is running

### Getting Help

- Check module READMEs for specific guidance
- Look at Identity module as reference implementation
- Review framework package documentation

## CLI Usage

### Creating New Projects

```bash
netmx new modular MyApp --output ./MyApp
```

### Scaffolding Modules

```bash
netmx add module Identity
```

### Generating CRUD

```bash
netmx generate crud User --module Identity
```

## Performance Considerations

### Database

- Use async methods everywhere
- Avoid N+1 queries - use `.Include()` for related data
- Use projections (`.Select()`) instead of loading full entities
- Index frequently queried columns

### HTMX

- Use `hx-indicator` for loading states
- Implement proper caching headers
- Use `hx-boost` for progressive enhancement
- Consider `hx-trigger` modifiers (`delay:500ms`, `throttle:1s`)

## Security

### Input Validation

- Validate in DTOs using data annotations
- Validate in domain entities
- Use guard clauses for defensive programming

### CSRF Protection

- ASP.NET Core provides CSRF protection automatically
- HTMX includes anti-forgery tokens in requests

### SQL Injection

- Use EF Core parameterized queries (automatic)
- Never concatenate SQL strings

## Future Roadmap

### Phase 1 (Current): Modular Monolith
- ✅ Framework SDK
- ✅ Identity module
- 🔄 HTMX helpers package
- 🔄 CLI scaffolding

### Phase 2: Enhanced Modules
- Audit logging
- Background jobs
- File storage
- Email/notifications

### Phase 3: Distributed Capabilities
- SignalR/SSE for real-time events
- Message bus integration
- Multi-tenant isolation
- API gateway support

### Phase 4: Developer Experience
- Visual Studio templates
- Hot reload for modules
- Admin dashboard generator
- API documentation generator

---

**Remember**: Build before commit, test thoroughly, and keep the framework pure!

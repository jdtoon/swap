# Event Registry: Multi-Architecture Support

**How Event Registry works across all NetMX templates**

---

## 🎯 Three Architecture Patterns

### 1. Basic Monolith (Single Project)

**Structure**:
```
MyApp/
  ├─ MyApp.csproj
  ├─ Program.cs
  ├─ Events/
  │   └─ AppEventDefinitions.cs
  ├─ Controllers/
  │   ├─ ProductController.cs
  │   └─ OrderController.cs
  └─ Views/
```

**Event Registration** (Program.cs):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register event registry
builder.Services.AddSingleton<IEventRegistry, EventRegistry>();

var app = builder.Build();

// Register app events at startup
var registry = app.Services.GetRequiredService<IEventRegistry>();
foreach (var e in AppEventDefinitions.AllEvents)
    registry.RegisterEvent(e.Name, e);

Events.Initialize(registry); // Global access

app.Run();
```

**Usage**:
```csharp
// In any controller
this.HxTrigger(Events.Get("product.created"), new { id });

// In any view
<div hx-trigger="@Events.Get("product.created") from:body">
```

✅ **Works!** Single project, single event definitions file, simple.

---

### 2. Modular Monolith (Multiple Projects, Single Deployment)

**Structure**:
```
MyApp/
  ├─ MyApp.Web/ (Host project)
  │   ├─ Program.cs
  │   └─ appsettings.json
  │
  ├─ modules/
  │   ├─ Authorization/
  │   │   └─ Authorization.Web/
  │   │       ├─ Events/AuthorizationEventDefinitions.cs
  │   │       └─ AuthorizationWebModule.cs
  │   │
  │   ├─ Identity/
  │   │   └─ Identity.Web/
  │   │       ├─ Events/IdentityEventDefinitions.cs
  │   │       └─ IdentityWebModule.cs
  │   │
  │   └─ Audit/
  │       └─ Audit.Web/
  │           ├─ Events/AuditEventDefinitions.cs
  │           └─ AuditWebModule.cs
  │
  └─ framework/
      └─ NetMX.Events/ (Registry core)
```

**Event Registration** (Program.cs):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register event registry (ONCE)
builder.Services.AddSingleton<IEventRegistry, EventRegistry>();

// Add modules
builder.Services.AddAuthorizationModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);

var app = builder.Build();

// Initialize global Events class
var registry = app.Services.GetRequiredService<IEventRegistry>();
Events.Initialize(registry);

// Each module registers its events
app.UseAuthorizationModule(); // Registers permission.*, role.*
app.UseIdentityModule();      // Registers login.*, registration.*
app.UseAuditModule();         // Registers auditlog.*, entry.*

// Validate no duplicates across all modules
registry.ValidateUniqueness();

app.Run();
```

**Module Registration** (Authorization.Web/AuthorizationWebModule.cs):
```csharp
public static IApplicationBuilder UseAuthorizationModule(this IApplicationBuilder app)
{
    var registry = app.ApplicationServices.GetRequiredService<IEventRegistry>();
    
    foreach (var eventMetadata in AuthorizationEventDefinitions.AllEvents)
    {
        registry.RegisterEvent(eventMetadata.Name, eventMetadata);
    }
    
    return app;
}
```

**Usage** (Same as basic monolith!):
```csharp
// In Authorization module
this.HxTrigger(Events.Get("permission.created"), new { id });

// In Audit module (NO reference to Authorization!)
<div hx-trigger="@Events.Get("permission.created") from:body">
```

✅ **Works!** Multiple modules, single deployment, no coupling between modules.

---

### 3. Microservices (Multiple Services, Distributed)

**Structure**:
```
NetMX.Events.Contracts/ (Shared NuGet package)
  └─ GlobalEventNames.cs (Just constants, no logic)

AuthorizationService/
  ├─ Program.cs
  ├─ Events/AuthorizationEventDefinitions.cs
  └─ Controllers/

IdentityService/
  ├─ Program.cs
  ├─ Events/IdentityEventDefinitions.cs
  └─ Controllers/

AuditService/
  ├─ Program.cs
  └─ Controllers/ (Subscribes to other services' events)
```

**Shared Contracts** (NetMX.Events.Contracts/GlobalEventNames.cs):
```csharp
namespace NetMX.Events.Contracts;

/// <summary>
/// Global event names shared across all microservices.
/// This is a CONTRACT - changes must be coordinated!
/// </summary>
public static class GlobalEventNames
{
    // Authorization service events
    public const string PermissionCreated = "permission.created";
    public const string PermissionUpdated = "permission.updated";
    public const string PermissionDeleted = "permission.deleted";
    
    public const string RoleCreated = "role.created";
    public const string RoleUpdated = "role.updated";
    public const string RoleDeleted = "role.deleted";
    
    // Identity service events
    public const string LoginSuccess = "login.success";
    public const string LoginFailed = "login.failed";
    public const string RegistrationSuccess = "registration.success";
    
    // Audit service events
    public const string AuditLogCreated = "auditlog.created";
    
    // ... more events
}
```

**Event Registry in Each Service** (AuthorizationService/Program.cs):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register event registry (in-memory for service's own events)
builder.Services.AddSingleton<IEventRegistry, EventRegistry>();

// Register event bus (RabbitMQ/Kafka adapter)
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

var app = builder.Build();

// Register this service's events
var registry = app.Services.GetRequiredService<IEventRegistry>();
foreach (var e in AuthorizationEventDefinitions.AllEvents)
    registry.RegisterEvent(e.Name, e);

Events.Initialize(registry);

app.Run();
```

**Publishing Events** (Same code as monolith!):
```csharp
// AuthorizationService/Controllers/PermissionsController.cs
[HttpPost]
public async Task<IActionResult> Create(CreatePermissionDto dto)
{
    var permission = await _service.CreateAsync(dto);
    
    // Publishes to RabbitMQ/Kafka (NOT in-process!)
    await _eventBus.PublishAsync(
        GlobalEventNames.PermissionCreated, 
        new { id = permission.Id });
    
    return Ok();
}
```

**Subscribing to Events** (AuditService):
```csharp
// AuditService/EventHandlers/PermissionEventHandler.cs
public class PermissionEventHandler : IEventHandler<PermissionCreatedEvent>
{
    public async Task HandleAsync(PermissionCreatedEvent @event)
    {
        // Audit service captures Authorization service event
        await _auditService.LogAsync(new AuditEntry
        {
            EventName = GlobalEventNames.PermissionCreated,
            UserId = @event.UserId,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

✅ **Works!** Each service registers its own events, publishes to message broker, subscribes to others' events via contracts package.

---

## 🔄 Key Insight: Same Abstraction, Different Transport

### IEventBus Abstraction

```csharp
namespace NetMX.Events;

public interface IEventBus
{
    Task PublishAsync(string eventName, object? data = null, EventContext? context = null);
}
```

### Three Implementations

**1. InProcessEventBus** (Basic Monolith):
```csharp
public class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _services;
    
    public async Task PublishAsync(string eventName, object? data, EventContext? context)
    {
        // Find all handlers for this event (in-memory)
        var handlers = _services.GetServices<IEventHandler>()
            .Where(h => h.CanHandle(eventName));
        
        foreach (var handler in handlers)
            await handler.HandleAsync(eventName, data);
    }
}
```

**2. InProcessEventBus** (Modular Monolith - Same!):
```csharp
// Exact same implementation!
// Handlers can be in different modules, but still in-process
```

**3. RabbitMqEventBus** (Microservices):
```csharp
public class RabbitMqEventBus : IEventBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    
    public async Task PublishAsync(string eventName, object? data, EventContext? context)
    {
        // Serialize event
        var message = JsonSerializer.Serialize(new
        {
            EventName = eventName,
            Data = data,
            Context = context,
            Timestamp = DateTime.UtcNow
        });
        
        var body = Encoding.UTF8.GetBytes(message);
        
        // Publish to RabbitMQ exchange
        _channel.BasicPublish(
            exchange: "netmx.events",
            routingKey: eventName,
            basicProperties: null,
            body: body);
        
        await Task.CompletedTask;
    }
}
```

### Application Code Doesn't Change!

```csharp
// This code works in ALL three architectures!
public class ProductController : Controller
{
    private readonly IEventBus _eventBus;
    
    public ProductController(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        
        // Same code, different transport based on DI registration!
        await _eventBus.PublishAsync("product.created", new { id = product.Id });
        
        return Ok();
    }
}
```

**Magic**: Just change DI registration in `Program.cs`:
- Monolith: `services.AddSingleton<IEventBus, InProcessEventBus>()`
- Microservices: `services.AddSingleton<IEventBus, RabbitMqEventBus>()`

---

## 🧪 Testing Strategy

### 1. Unit Tests (Event Registry Core)

Test the registry itself:

```csharp
public class EventRegistryTests
{
    [Fact]
    public void RegisterEvent_UniqueEvent_Succeeds()
    {
        var registry = new EventRegistry();
        
        var metadata = new EventMetadata
        {
            Name = "test.event",
            Module = "Test",
            Category = "Test"
        };
        
        registry.RegisterEvent(metadata.Name, metadata);
        
        Assert.True(registry.IsRegistered("test.event"));
    }
    
    [Fact]
    public void RegisterEvent_DuplicateEvent_Throws()
    {
        var registry = new EventRegistry();
        
        var metadata1 = new EventMetadata { Name = "test.event", Module = "Module1" };
        var metadata2 = new EventMetadata { Name = "test.event", Module = "Module2" };
        
        registry.RegisterEvent(metadata1.Name, metadata1);
        
        var ex = Assert.Throws<InvalidOperationException>(
            () => registry.RegisterEvent(metadata2.Name, metadata2));
        
        Assert.Contains("already registered", ex.Message);
    }
    
    [Fact]
    public void GetEvent_RegisteredEvent_ReturnsMetadata()
    {
        var registry = new EventRegistry();
        var metadata = new EventMetadata { Name = "test.event", Module = "Test" };
        
        registry.RegisterEvent(metadata.Name, metadata);
        var result = registry.GetEvent("test.event");
        
        Assert.Equal("test.event", result.Name);
        Assert.Equal("Test", result.Module);
    }
    
    [Fact]
    public void GetEvent_UnregisteredEvent_Throws()
    {
        var registry = new EventRegistry();
        
        Assert.Throws<KeyNotFoundException>(
            () => registry.GetEvent("nonexistent.event"));
    }
}
```

### 2. Integration Tests (Module Registration)

Test that modules register correctly:

```csharp
public class AuthorizationModuleIntegrationTests
{
    [Fact]
    public async Task UseAuthorizationModule_RegistersAllEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEventRegistry, EventRegistry>();
        
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);
        
        // Act
        app.UseAuthorizationModule();
        
        // Assert
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        
        Assert.True(registry.IsRegistered("permission.created"));
        Assert.True(registry.IsRegistered("permission.updated"));
        Assert.True(registry.IsRegistered("permission.deleted"));
        Assert.True(registry.IsRegistered("role.created"));
        Assert.True(registry.IsRegistered("role.updated"));
        Assert.True(registry.IsRegistered("role.deleted"));
    }
    
    [Fact]
    public void UseAuthorizationModule_EventsHaveCorrectMetadata()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddSingleton<IEventRegistry, EventRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);
        app.UseAuthorizationModule();
        
        // Assert
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var metadata = registry.GetEvent("permission.created");
        
        Assert.Equal("Authorization", metadata.Module);
        Assert.Equal("Permission", metadata.Category);
        Assert.Equal(EventDirection.Upstream, metadata.Direction);
    }
}
```

### 3. End-to-End Tests (Full Stack)

Test that events work across modules:

```csharp
public class CrossModuleEventTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public CrossModuleEventTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task CreatePermission_TriggersEvent_AuditCapturesIt()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act: Create permission in Authorization module
        var response = await client.PostAsJsonAsync("/Permission/Create", new
        {
            Name = "Test.Permission",
            DisplayName = "Test Permission"
        });
        
        response.EnsureSuccessStatusCode();
        
        // Assert: Check that Audit module captured the event
        var auditResponse = await client.GetAsync("/AuditLog/List");
        var auditLogs = await auditResponse.Content.ReadFromJsonAsync<AuditLogDto[]>();
        
        Assert.Contains(auditLogs, log => log.EventName == "permission.created");
    }
}
```

---

## 📋 Implementation Checklist

### Phase 1: Core Registry (Starting Now!)

- [ ] Create `IEventRegistry` interface in NetMX.Events
- [ ] Create `EventRegistry` implementation
- [ ] Create `EventMetadata` record
- [ ] Create `Events` static class with `Initialize()` and `Get()`
- [ ] Write 10+ unit tests for EventRegistry
- [ ] Run tests: `dotnet test`

### Phase 2: Module Integration

- [ ] Create `AuthorizationEventDefinitions.cs`
- [ ] Update `AuthorizationWebModule.cs` to register events
- [ ] Write integration tests for Authorization module
- [ ] Repeat for Identity module
- [ ] Repeat for Audit module
- [ ] Test cross-module event access

### Phase 3: CLI Generator

- [ ] Update `GenerateFeatureCommand.cs` to generate `*EventDefinitions.cs`
- [ ] Test: Generate feature and verify events registered
- [ ] Validate event names follow convention

### Phase 4: Documentation

- [ ] Update QUICK-START.md
- [ ] Update CLI documentation
- [ ] Update copilot-instructions.md

---

## ✅ Approval Confirmed

**Architecture**: Event Registry  
**Support**: All three patterns (Basic Monolith, Modular Monolith, Microservices)  
**Testing**: Unit → Integration → E2E  
**Next**: Start implementation with Phase 1 (Core Registry)

Let's build this! 🚀

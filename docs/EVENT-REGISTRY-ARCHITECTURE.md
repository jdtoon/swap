# Event Registry Architecture - Global Event Access Without Coupling

**Date**: October 22, 2025  
**Problem**: Need global event access without project references between modules  
**Solution**: Centralized event registry with distributed definitions

---

## 🎯 Requirements

Based on your architecture needs:

1. ✅ **Global Access** - Any module can reference any event by name
2. ✅ **No Module Coupling** - Modules don't reference each other
3. ✅ **Type Safety** - IntelliSense support, compile-time checking
4. ✅ **CLI Scaffolding** - Easy code generation
5. ✅ **Microservices Ready** - Events as service contracts
6. ✅ **Collision Detection** - Prevent duplicate event names across modules
7. ✅ **Runtime Discovery** - New modules register events dynamically

---

## 💡 Solution: Event Registry Pattern

### Core Concept

**Centralized registry, distributed definitions**

1. Each module **defines its own events** (isolated)
2. Events are **registered at startup** (runtime discovery)
3. All modules **query the registry** to access events (global access)
4. Registry **validates uniqueness** (collision detection)

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    NetMX.Events (Core)                       │
│                                                              │
│  ┌──────────────────────────────────────────┐              │
│  │         IEventRegistry                    │              │
│  │  - RegisterEvent(name, metadata)          │              │
│  │  - GetEvent(name) → EventMetadata         │              │
│  │  - GetAllEvents() → EventMetadata[]       │              │
│  │  - ValidateUniqueness()                   │              │
│  └──────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ Registers at startup
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│Authorization │  │   Identity   │  │    Audit     │
│              │  │              │  │              │
│  Events:     │  │  Events:     │  │  Events:     │
│  permission.*│  │  login.*     │  │  auditlog.*  │
│  role.*      │  │  register.*  │  │  entry.*     │
│              │  │              │  │              │
│ Registers:   │  │ Registers:   │  │ Registers:   │
│ at startup   │  │ at startup   │  │ at startup   │
└──────────────┘  └──────────────┘  └──────────────┘

┌─────────────────────────────────────────────────────────────┐
│                  YourApp.Web (Consumes)                      │
│                                                              │
│  Uses events via registry:                                  │
│  - Events.Get("permission.created")  ← String-based access  │
│  - Events.Permission.Created         ← Type-safe access     │
│                                                              │
│  NO project references to Authorization/Identity/Audit!     │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation

### 1. Core Registry (NetMX.Events Package)

**IEventRegistry.cs**:
```csharp
namespace NetMX.Events;

/// <summary>
/// Central registry for all events in the application.
/// Modules register their events at startup.
/// </summary>
public interface IEventRegistry
{
    /// <summary>
    /// Register an event in the registry.
    /// </summary>
    void RegisterEvent(string name, EventMetadata metadata);
    
    /// <summary>
    /// Get event metadata by name.
    /// </summary>
    EventMetadata GetEvent(string name);
    
    /// <summary>
    /// Get all registered events.
    /// </summary>
    IReadOnlyDictionary<string, EventMetadata> GetAllEvents();
    
    /// <summary>
    /// Check if an event is registered.
    /// </summary>
    bool IsRegistered(string name);
    
    /// <summary>
    /// Validate all events have unique names.
    /// Throws if duplicates found.
    /// </summary>
    void ValidateUniqueness();
}

/// <summary>
/// Metadata about an event.
/// </summary>
public record EventMetadata
{
    public string Name { get; init; } = null!;
    public string Module { get; init; } = null!;
    public string Category { get; init; } = null!;
    public EventDirection Direction { get; init; }
    public string? Description { get; init; }
    public Type? PayloadType { get; init; }
}
```

**EventRegistry.cs** (Implementation):
```csharp
namespace NetMX.Events;

public class EventRegistry : IEventRegistry
{
    private readonly Dictionary<string, EventMetadata> _events = new();
    private readonly object _lock = new();
    
    public void RegisterEvent(string name, EventMetadata metadata)
    {
        lock (_lock)
        {
            if (_events.ContainsKey(name))
            {
                var existing = _events[name];
                throw new InvalidOperationException(
                    $"Event '{name}' is already registered by module '{existing.Module}'. " +
                    $"Attempted to register from module '{metadata.Module}'.");
            }
            
            _events[name] = metadata;
        }
    }
    
    public EventMetadata GetEvent(string name)
    {
        if (!_events.TryGetValue(name, out var metadata))
        {
            throw new KeyNotFoundException($"Event '{name}' is not registered.");
        }
        return metadata;
    }
    
    public IReadOnlyDictionary<string, EventMetadata> GetAllEvents()
    {
        lock (_lock)
        {
            return _events.ToImmutableDictionary();
        }
    }
    
    public bool IsRegistered(string name) => _events.ContainsKey(name);
    
    public void ValidateUniqueness()
    {
        lock (_lock)
        {
            var duplicates = _events
                .GroupBy(e => e.Key)
                .Where(g => g.Count() > 1)
                .ToList();
            
            if (duplicates.Any())
            {
                var errors = string.Join("\n", duplicates.Select(d => 
                    $"  - '{d.Key}' registered by: {string.Join(", ", d.Select(e => e.Value.Module))}"));
                
                throw new InvalidOperationException(
                    $"Duplicate event names detected:\n{errors}");
            }
        }
    }
}
```

**Events.cs** (Global Access Point):
```csharp
namespace NetMX.Events;

/// <summary>
/// Global event access point.
/// Use Events.Get("name") or Events.Permission.Created (type-safe).
/// </summary>
public static class Events
{
    private static IEventRegistry? _registry;
    
    /// <summary>
    /// Initialize the event registry. Called at startup.
    /// </summary>
    public static void Initialize(IEventRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
    
    /// <summary>
    /// Get event name by string (runtime access).
    /// </summary>
    public static string Get(string name)
    {
        if (_registry == null)
            throw new InvalidOperationException("Event registry not initialized.");
        
        return _registry.GetEvent(name).Name;
    }
    
    /// <summary>
    /// Check if event exists.
    /// </summary>
    public static bool Exists(string name)
    {
        return _registry?.IsRegistered(name) ?? false;
    }
    
    /// <summary>
    /// Get all registered events.
    /// </summary>
    public static IReadOnlyDictionary<string, EventMetadata> GetAll()
    {
        if (_registry == null)
            throw new InvalidOperationException("Event registry not initialized.");
        
        return _registry.GetAllEvents();
    }
    
    // ==========================================
    // Type-safe event access (populated at startup)
    // ==========================================
    
    /// <summary>
    /// Authorization module events.
    /// Populated when Authorization module registers its events.
    /// </summary>
    public static class Permission
    {
        public static string Created => Get("permission.created");
        public static string Updated => Get("permission.updated");
        public static string Deleted => Get("permission.deleted");
    }
    
    public static class Role
    {
        public static string Created => Get("role.created");
        public static string Updated => Get("role.updated");
        public static string Deleted => Get("role.deleted");
    }
    
    // More event categories added by source generators (see below)
}
```

---

### 2. Module Event Definitions

Each module defines its events in isolation:

**Authorization.Web/Events/AuthorizationEventDefinitions.cs**:
```csharp
namespace NetMX.Authorization.Events;

/// <summary>
/// Authorization module event definitions.
/// These are registered at startup.
/// </summary>
public static class AuthorizationEventDefinitions
{
    public static readonly EventMetadata[] AllEvents = new[]
    {
        new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a permission is created. Payload: { id: Guid }"
        },
        new EventMetadata
        {
            Name = "permission.updated",
            Module = "Authorization",
            Category = "Permission",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a permission is updated. Payload: { id: Guid }"
        },
        new EventMetadata
        {
            Name = "permission.deleted",
            Module = "Authorization",
            Category = "Permission",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a permission is deleted. Payload: { id: Guid }"
        },
        new EventMetadata
        {
            Name = "role.created",
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a role is created. Payload: { id: Guid }"
        },
        new EventMetadata
        {
            Name = "role.updated",
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a role is updated. Payload: { id: Guid }"
        },
        new EventMetadata
        {
            Name = "role.deleted",
            Module = "Authorization",
            Category = "Role",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a role is deleted. Payload: { id: Guid }"
        }
    };
}
```

**Authorization.Web/AuthorizationWebModule.cs** (Module Initializer):
```csharp
namespace NetMX.Authorization;

public static class AuthorizationWebModule
{
    public static IServiceCollection AddAuthorizationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ... existing service registrations ...
        
        // Register events
        services.AddSingleton<IEventRegistry, EventRegistry>();
        
        return services;
    }
    
    public static IApplicationBuilder UseAuthorizationModule(
        this IApplicationBuilder app)
    {
        // Register events at startup
        var registry = app.ApplicationServices.GetRequiredService<IEventRegistry>();
        
        foreach (var eventMetadata in AuthorizationEventDefinitions.AllEvents)
        {
            registry.RegisterEvent(eventMetadata.Name, eventMetadata);
        }
        
        // Validate no duplicates
        registry.ValidateUniqueness();
        
        return app;
    }
}
```

---

### 3. Global Access in Controllers (No Project References!)

**YourApp.Web/Controllers/ProductController.cs**:
```csharp
using NetMX.Events;  // ← Only reference to NetMX.Events package!

public class ProductController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _service.CreateAsync(dto);
        
        // Access Authorization events WITHOUT referencing Authorization.Web!
        this.HxTrigger(Events.Permission.Created, new { id = product.Id });
        
        // Or use string-based access
        this.HxTrigger(Events.Get("role.updated"), new { id });
        
        return Ok();
    }
}
```

**YourApp.Web/Views/Product/Index.cshtml**:
```html
@using NetMX.Events

<!-- Listen to Authorization events WITHOUT referencing Authorization.Web! -->
<div id="product-list" 
     hx-get="/Product/List" 
     hx-trigger="@Events.Permission.Created from:body">
</div>
```

---

### 4. Source Generator for Type-Safe Access (Advanced)

**NetMX.Events.SourceGenerators** (Roslyn Source Generator):

Automatically generates `Events.cs` partial class based on registered events:

```csharp
// Auto-generated by NetMX.Events.SourceGenerators
namespace NetMX.Events;

public static partial class Events
{
    // Authorization module events (discovered at compile time)
    public static class Permission
    {
        public const string Created = "permission.created";
        public const string Updated = "permission.updated";
        public const string Deleted = "permission.deleted";
    }
    
    public static class Role
    {
        public const string Created = "role.created";
        public const string Updated = "role.updated";
        public const string Deleted = "role.deleted";
    }
    
    // Identity module events
    public static class Login
    {
        public const string Success = "login.success";
        public const string Failed = "login.failed";
    }
    
    // ... more events discovered from all modules ...
}
```

**How Source Generator Works**:
1. Scans all `*EventDefinitions.cs` files in solution
2. Extracts event names and categories
3. Generates `Events.cs` partial class with nested classes
4. Provides IntelliSense support without project references!

---

## 🚀 CLI Integration

### Generating Events

**When CLI creates a feature**:

```bash
netmx generate feature Product -m Catalog
```

**CLI generates**:

1. **Product entity, DTOs, services, controller** (as usual)

2. **CatalogEventDefinitions.cs** (NEW):
```csharp
public static class CatalogEventDefinitions
{
    public static readonly EventMetadata[] AllEvents = new[]
    {
        new EventMetadata
        {
            Name = "product.created",
            Module = "Catalog",
            Category = "Product",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a product is created. Payload: { id: Guid }"
        },
        new EventMetadata
        {
            Name = "product.updated",
            Module = "Catalog",
            Category = "Product",
            Direction = EventDirection.Upstream
        },
        new EventMetadata
        {
            Name = "product.deleted",
            Module = "Catalog",
            Category = "Product",
            Direction = EventDirection.Upstream
        }
    };
}
```

3. **Module initialization code** (updates existing or creates new):
```csharp
public static IApplicationBuilder UseCatalogModule(this IApplicationBuilder app)
{
    var registry = app.ApplicationServices.GetRequiredService<IEventRegistry>();
    
    foreach (var eventMetadata in CatalogEventDefinitions.AllEvents)
    {
        registry.RegisterEvent(eventMetadata.Name, eventMetadata);
    }
    
    return app;
}
```

---

## 🌐 Microservices Ready

### Shared Event Contracts Package

When splitting into microservices:

**NetMX.Events.Contracts** (Shared NuGet Package):
```csharp
// Shared across ALL microservices
public static class GlobalEventNames
{
    // Authorization service events
    public const string PermissionCreated = "permission.created";
    public const string RoleUpdated = "role.updated";
    
    // Identity service events
    public const string UserLoggedIn = "login.success";
    public const string UserRegistered = "registration.success";
    
    // Catalog service events
    public const string ProductCreated = "product.created";
    public const string InventoryUpdated = "inventory.updated";
}
```

Each microservice:
1. References `NetMX.Events.Contracts` (just event names, no logic)
2. Publishes events to message broker (RabbitMQ, Kafka, etc.)
3. No direct service-to-service coupling!

---

## ✅ Benefits

### 1. Global Access ✅
```csharp
Events.Permission.Created  // Works from ANY module!
Events.Get("role.updated") // Runtime access
```

### 2. No Module Coupling ✅
```
YourApp.Web
  ├─ References: NetMX.Events (core only)
  ├─ NO reference to Authorization.Web
  └─ NO reference to Identity.Web
```

### 3. Type Safety ✅
- IntelliSense shows all events
- Compile-time checking via source generator
- Refactor-safe (rename propagates)

### 4. CLI Scaffolding ✅
- CLI generates `*EventDefinitions.cs` automatically
- Registers events in module initializer
- Zero manual work!

### 5. Microservices Ready ✅
- Event registry becomes event contracts
- Each service publishes to message broker
- Shared event names package (no business logic)

### 6. Collision Detection ✅
```csharp
// Startup validation
registry.ValidateUniqueness();

// Throws: InvalidOperationException
// "Duplicate event names detected:
//   - 'permission.created' registered by: Authorization, Catalog"
```

### 7. Runtime Discovery ✅
- New modules register events at startup
- No compile-time dependencies
- Dynamic module loading supported

---

## 📊 Comparison: Partial Classes vs Registry

| Feature | Partial Classes (OLD) | Event Registry (NEW) |
|---------|----------------------|----------------------|
| **Global Access** | ✅ Yes | ✅ Yes |
| **No Module Coupling** | ❌ No (CS0433 errors) | ✅ Yes |
| **Type Safety** | ✅ Yes (when works) | ✅ Yes (source gen) |
| **CLI Scaffolding** | ✅ Easy | ✅ Easy |
| **Microservices** | ❌ Monolith only | ✅ Ready |
| **Collision Detection** | ❌ Compile warnings | ✅ Runtime exception |
| **Testing** | ❌ CS0433 errors | ✅ No issues |
| **Discovery** | ⚠️ Compile-time only | ✅ Runtime + compile |

---

## 🎯 Implementation Plan

### Phase 1: Core Registry (2 hours)
- [ ] Create `IEventRegistry` interface
- [ ] Create `EventRegistry` implementation
- [ ] Create `EventMetadata` record
- [ ] Create `Events` static class with `Get()` method
- [ ] Add to `NetMX.Events` package
- [ ] Write unit tests

### Phase 2: Module Integration (2 hours)
- [ ] Create `AuthorizationEventDefinitions.cs`
- [ ] Update `AuthorizationWebModule.cs` to register events
- [ ] Create `IdentityEventDefinitions.cs`
- [ ] Update Identity module initialization
- [ ] Create `AuditEventDefinitions.cs`
- [ ] Update Audit module initialization

### Phase 3: Source Generator (4 hours)
- [ ] Create `NetMX.Events.SourceGenerators` project
- [ ] Implement Roslyn source generator
- [ ] Scan for `*EventDefinitions.cs` files
- [ ] Generate `Events.{Category}.{Action}` nested classes
- [ ] Test source generator output

### Phase 4: CLI Integration (2 hours)
- [ ] Update `GenerateFeatureCommand` to generate `*EventDefinitions.cs`
- [ ] Generate module initializer code
- [ ] Test CLI scaffolding

### Phase 5: Documentation (1 hour)
- [ ] Update `QUICK-START.md`
- [ ] Update `CLI-IMPLEMENTATION.md`
- [ ] Update `.github/copilot-instructions.md`

**Total Effort**: ~11 hours (vs 4 hours for module-specific classes)

---

## 🤔 Open Questions

1. **Source Generator Complexity**: Worth the investment? Alternative: Manual `Events.cs` updates
2. **Performance**: Registry lookup vs const strings? (Negligible in practice)
3. **Event Versioning**: How to handle event schema changes in microservices?
4. **Tooling**: VS Code extension to visualize event graph?

---

## 📸 Visual Comparison

### Monolith Architecture (Event Registry)

```
┌────────────────────────────────────────────────────────┐
│                  Your Application                       │
│                                                         │
│  ┌─────────────────────────────────────────┐          │
│  │       NetMX.Events (Registry)            │          │
│  │  - Events.Permission.Created             │          │
│  │  - Events.Role.Updated                   │          │
│  │  - Events.Login.Success                  │          │
│  └─────────────────────────────────────────┘          │
│                      ▲                                  │
│                      │ Registers events                │
│       ┌──────────────┼──────────────┐                 │
│       │              │              │                  │
│       ▼              ▼              ▼                  │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐               │
│  │  Auth   │  │Identity │  │  Audit  │               │
│  └─────────┘  └─────────┘  └─────────┘               │
│                                                         │
│  NO project references between modules! ✅             │
└────────────────────────────────────────────────────────┘
```

### Microservices Architecture (Event Contracts)

```
┌──────────────────┐       ┌──────────────────┐
│ Authorization    │       │   Identity       │
│   Service        │       │    Service       │
│                  │       │                  │
│ Publishes:       │       │ Publishes:       │
│ permission.*     │       │ login.*          │
└────────┬─────────┘       └────────┬─────────┘
         │                          │
         │   ┌──────────────────┐   │
         └───► Message Broker   ◄───┘
             │ (RabbitMQ/Kafka) │
             └──────┬───────────┘
                    │
        ┌───────────┼───────────┐
        │           │           │
        ▼           ▼           ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│  Audit   │  │ Catalog  │  │ Billing  │
│ Service  │  │ Service  │  │ Service  │
│          │  │          │  │          │
│Subscribes│  │Subscribes│  │Subscribes│
└──────────┘  └──────────┘  └──────────┘

All services reference: NetMX.Events.Contracts (event names only)
```

---

## 🎯 Key Insight: Registry Enables Both Patterns

**The beauty**: Same code works in both monolith and microservices!

**Monolith** (In-Memory Registry):
```csharp
// Publish event in-process
eventBus.PublishAsync(Events.Permission.Created, new { id });
```

**Microservices** (Message Broker):
```csharp
// Publish to RabbitMQ/Kafka (same code!)
eventBus.PublishAsync(Events.Permission.Created, new { id });
```

Just swap `IEventBus` implementation:
- `InProcessEventBus` → Monolith
- `RabbitMqEventBus` → Microservices

**No code changes needed!** 🎉

---

**Status**: Design phase - awaiting approval for implementation

**Recommendation**: This provides the best of both worlds:
- ✅ Global access (like partial classes)
- ✅ No coupling (like independent modules)
- ✅ Microservices ready (event contracts)
- ✅ Runtime discovery (dynamic modules)
- ✅ Future-proof (easy to migrate to microservices)

This is the **right architecture** for your vision! 🚀

**Next Step**: Approve design and I'll start Phase 1 (Core Registry, 2 hours)

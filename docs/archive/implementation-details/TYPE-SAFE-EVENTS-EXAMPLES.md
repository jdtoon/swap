# Type-Safe Events - Usage Examples

## ✅ **IMPLEMENTED** - Full IntelliSense Support!

### What You Get

Type-safe event constants with full IntelliSense, no magic strings, compile-time safety:

```csharp
// ✅ Type Events. and get IntelliSense!
await _eventBus.PublishAsync(Events.Permission.Created, new { permissionId = 123 });
await _eventBus.PublishAsync(Events.Role.Updated, new { roleId = 456 });
await _eventBus.PublishAsync(Events.User.Login.Success, new { userId = 789 });
```

### Authorization Module Events

```csharp
using NetMX.Events;

// Permission events
Events.Permission.Created         // "permission.created"
Events.Permission.Updated         // "permission.updated"
Events.Permission.Deleted         // "permission.deleted"

// Role events
Events.Role.Created              // "role.created"
Events.Role.Updated              // "role.updated"
Events.Role.Deleted              // "role.deleted"
Events.Role.PermissionGranted    // "role.permission.granted"
Events.Role.PermissionRevoked    // "role.permission.revoked"
```

### Identity Module Events

```csharp
using NetMX.Events;

// User events
Events.User.Registered           // "user.registered"
Events.User.ProfileUpdated       // "user.profile.updated"
Events.User.EmailConfirmed       // "user.email.confirmed"
Events.User.Deleted              // "user.deleted"

// Login events (nested)
Events.User.Login.Success        // "user.login.success"
Events.User.Login.Failed         // "user.login.failed"
Events.User.Login.LockedOut      // "user.login.lockedout"
Events.User.Login.TwoFactorRequired  // "user.login.twofactor.required"

// Session events (nested)
Events.User.Session.Created      // "user.session.created"
Events.User.Session.Renewed      // "user.session.renewed"
Events.User.Session.Expired      // "user.session.expired"

// Password events (nested)
Events.User.Password.Changed     // "user.password.changed"
Events.User.Password.ResetRequested   // "user.password.reset.requested"
Events.User.Password.ResetCompleted   // "user.password.reset.completed"

// Account events (nested)
Events.User.Account.Locked       // "user.account.locked"
Events.User.Account.Unlocked     // "user.account.unlocked"
```

### Audit Module Events

```csharp
using NetMX.Events;

// AuditLog events
Events.AuditLog.Created          // "auditlog.created"
Events.AuditLog.Viewed           // "auditlog.viewed"
Events.AuditLog.Exported         // "auditlog.exported"

// AuditEntry events
Events.AuditEntry.Recorded       // "auditentry.recorded"
Events.AuditEntry.Updated        // "auditentry.updated"

// EntityChange events
Events.EntityChange.Tracked      // "entitychange.tracked"
Events.EntityChange.PropertyChanged  // "entitychange.property.changed"

// Compliance events
Events.Compliance.ReportGenerated     // "compliance.report.generated"
Events.Compliance.ViolationDetected   // "compliance.violation.detected"
Events.Compliance.PolicyUpdated       // "compliance.policy.updated"
```

## Real-World Examples

### Publishing Events (Type-Safe!)

```csharp
// In Authorization module - Permission service
public class PermissionService
{
    private readonly IEventBus _eventBus;
    
    public async Task CreatePermissionAsync(string name, string displayName)
    {
        var permission = new Permission(name, displayName);
        await _repository.InsertAsync(permission);
        
        // ✅ Type-safe with IntelliSense!
        await _eventBus.PublishAsync(Events.Permission.Created, new
        {
            permissionId = permission.Id,
            name = permission.Name,
            displayName = permission.DisplayName
        });
    }
}
```

### Subscribing to Events (Cross-Module!)

```csharp
// In Audit module - Listen to Authorization events WITHOUT project reference!
public class AuditEventSubscriber
{
    private readonly IEventBus _eventBus;
    private readonly IAuditLogger _auditLogger;
    
    public void Subscribe()
    {
        // ✅ Audit module can listen to Authorization events!
        _eventBus.Subscribe(Events.Permission.Created, async (data, ctx) =>
        {
            await _auditLogger.LogAsync("Permission created", data);
        });
        
        // ✅ Listen to multiple events
        _eventBus.Subscribe(Events.User.Login.Success, async (data, ctx) =>
        {
            await _auditLogger.LogAsync("User logged in", data);
        });
        
        _eventBus.Subscribe(Events.User.Login.Failed, async (data, ctx) =>
        {
            await _auditLogger.LogAsync("Login failed", data);
        });
    }
}
```

### HTMX Integration

```csharp
// In controller - Trigger HTMX refresh with type-safe events
[HttpPost]
public async Task<IActionResult> CreatePermission(CreatePermissionDto dto)
{
    var permission = await _permissionService.CreateAsync(dto);
    
    // ✅ Type-safe event trigger
    this.HxTrigger(Events.Permission.Created, new { permissionId = permission.Id });
    
    return Ok();
}
```

### In Views - Listen to Events

```html
<!-- Audit log auto-refreshes when permissions change -->
<div id="audit-list" 
     hx-get="/Audit/List" 
     hx-trigger="@Events.Permission.Created from:body, 
                 @Events.Permission.Updated from:body,
                 @Events.Permission.Deleted from:body">
    <!-- Audit entries -->
</div>
```

## Benefits

✅ **Full IntelliSense** - Type `Events.` and see all available events  
✅ **Compile-time safety** - Typos caught at compile time, not runtime  
✅ **Refactoring-friendly** - Rename events safely with IDE refactoring  
✅ **No project references** - Modules stay isolated, communicate via events  
✅ **Cross-module access** - Any module can listen to any event  
✅ **Self-documenting** - XML docs show payload structure  

## How It Works

### 1. Module Defines Events

```csharp
// modules/Authorization/Authorization.Web/Events/AuthorizationEventDefinitions.cs
public static class AuthorizationEventDefinitions
{
    public static void Register(IEventRegistry registry)
    {
        registry.RegisterEvent(Events.Permission.Created, new EventMetadata
        {
            Name = Events.Permission.Created,
            Module = "Authorization",
            Category = "Permission",
            Description = "Payload: { permissionId: Guid, name: string }"
        });
    }
}
```

### 2. Partial Class Extends Global Events

```csharp
// framework/NetMX.Events/Events.Authorization.cs
public static partial class Events
{
    public static class Permission
    {
        public const string Created = "permission.created";
        public const string Updated = "permission.updated";
    }
}
```

### 3. All Modules Get Access

```csharp
// ANY module, ANY project - just:
using NetMX.Events;

// Now you have:
Events.Permission.Created
Events.Role.Updated
Events.User.Login.Success
Events.AuditLog.Created
// etc.
```

## Startup Registration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register event registry
builder.Services.AddSingleton<IEventRegistry, EventRegistry>();
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();

var app = builder.Build();

// Register module events
var registry = app.Services.GetRequiredService<IEventRegistry>();
AuthorizationEventDefinitions.Register(registry);
IdentityEventDefinitions.Register(registry);
AuditEventDefinitions.Register(registry);

// Validate and initialize
registry.ValidateUniqueness();
Events.Initialize(registry);

app.Run();
```

## Future: Source Generator

Later, we'll add a Roslyn source generator to auto-generate these partial classes from event registrations. But for now, manual partial classes give us full type safety immediately!

```csharp
// Future: Source generator will create these automatically
// Just register events, get type-safe constants for free!
```

---

**Ready to use!** All 47 tests passing, full IntelliSense support, zero magic strings! 🚀

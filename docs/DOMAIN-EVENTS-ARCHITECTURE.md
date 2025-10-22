# Domain Events Architecture Decision

**Date**: October 22, 2025  
**Status**: ✅ Implemented  
**Impact**: All modules

---

## Problem Statement

How should modules define their own domain events while maintaining:
1. ✅ Type safety (IntelliSense, compile-time checking)
2. ✅ Extensibility (modules can add events without modifying framework)
3. ✅ Discoverability (developers can find all available events)
4. ✅ Cross-module references (modules can listen to each other's events)
5. ✅ Works with NuGet packages

---

## Decision

**Use partial classes to extend the base `DomainEvents` class**

### Framework (NetMX.Events)

```csharp
// NetMX.Events/DomainEvents.cs
namespace NetMX.Events;

public static partial class DomainEvents
{
    // Core framework events (User, Product, etc.)
    public static class User
    {
        public const string Created = "user.created";
        public const string Updated = "user.updated";
        public const string Deleted = "user.deleted";
    }
}
```

### Modules Extend via Partial Classes

```csharp
// Authorization.Web/Events/DomainEvents.Authorization.cs
namespace NetMX.Events;

public static partial class DomainEvents
{
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
}
```

---

## How It Works

### 1. **Same Namespace** = Unified API

Both files use `namespace NetMX.Events;`, so from the developer's perspective, there's one unified `DomainEvents` class:

```csharp
// In controller
using NetMX.Events;

this.HxTrigger(DomainEvents.User.Created, new { id = user.Id });         // Framework event
this.HxTrigger(DomainEvents.Permission.Created, new { id = perm.Id });   // Module event
```

**IntelliSense shows ALL events** from framework + all installed modules!

### 2. **Partial Class Merging**

C# compiler merges all `partial class DomainEvents` declarations into one class at compile time:

```
NetMX.Events (NuGet package)
  ↓
DomainEvents.cs → partial class DomainEvents { User, Product, ... }

Authorization.Web (NuGet package)
  ↓
DomainEvents.Authorization.cs → partial class DomainEvents { Permission, Role }

Identity.Web (NuGet package)
  ↓
DomainEvents.Identity.cs → partial class DomainEvents { UserLogin, UserRole }

==== COMPILED RESULT ====
DomainEvents {
    User, Product, Permission, Role, UserLogin, UserRole
}
```

### 3. **Works with NuGet Packages**

When you install a module via NuGet:

```powershell
dotnet add package NetMX.Identity.Web
```

The `DomainEvents.Identity.cs` file is included in the package, and your app automatically gets access to Identity events!

```csharp
// Immediately available after installing NetMX.Identity.Web
DomainEvents.UserLogin.Success  // ✅ IntelliSense works!
DomainEvents.UserLogin.Failed   // ✅ Compile-time safe!
```

---

## Benefits

### ✅ Open/Closed Principle

- **Open for extension**: Modules can add new events
- **Closed for modification**: No changes to framework needed

### ✅ Type Safety

- IntelliSense shows all available events
- Compile-time errors if you reference a non-existent event
- Refactoring safety (rename propagates everywhere)

### ✅ Cross-Module Communication

**Example**: Audit module listens to Authorization events

```html
<!-- In Audit.Web/Views/AuditLog/Index.cshtml -->
<div hx-get="/AuditLog/List" 
     hx-trigger="@DomainEvents.Permission.Created from:body, 
                 @DomainEvents.Permission.Deleted from:body">
    <!-- Auto-refreshes when permissions change -->
</div>
```

**No coupling!** Audit module doesn't reference Authorization module. It just uses the shared `DomainEvents` class.

### ✅ Discoverability

Developers can explore all events via IntelliSense:

```csharp
DomainEvents.  // <-- Autocomplete shows ALL events from ALL modules
    ├─ User
    ├─ Product
    ├─ Permission  (from Authorization module)
    ├─ Role        (from Authorization module)
    ├─ AuditLog    (from Audit module)
    └─ UserLogin   (from Identity module)
```

### ✅ Self-Documenting

Each event has XML docs:

```csharp
/// <summary>
/// Triggered when a permission is created.
/// Payload: { id: Guid }
/// </summary>
public const string Created = "permission.created";
```

Hover over `DomainEvents.Permission.Created` in VS Code → See documentation!

---

## Usage Examples

### In Controllers (Triggering Events)

```csharp
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;

public class PermissionController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(CreatePermissionDto dto)
    {
        var permission = await _service.CreateAsync(dto);
        
        // Trigger event - HTMX clients will receive this
        this.HxTrigger(DomainEvents.Permission.Created, new { id = permission.Id });
        
        return Ok();
    }
}
```

### In Views (Listening to Events)

```html
@using NetMX.Events

<!-- Auto-refresh when permission created or updated -->
<div id="permission-list" 
     hx-get="/Permission/List" 
     hx-trigger="load, 
                 @DomainEvents.Permission.Created from:body, 
                 @DomainEvents.Permission.Updated from:body">
</div>
```

### Cross-Module Communication

```html
<!-- In Audit module - listen to Authorization events -->
<div id="audit-log" 
     hx-get="/AuditLog/List" 
     hx-trigger="@DomainEvents.Permission.Created from:body, 
                 @DomainEvents.Role.Created from:body">
    <!-- Refreshes when permissions/roles change -->
</div>
```

**No direct dependency!** Audit doesn't reference Authorization. They communicate via events.

---

## Naming Conventions

### Event Names (String Constants)

- Use lowercase with dots: `"permission.created"`
- Format: `{entity}.{action}`
- Actions: `created`, `updated`, `deleted`, `activated`, `deactivated`

### Class Names

- Use PascalCase: `Permission`, `Role`, `UserLogin`
- Should match entity name

### File Names

- Format: `DomainEvents.{ModuleName}.cs`
- Examples:
  - `DomainEvents.Authorization.cs`
  - `DomainEvents.Identity.cs`
  - `DomainEvents.Audit.cs`

---

## Module Guidelines

### Creating Events for Your Module

**Step 1**: Create `Events/DomainEvents.{ModuleName}.cs`

```csharp
namespace NetMX.Events;

/// <summary>
/// {ModuleName} module domain events.
/// </summary>
public static partial class DomainEvents
{
    /// <summary>
    /// {Entity}-related events.
    /// </summary>
    public static class {Entity}
    {
        /// <summary>
        /// Triggered when {entity} is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "{entity}.created";
    }
}
```

**Step 2**: Use in your controllers

```csharp
using NetMX.Events;

this.HxTrigger(DomainEvents.{Entity}.Created, new { id = entity.Id });
```

**Step 3**: Document payload structure in XML docs

```csharp
/// <summary>
/// Triggered when permission is granted to role.
/// Payload: { roleId: Guid, permissionId: Guid, grantedBy: Guid }
/// </summary>
public const string Granted = "permission.granted";
```

---

## Alternatives Considered (and Rejected)

### ❌ Option 1: Magic Strings Everywhere

```csharp
this.HxTrigger("permission.created", new { id });  // ❌ No IntelliSense, typos!
```

**Rejected**: No type safety, prone to errors

### ❌ Option 2: Each Module Has Own Event Class

```csharp
AuthorizationEvents.Permission.Created  // ❌ Inconsistent API
IdentityEvents.User.LoggedIn           // ❌ Harder to discover
```

**Rejected**: Inconsistent API, harder to discover events

### ❌ Option 3: Central Event Registry

```csharp
// Framework must know about ALL module events
EventRegistry.Register("permission.created", typeof(PermissionCreatedEvent));
```

**Rejected**: Violates Open/Closed Principle, framework must change when modules add events

### ✅ Option 4: Partial Classes (CHOSEN)

- Type safe ✅
- Extensible ✅
- Unified API ✅
- Discoverable ✅
- Works with NuGet ✅

---

## Compiler Warnings (Expected & Harmless)

When building modules, you'll see warnings like:

```
warning CS0436: The type 'DomainEvents' in 'Authorization.Web\Events\DomainEvents.Authorization.cs' 
conflicts with the imported type 'DomainEvents' in 'NetMX.Events'
```

**This is expected and harmless!**

**Why?** The compiler sees two `DomainEvents` classes:
1. Base class in `NetMX.Events` package
2. Extended class in `Authorization.Web`

**Resolution**: Compiler uses the module's version (which is what we want!)

**To suppress** (optional): Add to `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CS0436</NoWarn>
</PropertyGroup>
```

---

## Future Enhancements

### Event Payload Validation (Future)

```csharp
public static class Permission
{
    public const string Created = "permission.created";
    
    // Type-safe payload definition
    public record CreatedPayload(Guid Id, string Name);
}

// Usage with validation
this.HxTrigger(
    DomainEvents.Permission.Created, 
    new DomainEvents.Permission.CreatedPayload(perm.Id, perm.Name)
);
```

### Event Versioning (Future)

```csharp
public static class Permission
{
    public const string Created = "permission.created.v1";
    public const string CreatedV2 = "permission.created.v2";  // Breaking change
}
```

### Event Discovery API (Future)

```csharp
var allEvents = DomainEvents.GetAllEvents();
// Returns: ["user.created", "permission.created", "role.created", ...]
```

---

## Summary

**Pattern**: Partial classes extending `NetMX.Events.DomainEvents`

**Benefits**:
- ✅ Type safe (IntelliSense, compile-time checking)
- ✅ Extensible (Open/Closed Principle)
- ✅ Discoverable (one unified API)
- ✅ Cross-module communication without coupling
- ✅ Works seamlessly with NuGet packages

**Result**: Clean, type-safe, extensible event system for HTMX-driven UIs!

---

## References

- [Partial Classes (C# Reference)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods)
- [Open/Closed Principle](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle)
- NetMX.Events package source: `framework/NetMX.Events/DomainEvents.cs`
- Example module: `modules/Authorization/Authorization.Web/Events/DomainEvents.Authorization.cs`

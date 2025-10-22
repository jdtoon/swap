# Event Registry Pattern - Updated Guidelines

**Date**: October 22, 2025  
**Status**: Event Registry Phase 2 Complete  
**Purpose**: Supplement to .github/copilot-instructions.md with Event Registry specifics

---

## 🎯 Event Registry Pattern (THE NEW WAY)

### Core Architecture

**Event Registry** replaces the old `DomainEvents.*` partial class pattern with a centralized, type-safe system.

**Old Pattern** (Deprecated - DO NOT USE):
```csharp
// Module-specific partial classes (collision-prone)
public static partial class DomainEvents
{
    public static class Permission
    {
        public const string Created = "permission.created";
    }
}

// Usage (prone to errors)
this.HxTrigger(DomainEvents.Permission.Created);
```

**New Pattern** (Event Registry - USE THIS):
```csharp
// In framework: NetMX.Events/Events.cs
public static partial class Events
{
    public static partial class Permission
    {
        public const string Created = "permission.created";
        public const string Updated = "permission.updated";
        public const string Deleted = "permission.deleted";
    }
}

// In module: Authorization.Web/Events/AuthorizationEventDefinitions.cs
public static class AuthorizationEventDefinitions
{
    public static void Register(IEventRegistry registry)
    {
        registry.Register<PermissionCreatedPayload>(
            Events.Permission.Created,
            category: "Permission",
            description: "Triggered when a permission is created"
        );
        
        registry.Register<PermissionUpdatedPayload>(
            Events.Permission.Updated,
            category: "Permission",
            description: "Triggered when a permission is updated"
        );
    }
}

// Usage in controller
using NetMX.Events;

this.HxTrigger(Events.Permission.Created, new { permissionId = id });

// Usage in view
@using NetMX.Events

<div hx-trigger="@Events.Permission.Created from:body">
```

### Benefits

1. **No CS0436 Errors**: Centralized definitions eliminate duplicate definition warnings
2. **Type-Safe IntelliSense**: `Events.Permission.Created` provides full IntelliSense support
3. **No Project References**: Modules don't need to reference each other for events
4. **Compile-Time Safety**: Typos caught at compile time, not runtime
5. **Centralized Catalog**: All events in one place (`framework/NetMX.Events/`)
6. **Refactoring Support**: Rename event once, updates everywhere

### Key Files

**Framework** (`framework/NetMX.Events/`):
- `IEventRegistry.cs` - Interface for event registration
- `EventRegistry.cs` - Thread-safe implementation with collision detection
- `EventMetadata.cs` - Event information record
- `Events.cs` - Base partial class
- `Events.Authorization.cs` - Authorization events (6 events)
- `Events.Identity.cs` - Identity events (16 events)
- `Events.Audit.cs` - Audit events (15 events)

**Modules** (`modules/*/Events/`):
- `*EventDefinitions.cs` - Module-specific registration logic
- Registers events with IEventRegistry on module initialization

### Migration Status

✅ **Authorization Module**: 6 events, 6 controllers, 4 views, 1 test
✅ **Identity Module**: 16 events, 6 controllers, 0 views, 1 test  
✅ **Audit Module**: 15 events, 6 controllers, 2 views, 1 test

**Total**: 37 events, 18 controllers, 6 views, 3 integration tests

**Test Results**: 47 tests passing (34 Event Registry + 13 module integration)

### CLI Status (Phase 3 - HIGH PRIORITY)

⚠️ **CLI still generates old pattern!**

`tools/NetMX.CLI/Commands/GenerateFeatureCommand.cs` line 242:
```csharp
public static partial class DomainEvents // ← OLD PATTERN
```

**Needs Update To**:
```csharp
// Generate Events.*.cs partial class in NetMX.Events
// Generate *EventDefinitions.cs in module
// Generate Add*Events() extension method
```

**Why High Priority**: CLI is central to development workflow. Generates 2+ features per day.

### Usage Guidelines

**In Controllers**:
```csharp
using NetMX.Events;

[HttpPost]
public async Task<IActionResult> Create(CreatePermissionDto dto)
{
    var permission = await _service.CreateAsync(dto);
    
    // Type-safe event trigger
    this.HxTrigger(Events.Permission.Created, new 
    { 
        permissionId = permission.Id,
        permissionName = permission.Name
    });
    
    return Ok();
}
```

**In Views**:
```html
@using NetMX.Events

<!-- Type-safe event listener -->
<div id="permission-list" 
     hx-get="/api/permissions" 
     hx-trigger="load, @Events.Permission.Created from:body, @Events.Permission.Updated from:body">
</div>

<!-- IntelliSense shows all available events -->
<button hx-post="/api/permissions" 
        hx-trigger="click"
        hx-swap="none">
    Create Permission
</button>
```

**In Tests**:
```csharp
using NetMX.Events;

[Fact]
public async Task CreatePermission_TriggersEvent()
{
    // Arrange
    var dto = new CreatePermissionDto { Name = "Test" };
    
    // Act
    var result = await _controller.Create(dto);
    
    // Assert - type-safe event name
    Assert.Contains(Events.Permission.Created, _eventBus.TriggeredEvents);
}
```

### Documentation

**Core Architecture**:
- [EVENT-REGISTRY-ARCHITECTURE.md](../docs/EVENT-REGISTRY-ARCHITECTURE.md)
- [EVENT-REGISTRY-MULTI-ARCHITECTURE.md](../docs/EVENT-REGISTRY-MULTI-ARCHITECTURE.md)
- [TYPE-SAFE-EVENTS-EXAMPLES.md](../docs/TYPE-SAFE-EVENTS-EXAMPLES.md)

**Implementation Details**:
- [TESTING-RESULTS.md](../docs/TESTING-RESULTS.md) - 47 tests passing
- [COMPLETE-DEVELOPMENT-ROADMAP.md](../docs/COMPLETE-DEVELOPMENT-ROADMAP.md) - Phase 2 status

**User Guides**:
- [QUICK-START.md](../docs/QUICK-START.md) - Updated with Event Registry examples
- [TERMINOLOGY.md](../docs/TERMINOLOGY.md) - Event Registry definitions added

### Next Steps

**Phase 3: CLI Updates** (2-3 hours):
1. Update GenerateFeatureCommand to generate Events.*.cs partial classes
2. Generate *EventDefinitions.cs with Register() method
3. Generate Add*Events() extension method in modules
4. Update controller template to use Events.*
5. Update view template to use @Events.*
6. Test with `netmx generate feature TestEntity`

**Phase 4: Final Validation** (1 hour):
1. Run full test suite (target: 50+ tests passing)
2. Generate test feature with new CLI
3. Validate IntelliSense works in generated code
4. Update framework README files (8 packages need content)
5. Remove framework/NetMX.Events/DomainEvents.cs (no longer needed)

---

## 📋 Quick Reference

**Check Event Names**:
- Framework: `framework/NetMX.Events/Events.*.cs`
- IntelliSense: Type `Events.` in any .cs file
- Documentation: [TYPE-SAFE-EVENTS-EXAMPLES.md](../docs/TYPE-SAFE-EVENTS-EXAMPLES.md)

**Add New Events**:
1. Add to appropriate `Events.*.cs` partial class in `framework/NetMX.Events/`
2. Register in module's `*EventDefinitions.cs`
3. Use in controllers: `this.HxTrigger(Events.YourEvent.Name)`
4. Use in views: `hx-trigger="@Events.YourEvent.Name from:body"`

**CLI Status**:
- ❌ CLI still generates old `DomainEvents.*` pattern
- ⏸️ Phase 3 update pending (HIGH PRIORITY)
- ⚠️ Manual conversion needed until CLI updated

---

**This document supplements .github/copilot-instructions.md with Event Registry specifics.**  
**Last Updated**: October 22, 2025

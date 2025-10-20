# NetMX.Events

**Type-safe event naming system for HTMX event-driven architecture.**

## Problem

HTMX events use string-based event names, which can lead to:
- ❌ Typos that cause silent failures
- ❌ No IntelliSense support
- ❌ Refactoring challenges
- ❌ Unclear event contracts

## Solution

NetMX.Events provides static constants for all framework events:

```csharp
// ❌ Before: Magic strings
HtmxResponse.Trigger(this, "user-created", new { userId = user.Id });

// ✅ After: Type-safe constants
HtmxResponse.Trigger(this, DomainEvents.User.Created, new { userId = user.Id });
```

## Features

- ✅ **IntelliSense Support** - Discover available events while typing
- ✅ **Compile-Time Checking** - Catch typos at compile time
- ✅ **Self-Documenting** - XML docs describe event purpose and payload
- ✅ **Refactoring Safety** - Find all usages with "Go to References"
- ✅ **Extensible** - Add custom events via partial classes

## Installation

```bash
dotnet add package NetMX.Events
```

## Usage

### In Controllers

```csharp
using NetMX.Events;
using NetMX.Htmx;

public class UsersController : Controller
{
    [HttpPost]
    public IActionResult Create(CreateUserDto dto)
    {
        var user = _userService.Create(dto);
        
        // Trigger event with type-safe constant
        HtmxResponse.Trigger(this, DomainEvents.User.Created, new { 
            userId = user.Id 
        });
        
        return Ok();
    }
    
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _userService.Delete(id);
        
        // Multiple events can be triggered
        HtmxResponse.Trigger(this, DomainEvents.User.Deleted, new { userId = id });
        HtmxResponse.Trigger(this, DomainEvents.UI.ShowToast, new { 
            message = "User deleted", 
            type = "success" 
        });
        
        return Ok();
    }
}
```

### In Razor Views

```html
@using NetMX.Events

<!-- Listen for user creation -->
<div id="user-stats" 
     hx-get="/api/stats/users" 
     hx-trigger="@DomainEvents.User.Created from:body">
    <!-- Stats refresh automatically when user created -->
</div>

<!-- Listen for role changes -->
<div id="permissions-panel"
     hx-get="/api/users/@userId/permissions"
     hx-trigger="@DomainEvents.User.RoleChanged from:body">
    <!-- Panel updates when user role changes -->
</div>

<!-- Show toast on entity creation -->
<div id="notifications"
     hx-swap-oob="true"
     hx-trigger="@DomainEvents.UI.ShowToast from:body">
</div>
```

### Custom Events (Extending)

Create partial class in your project:

```csharp
namespace NetMX.Events;

public static partial class DomainEvents
{
    public static class Product
    {
        public const string Created = "product:created";
        public const string Updated = "product:updated";
        public const string Deleted = "product:deleted";
        public const string StockChanged = "product:stock-changed";
        public const string PriceChanged = "product:price-changed";
    }
    
    public static class Order
    {
        public const string Placed = "order:placed";
        public const string Shipped = "order:shipped";
        public const string Delivered = "order:delivered";
        public const string Cancelled = "order:cancelled";
    }
}
```

## Available Event Categories

### User Events
- `DomainEvents.User.Created`
- `DomainEvents.User.Updated`
- `DomainEvents.User.Deleted`
- `DomainEvents.User.RoleChanged`
- `DomainEvents.User.LoggedIn`
- `DomainEvents.User.LoggedOut`

### Role Events
- `DomainEvents.Role.Created`
- `DomainEvents.Role.Updated`
- `DomainEvents.Role.Deleted`
- `DomainEvents.Role.PermissionsChanged`

### Audit Events
- `DomainEvents.Audit.LogCreated`
- `DomainEvents.Audit.SettingsChanged`

### Generic Entity Events
- `DomainEvents.Entity.Created`
- `DomainEvents.Entity.Updated`
- `DomainEvents.Entity.Deleted`

### UI Events
- `DomainEvents.UI.ShowToast`
- `DomainEvents.UI.CloseModal`
- `DomainEvents.UI.RefreshSection`
- `DomainEvents.UI.ShowLoading`
- `DomainEvents.UI.HideLoading`

### Form Events
- `DomainEvents.Form.Submitted`
- `DomainEvents.Form.ValidationFailed`
- `DomainEvents.Form.Reset`

## Event Naming Convention

Format: `{category}:{action}`

Examples:
- `user:created` - User category, created action
- `role:permissions-changed` - Role category, permissions changed action
- `ui:show-toast` - UI category, show toast action

**Rules**:
- Use lowercase with hyphens
- Keep verbs in past tense for completed actions
- Use present tense for ongoing actions
- Be specific but concise

## Event Payload Guidelines

Each event should document its payload in XML comments:

```csharp
/// <summary>
/// Triggered when a user's role changes.
/// Payload: { userId: Guid, roleId: Guid, previousRoleId: Guid }
/// </summary>
public const string RoleChanged = "user:role-changed";
```

**Payload Best Practices**:
1. Include minimum required data (IDs)
2. Avoid sending full entities (keep payloads small)
3. Use consistent property naming (camelCase)
4. Document payload structure in XML comments

## CLI Integration

When using `netmx generate feature`, events are automatically generated:

```bash
netmx generate feature Product

# Generates:
# - DomainEvents.Product.Created
# - DomainEvents.Product.Updated
# - DomainEvents.Product.Deleted
```

Controller code uses static event names:

```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    var product = await _service.CreateAsync(dto);
    HtmxResponse.Trigger(this, DomainEvents.Product.Created, new { productId = product.Id });
    return Ok();
}
```

## Migration Guide

### From Magic Strings

**Before:**
```csharp
// Controller
HtmxResponse.Trigger(this, "user-created", new { userId = user.Id });

// View
<div hx-trigger="user-created from:body">
```

**After:**
```csharp
// 1. Install package
dotnet add package NetMX.Events

// 2. Add using
using NetMX.Events;

// 3. Replace strings with constants
HtmxResponse.Trigger(this, DomainEvents.User.Created, new { userId = user.Id });

// View
@using NetMX.Events
<div hx-trigger="@DomainEvents.User.Created from:body">
```

## Testing

Events are just constants, so testing is straightforward:

```csharp
[Fact]
public void Create_TriggersUserCreatedEvent()
{
    // Arrange
    var controller = new UsersController(_mockService.Object);
    
    // Act
    var result = controller.Create(new CreateUserDto());
    
    // Assert
    var headers = controller.Response.Headers;
    Assert.Contains(DomainEvents.User.Created, headers["HX-Trigger"]);
}
```

## Performance

Zero runtime overhead - these are compile-time constants that get inlined by the JIT compiler.

## Related Packages

- **NetMX.AspNetCore.Mvc** - HTMX helpers (`HtmxResponse.Trigger`)
- **NetMX.Ddd.Application** - Domain event dispatcher

## Contributing

To add new event categories:

1. Edit `DomainEvents.cs`
2. Add nested static class
3. Add const string event names
4. Document with XML comments including payload
5. Follow naming convention (`category:action`)

## License

MIT - See LICENSE file in repository root.

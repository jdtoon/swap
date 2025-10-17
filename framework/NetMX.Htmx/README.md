# NetMX.Htmx

Strongly-typed C# helpers for working with HTMX in ASP.NET Core applications.

## Overview

This package provides type-safe C# APIs for working with HTMX, eliminating magic strings and providing IntelliSense support for HTMX features.

## Philosophy

- **Views use raw HTMX attributes** - Keep your Razor views clean with standard HTMX syntax
- **Controllers use typed helpers** - Get compile-time safety and IntelliSense in your C# code
- **No magic** - Simple wrappers around HTMX headers and conventions

## Features

### Response Helpers

Use `HtmxResponse` static methods in your controllers:

```csharp
public IActionResult DeleteUser(Guid id)
{
    // Delete user logic...
    
    // Trigger a client-side event
    HtmxResponse.Trigger(this, "userDeleted", new { userId = id });
    
    // Retarget to a different element
    HtmxResponse.Retarget(this, "#user-list");
    
    // Change swap strategy
    HtmxResponse.Reswap(this, HtmxSwap.OuterHTML);
    
    return PartialView("_UserRow", nextUser);
}
```

Available methods:
- `Trigger()` - Trigger client-side events
- `TriggerAfterSettle()` / `TriggerAfterSwap()` - Control event timing
- `PushUrl()` / `ReplaceUrl()` - Update browser history
- `Retarget()` - Change target element
- `Reswap()` - Change swap strategy
- `Redirect()` - Client-side redirect
- `Refresh()` - Full page refresh

### Request Helpers

Check for HTMX requests and access HTMX headers:

```csharp
public IActionResult Index()
{
    if (Request.IsHtmx())
    {
        // Return partial view for HTMX requests
        return PartialView("_UserList", users);
    }
    
    // Return full page for regular requests
    return View(users);
}

public IActionResult GetDetails(Guid id)
{
    var targetId = Request.GetTargetId();
    var triggerId = Request.GetTriggerId();
    
    // Use HTMX headers to customize response...
}
```

### Swap Strategies

Type-safe enum for HTMX swap strategies:

```csharp
HtmxResponse.Reswap(this, HtmxSwap.OuterHTML);  // Replace entire element
HtmxResponse.Reswap(this, HtmxSwap.BeforeEnd);  // Append to container
HtmxResponse.Reswap(this, HtmxSwap.Delete);     // Delete element
```

## Usage Example

**View (Razor) - Raw HTMX:**
```html
<button hx-delete="/users/@user.Id" 
        hx-target="#user-row-@user.Id"
        hx-swap="outerHTML"
        hx-confirm="Are you sure?">
    Delete
</button>
```

**Controller - Typed Helpers:**
```csharp
[HttpDelete("/users/{id}")]
public IActionResult Delete(Guid id)
{
    _userService.Delete(id);
    
    // Strongly typed, IntelliSense-friendly
    HtmxResponse.Trigger(this, "userDeleted", new { userId = id });
    HtmxResponse.Reswap(this, HtmxSwap.Delete);
    
    return Ok();
}
```

## Event-Driven Components

Use HTMX events for component communication:

**Trigger event from controller:**
```csharp
HtmxResponse.Trigger(this, "user:created", new { 
    userId = newUser.Id,
    userName = newUser.FullName 
});
```

**Listen in view:**
```html
<div id="user-stats" 
     hx-get="/api/users/stats" 
     hx-trigger="user:created from:body">
    <!-- Stats auto-refresh on user creation -->
</div>
```

## Integration

Add to your project:

```bash
dotnet add package NetMX.Htmx
```

No additional configuration needed - just start using the helpers!

# Source Generators

`Swap.Htmx` includes Source Generators to make event handling type-safe and refactor-friendly.

## Why?
HTMX relies heavily on events (triggers) to coordinate updates. Using raw strings like `"user.created"` is error-prone:
- Typos are not caught at compile time.
- Refactoring is difficult (Find & Replace).
- No IntelliSense discovery of available events.

## How it works
The `Swap.Htmx.Generators` package analyzes your code at build time and generates strongly-typed wrappers for your event strings.

### 1. Define Events
Create a partial class and add the `[SwapEventSource]` attribute. Define your events as dot-notated strings.

```csharp
using Swap.Htmx.Attributes;

[SwapEventSource]
public partial class DomainEvents
{
    public const string UserSignedUp = "user.signed_up";
    public const string TodoItemCompleted = "todo.item.completed";
}
```

### 2. Use Generated Types
The generator parses the dot-notation and creates a nested class hierarchy.

**In Controllers:**
```csharp
// Before
return this.Htmx(h => h.WithTrigger("user.signed_up"));

// After
return this.SwapResponse()
    .WithTrigger(DomainEvents.User.SignedUp)
    .Build();
```

**In Razor Views:**
```razor
<!-- Before -->
<div hx-trigger="user.signed_up from:body">

<!-- After -->
<div hx-trigger="@DomainEvents.User.SignedUp from:body">
```

## Installation
The generator is included automatically when you reference `Swap.Htmx`.

# Source Generators

`Swap.Htmx` includes Source Generators to make development type-safe and refactor-friendly.

## Why?
HTMX relies heavily on events and view names. Using raw strings like `"user.created"` or `"_ProductGrid"` is error-prone:
- Typos are not caught at compile time.
- Refactoring is difficult (Find & Replace).
- No IntelliSense discovery of available events or views.

## Available Generators

| Generator | Attribute | Purpose |
|-----------|-----------|---------|
| **EventSourceGenerator** | `[SwapEventSource]` | Type-safe event keys from string constants |
| **ViewPathGenerator** | `[SwapViewSource]` | View name constants from .cshtml files |

---

## Event Source Generator

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

---

## View Path Generator

Automatically generates string constants for all `.cshtml` files in a folder.

### 1. Configure Your Project

Add `.cshtml` files as AdditionalFiles in your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

### 2. Define View Sources

Create a partial class with the `[SwapViewSource]` attribute:

```csharp
using Swap.Htmx.Attributes;

[SwapViewSource("Views/Inventory")]
public static partial class InventoryViews { }
```

### 3. Generated Output

The generator scans the folder and creates constants:

```csharp
// Auto-generated
public static partial class InventoryViews
{
    public const string Index = "Index";
    public const string Create = "Create";
    public const string Edit = "Edit";

    public static class Partials
    {
        public const string Grid = "_Grid";
        public const string Pagination = "_Pagination";
        public const string EditModal = "_EditModal";
    }
}
```

Views starting with `_` are considered partials and placed in the nested `Partials` class.

### 4. Use in Controllers

```csharp
// Before - magic strings
return this.SwapResponse()
    .WithView("_ProductGrid", viewModel)
    .AlsoUpdate("pagination", "_Pagination", viewModel)
    .Build();

// After - compile-time checked constants
return this.SwapResponse()
    .WithView(InventoryViews.Partials.Grid, viewModel)
    .AlsoUpdate("pagination", InventoryViews.Partials.Pagination, viewModel)
    .Build();
```

### Configuration Options

```csharp
// Include views from subdirectories
[SwapViewSource("Views/Admin", IncludeSubdirectories = true)]
public static partial class AdminViews { }
```

### Naming Conventions

| File Name | Generated Constant |
|-----------|-------------------|
| `Index.cshtml` | `Index` |
| `_Grid.cshtml` | `Partials.Grid` |
| `_EditModal.cshtml` | `Partials.EditModal` |
| `user-profile.cshtml` | `UserProfile` (kebab-case → PascalCase) |

---

## Installation

The generators are included automatically when you reference `Swap.Htmx`.

# Source Generators

`Swap.Htmx` includes Source Generators to make development type-safe and refactor-friendly.

## Why?
HTMX relies heavily on events, view names, and element IDs. Using raw strings like `"user.created"`, `"_ProductGrid"`, or `"pagination"` is error-prone:
- Typos are not caught at compile time.
- Refactoring is difficult (Find & Replace).
- No IntelliSense discovery of available events, views, or IDs.

## Available Generators

| Generator | Attribute | Purpose |
|-----------|-----------|---------|
| **EventSourceGenerator** | `[SwapEventSource]` | Type-safe event keys from string constants |
| **ViewPathGenerator** | `[SwapViewSource]` | View name constants from .cshtml files |
| **ElementIdGenerator** | `[SwapElementSource]` | Element ID constants from id="..." in .cshtml files |

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
    .Build();

// After - compile-time checked constants
return this.SwapResponse()
    .WithView(InventoryViews.Partials.Grid, viewModel)
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

## Element ID Generator

Automatically generates string constants for element IDs found in `.cshtml` files.

### 1. Configure Your Project

Ensure `.cshtml` files are included as AdditionalFiles (same as View Path Generator):

```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

### 2. Define Element Sources

Create a partial class with the `[SwapElementSource]` attribute:

```csharp
using Swap.Htmx.Attributes;

[SwapElementSource("Views/Inventory")]
public static partial class InventoryIds { }
```

### 3. Generated Output

The generator scans `.cshtml` files for `id="..."` attributes:

```html
<!-- Views/Inventory/Index.cshtml -->
<div id="product-grid">...</div>
<div id="pagination">...</div>
<div id="product-count">...</div>
<input id="search-input" />
```

Generates:

```csharp
// Auto-generated
public static partial class InventoryIds
{
    public const string Pagination = "pagination";
    public const string ProductCount = "product-count";
    public const string ProductGrid = "product-grid";
    public const string SearchInput = "search-input";
}
```

### 4. Use in Controllers

```csharp
// Before - magic strings (typos not caught!)
return this.SwapResponse()
    .WithView("_Grid", viewModel)
    .AlsoUpdate("product-grid", "_Grid", viewModel)
    .AlsoUpdate("pagniation", "_Pagination", viewModel)  // Typo!
    .Build();

// After - compile-time checked constants
return this.SwapResponse()
    .WithView(InventoryViews.Partials.Grid, viewModel)
    .AlsoUpdate(InventoryIds.ProductGrid, InventoryViews.Partials.Grid, viewModel)
    .AlsoUpdate(InventoryIds.Pagination, InventoryViews.Partials.Pagination, viewModel)
    .Build();
```

### Configuration Options

```csharp
// Include IDs from subdirectories
[SwapElementSource("Views/Admin", IncludeSubdirectories = true)]
public static partial class AdminIds { }

// Filter by prefix (only include IDs starting with "product-")
[SwapElementSource("Views/Products", Prefix = "product-")]
public static partial class ProductIds { }
```

### What Gets Extracted

| HTML | Generated Constant | Notes |
|------|-------------------|-------|
| `id="product-grid"` | `ProductGrid` | kebab-case → PascalCase |
| `id="nav_menu"` | `NavMenu` | snake_case → PascalCase |
| `id='single-quotes'` | `SingleQuotes` | Both quote styles supported |
| `id="@Model.Id"` | *(skipped)* | Dynamic Razor expressions skipped |
| `id="item-@i"` | *(skipped)* | Interpolated IDs skipped |

### Best Practice: Combine with View Path Generator

For maximum type safety, use both generators together:

```csharp
// Define both in your project
[SwapViewSource("Views/Products")]
public static partial class ProductViews { }

[SwapElementSource("Views/Products")]
public static partial class ProductIds { }

// Use in controller - fully type-safe!
return this.SwapResponse()
    .WithView(ProductViews.Partials.Grid, viewModel)
    .AlsoUpdate(ProductIds.Pagination, ProductViews.Partials.Pagination, viewModel)
    .AlsoUpdate(ProductIds.ProductCount, ProductViews.Partials.ProductCount, stats)
    .Build();
```

### Using Generated Constants in Views

You can also use the generated constants in your Razor views for `hx-target` and other reference attributes.

**Important:** Keep literal string IDs in the `id="..."` attribute so the generator can extract them.
Use constants only when *referencing* IDs (like `hx-target`).

```html
@using MyApp.Views  <!-- Contains PatternIds -->

<!-- ✅ Correct: id uses literal string, hx-target uses constant -->
<div id="product-grid">
    ...
</div>
<button hx-get="/products" 
        hx-target="#@ProductIds.ProductGrid">
    Refresh
</button>

<!-- ❌ Incorrect: Generator can't extract IDs from Razor expressions -->
<div id="@ProductIds.ProductGrid">  <!-- Won't be found by generator -->
    ...
</div>
```

This pattern gives you:
- **Type-safe references** - Compile-time checking for hx-target attributes
- **Single source of truth** - ID values defined once in HTML, constants auto-generated
- **Refactoring support** - Rename an ID in HTML, rebuild, and compiler shows all references

---

## State Class Generator

Automatically generates SwapState properties from view annotations.

### 1. Define State Container Class

Create a partial class inheriting from `SwapState`:

```csharp
using Swap.Htmx.Attributes;
using Swap.Htmx.State;

[SwapStateSource("Views/Inventory/_InventoryState.cshtml")]
public partial class InventoryState : SwapState { }
```

### 2. Define Properties in View

Add `swap-state-prop` annotations to hidden inputs:

```html
<!-- Views/Inventory/_InventoryState.cshtml -->
<div data-swap-state>
    <input type="hidden" swap-state-prop="Tab:string=all" />
    <input type="hidden" swap-state-prop="Page:int=1" />
    <input type="hidden" swap-state-prop="Search:string?" />
    <input type="hidden" swap-state-prop="SortBy:string=name" />
    <input type="hidden" swap-state-prop="SortDesc:bool=false" />
</div>
```

### 3. Generated Output

```csharp
// Auto-generated
public partial class InventoryState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}
```

### Property Syntax

```
swap-state-prop="PropertyName:Type=DefaultValue"
```

| Component | Required | Description |
|-----------|----------|-------------|
| PropertyName | Yes | PascalCase property name |
| Type | Yes | C# type (string, int, bool, decimal, etc.) |
| DefaultValue | No | Default value for the property |

**Supported Types:**
- `string`, `string?`
- `int`, `int?`, `long`, `short`, `byte`
- `bool`, `bool?`
- `decimal`, `double`, `float`
- `DateTime`, `Guid`

---

## Handler Validation Analyzer

A Roslyn diagnostic analyzer that validates event handler configurations at compile-time.

### Diagnostics

| Code | Severity | Message |
|------|----------|---------|
| `SWAP001` | Warning | Event '{0}' is triggered but no ISwapEventConfiguration handles it |
| `SWAP002` | Warning | Event key '{0}' is referenced but not defined in any [SwapEventSource] class |
| `SWAP003` | Warning | Event chain for '{0}' may create a circular dependency |
| `SWAP004` | Info | Event '{0}' has multiple handlers in the same configuration |

### Example: SWAP001 - No Handler

```csharp
// ⚠️ SWAP001: Event 'product.updated' is triggered but no ISwapEventConfiguration handles it
return SwapResponse()
    .WithTrigger("product.updated")
    .Build();
```

**Fix:** Add a handler in an ISwapEventConfiguration:

```csharp
public class ProductEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        events.When("product.updated")
            .RefreshPartial("#product-grid", "_Grid");
    }
}
```

### Suppressing Warnings

For events handled only on the client-side:

```csharp
#pragma warning disable SWAP001
return SwapResponse()
    .WithTrigger("client.only.event")  // Handled by JavaScript
    .Build();
#pragma warning restore SWAP001
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.SWAP001.severity = none
```

---

## Installation

The generators are included automatically when you reference `Swap.Htmx`.

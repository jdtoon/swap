# Swap.Htmx.Generators

Roslyn Source Generators and Analyzers for `Swap.Htmx`.

## Overview
This project provides build-time code generation and analysis to enhance the developer experience when working with `Swap.Htmx`.

## Generators & Analyzers

| Component | Attribute | Purpose |
|-----------|-----------|---------|
| `EventSourceGenerator` | `[SwapEventSource]` | Type-safe event keys from string constants |
| `ViewPathGenerator` | `[SwapViewSource]` | View name constants from .cshtml files |
| `ElementIdGenerator` | `[SwapElementSource]` | Element ID constants from id="..." in .cshtml files |
| `StateClassGenerator` | `[SwapStateSource]` | SwapState properties from view annotations |
| `HandlerValidationAnalyzer` | N/A | Warns on events without handlers |

---

## Event Source Generator

Automatically generates strongly-typed `EventKey` hierarchies from string constants.

**Input:**
```csharp
[SwapEventSource]
public partial class AppEvents
{
    public const string UserCreated = "user.created";
    public const string OrderShipped = "order.shipped";
}
```

**Generated Output:**
```csharp
public partial class AppEvents
{
    public static partial class User
    {
        public static readonly EventKey Created = new EventKey("user.created");
    }
    public static partial class Order
    {
        public static readonly EventKey Shipped = new EventKey("order.shipped");
    }
}
```

### Usage
1. Mark a class with `[SwapEventSource]`.
2. Define your events as `public const string`.
3. Build the project to generate the types.

---

## View Path Generator

Automatically generates string constants for view names from `.cshtml` files.

**Setup (.csproj):**
```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

**Input:**
```csharp
[SwapViewSource("Views/Inventory")]
public static partial class InventoryViews { }
```

**Generated Output (based on files in Views/Inventory):**
```csharp
public static partial class InventoryViews
{
    public const string Index = "Index";
    public const string Create = "Create";

    public static class Partials
    {
        public const string Grid = "_Grid";
        public const string Pagination = "_Pagination";
    }
}
```

### Usage
1. Add `.cshtml` files as `<AdditionalFiles>` in your `.csproj`.
2. Mark a class with `[SwapViewSource("path/to/views")]`.
3. Build the project to generate the constants.

### Options
- `IncludeSubdirectories = true` - Include views from nested folders.

---

## Element ID Generator

Automatically generates string constants for element IDs found in `.cshtml` files.

**Setup (.csproj):**
```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

**Input:**
```csharp
[SwapElementSource("Views/Inventory")]
public static partial class InventoryIds { }
```

**HTML in Views/Inventory/*.cshtml:**
```html
<div id="product-grid">...</div>
<div id="pagination">...</div>
```

**Generated Output:**
```csharp
public static partial class InventoryIds
{
    public const string Pagination = "pagination";
    public const string ProductGrid = "product-grid";
}
```

### Usage
1. Add `.cshtml` files as `<AdditionalFiles>` in your `.csproj`.
2. Mark a class with `[SwapElementSource("path/to/views")]`.
3. Build the project to generate the constants.

### Options
- `IncludeSubdirectories = true` - Include IDs from nested folders.
- `Prefix = "product-"` - Only include IDs starting with the specified prefix.

### Notes
- Dynamic Razor IDs (`id="@Model.Id"`) are automatically skipped.
- IDs are deduplicated across all files in the folder.
- kebab-case and snake_case are converted to PascalCase.

---

## State Class Generator

Automatically generates SwapState properties from view annotations.

**Setup (.csproj):**
```xml
<ItemGroup>
  <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

**Input (C#):**
```csharp
[SwapStateSource("Views/Inventory/_InventoryState.cshtml")]
public partial class InventoryState : SwapState { }
```

**Input (Views/Inventory/_InventoryState.cshtml):**
```html
<div data-swap-state>
    <input type="hidden" swap-state-prop="Tab:string=all" />
    <input type="hidden" swap-state-prop="Page:int=1" />
    <input type="hidden" swap-state-prop="Search:string?" />
    <input type="hidden" swap-state-prop="ShowDeleted:bool=false" />
</div>
```

**Generated Output:**
```csharp
public partial class InventoryState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
    public bool ShowDeleted { get; set; } = false;
}
```

### Property Syntax
```
swap-state-prop="PropertyName:Type=DefaultValue"
```

| Type | Example | Generated |
|------|---------|-----------|
| `string` | `Name:string=John` | `public string Name { get; set; } = "John";` |
| `string?` | `Search:string?` | `public string? Search { get; set; }` |
| `int` | `Page:int=1` | `public int Page { get; set; } = 1;` |
| `bool` | `Active:bool=true` | `public bool Active { get; set; } = true;` |
| `decimal` | `Price:decimal=99.99` | `public decimal Price { get; set; } = 99.99;` |

---

## Handler Validation Analyzer

Roslyn diagnostic analyzer that validates event handler configurations at compile-time.

### Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| `SWAP001` | Warning | Event triggered but no ISwapEventConfiguration handles it |
| `SWAP002` | Warning | Event key referenced but not defined in [SwapEventSource] class |
| `SWAP003` | Warning | Potential circular event chain detected |
| `SWAP004` | Info | Duplicate event handler in same configuration |

### Example Warnings

```csharp
// SWAP001: Event has no handler
return SwapResponse()
    .WithTrigger("product.updated")  // ⚠️ No configuration handles this
    .Build();

// Solution: Add to ISwapEventConfiguration
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

If you're intentionally triggering events for client-side handling only:

```csharp
#pragma warning disable SWAP001
return SwapResponse()
    .WithTrigger("client.only.event")
    .Build();
#pragma warning restore SWAP001
```

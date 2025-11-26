# Swap.Htmx.Generators

Roslyn Source Generators for `Swap.Htmx`.

## Overview
This project provides build-time code generation to enhance the developer experience when working with `Swap.Htmx`.

## Generators

| Generator | Attribute | Purpose |
|-----------|-----------|---------|
| `EventSourceGenerator` | `[SwapEventSource]` | Type-safe event keys from string constants |
| `ViewPathGenerator` | `[SwapViewSource]` | View name constants from .cshtml files |

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

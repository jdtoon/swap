# Swap.Htmx.Generators

Roslyn Source Generators and Analyzers for `Swap.Htmx`.

## Overview

This project provides build-time code generation and analysis to enhance the developer experience when working with `Swap.Htmx`. The generators eliminate magic strings by creating compile-time constants for views, element IDs, and event keys.

## Generators & Analyzers

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| `AutoScanGenerator` | Generates `SwapViews` and `SwapElements` | **Zero config** (recommended) |
| `EventSourceGenerator` | Type-safe event keys from string constants | `[SwapEventSource]` attribute |
| `ViewPathGenerator` | View constants from specific folder | `[SwapViewSource]` attribute (legacy) |
| `ElementIdGenerator` | Element ID constants from specific folder | `[SwapElementSource]` attribute (legacy) |
| `HandlerValidationAnalyzer` | Warns on events without handlers | N/A |

---

## AutoScanGenerator (Recommended)

The `AutoScanGenerator` automatically creates `SwapViews` and `SwapElements` classes with **zero configuration**.

### Zero Config Setup (v1.0.6+)

Simply reference `Swap.Htmx` and build. The included `Swap.Htmx.targets` file automatically includes:

```xml
<!-- Auto-included via Swap.Htmx.targets -->
Views/**/*.cshtml
Modules/**/Views/**/*.cshtml
Pages/**/*.cshtml
Components/**/*.cshtml
Areas/**/Views/**/*.cshtml
```

### Generated Output

**SwapViews** - Grouped by controller folder:

```csharp
// Generated from Views/Products/*.cshtml
public static class SwapViews
{
    public static class Products
    {
        public const string Index = "Index";
        public const string Details = "Details";
        public const string _Grid = "_Grid";           // Partials keep underscore
        public const string _Pagination = "_Pagination";
    }
    public static class Shared
    {
        public const string Error = "Error";
        public const string _Layout = "_Layout";
    }
    public static class Home
    {
        public const string Index = "Index";
    }
}
```

**SwapElements** - Filtered for meaningful IDs:

```csharp
// Generated from id="..." attributes in .cshtml files
public static class SwapElements
{
    public const string ProductGrid = "product-grid";
    public const string CartCount = "cart-count";
    public const string SearchResults = "search-results";
}
```

### Usage

```csharp
// Controller - use short view names
return SwapView(SwapViews.Products.Index, model);

// SwapResponse - controller-relative view names
return SwapResponse()
    .WithView(SwapViews.Products._Grid, products)
    .AlsoUpdate(SwapElements.CartCount, SwapViews.Cart._Count, count)
    .Build();

// Event handlers
builder.AlsoUpdate(SwapElements.ProductGrid, SwapViews.Products._Grid, products);
```

### Modular Monolith Support

For modular apps, each project with views that references `Swap.Htmx` gets its own generated classes:

```
MyApp.Modules.Billing/
  Views/
    Invoices/              → SwapViews.Invoices
      Index.cshtml
      _InvoiceList.cshtml
    Payments/              → SwapViews.Payments  
      Index.cshtml

MyApp.Modules.Auth/
  Views/
    Login/                 → SwapViews.Login
      Index.cshtml
```

```csharp
// In Billing module
builder.AlsoUpdate("invoice-list", SwapViews.Invoices._InvoiceList, model);

// In Auth module  
return SwapView(SwapViews.Login.Index, model);
```

### Areas Support

Views in `Areas/` folders are also grouped by controller:

```
Areas/
  Admin/
    Views/
      Users/               → SwapViews.Users
        Index.cshtml
        _UserList.cshtml
      Dashboard/           → SwapViews.Dashboard
        Index.cshtml
```

### SwapElements Filtering

The generator automatically filters out noise:

| Filtered | Example | Reason |
|----------|---------|--------|
| Numeric IDs | `id="123"` | Auto-generated |
| Single characters | `id="a"` | Loop variables |
| Framework IDs | `id="__RequestVerificationToken"` | ASP.NET internal |
| Razor expressions | `id="@Model.Id"` | Dynamic values |

### Optional: Inspect Generated Code

To see the generated output (for debugging):

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>obj\Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
<ItemGroup>
  <Compile Remove="obj\Generated\**\*.cs" />
</ItemGroup>
```

Generated files appear in `obj/Generated/Swap.Htmx.Generators/`.

### Adding Additional Paths

If you have views outside the standard folders:

```xml
<ItemGroup>
  <AdditionalFiles Include="CustomTemplates\**\*.cshtml" />
</ItemGroup>
```

---

## Event Source Generator

Generates strongly-typed `EventKey` constants from string constants.

**Input:**
```csharp
[SwapEventSource]
public static partial class CartEvents
{
    public const string ItemAdded = "cart.itemAdded";
    public const string CheckoutCompleted = "cart.checkoutCompleted";
}
```

**Generated Output:**
```csharp
public static partial class CartEvents
{
    public static partial class Cart
    {
        public static readonly EventKey ItemAdded = new EventKey("cart.itemAdded");
        public static readonly EventKey CheckoutCompleted = new EventKey("cart.checkoutCompleted");
    }
}
```

### Usage

```csharp
// Controller
return SwapEvent(CartEvents.Cart.ItemAdded, item).Build();

// Event handler attribute
[SwapHandler(typeof(CartEvents), nameof(CartEvents.ItemAdded))]
public class CartHandler : ISwapEventHandler<CartPayload>
{
    public void Handle(SwapEventContext<CartPayload> context)
    {
        context.Response.AlsoUpdate("cart-count", SwapViews.Cart._Count, count);
    }
}
```

---

## Legacy Attribute-Based Generators

For advanced scenarios, attribute-based generators allow targeting specific folders.

### ViewPathGenerator

```csharp
[SwapViewSource("Views/Inventory")]
public static partial class InventoryViews { }
```

**Options:**
- `IncludeSubdirectories = true` - Include nested folders

### ElementIdGenerator

```csharp
[SwapElementSource("Views/Inventory")]
public static partial class InventoryIds { }
```

**Options:**
- `IncludeSubdirectories = true` - Include nested folders
- `Prefix = "product-"` - Filter by prefix

> **Note:** These require manual `<AdditionalFiles>` configuration. Use `AutoScanGenerator` for most cases.

---

## Handler Validation Analyzer

Compile-time diagnostics for event handler configurations.

| Code | Severity | Description |
|------|----------|-------------|
| `SWAP001` | Warning | Event triggered but no handler configured |
| `SWAP002` | Warning | Event key referenced but not defined |
| `SWAP003` | Warning | Circular event chain detected |
| `SWAP004` | Info | Duplicate handler for same event |

### Suppressing Warnings

For client-side only events:

```csharp
#pragma warning disable SWAP001
return SwapResponse()
    .WithTrigger("client.only.event")
    .Build();
#pragma warning restore SWAP001
```

---

## Troubleshooting

### SwapViews/SwapElements Not Generated

1. **Check targets file loaded** - Verify `Swap.Htmx.targets` is included (v1.0.6+)
2. **Rebuild solution** - Generators run on build, not on save
3. **Check output** - Enable `EmitCompilerGeneratedFiles` to inspect generated code
4. **Verify folder structure** - Views must be in `Views/`, `Pages/`, `Modules/**/Views/`, etc.

### Missing Views in SwapViews

1. **Check file extension** - Must be `.cshtml`
2. **Check folder location** - Must match auto-scan patterns
3. **Add custom path** - Use `<AdditionalFiles>` for non-standard locations

### Missing Element IDs in SwapElements

1. **Check ID format** - Must be static string: `id="my-element"`
2. **Check filtering** - Numeric-only and single-char IDs are excluded
3. **Razor expressions skipped** - `id="@Model.Id"` won't be included

### Generator Errors

1. **Clean and rebuild** - `dotnet clean && dotnet build`
2. **Check for syntax errors** - Invalid .cshtml can cause generator failures
3. **Update package** - Ensure latest Swap.Htmx version

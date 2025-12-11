# Source Generators

Swap.Htmx includes Source Generators to make development type-safe and refactor-friendly.

## Why?

HTMX relies heavily on events, view names, and element IDs. Using raw strings like `"user.created"`, `"_ProductGrid"`, or `"product-list"` is error-prone:
- Typos are not caught at compile time
- Refactoring is difficult (Find & Replace)
- No IntelliSense discovery of available events, views, or IDs

## Available Generators

| Generator | Trigger | Purpose |
|-----------|---------|---------|
| **AutoScanGenerator** | Automatic | Type-safe view and element constants (recommended) |
| **EventSourceGenerator** | `[SwapEventSource]` | Type-safe event keys from string constants |
| **ViewPathGenerator** | `[SwapViewSource]` | View constants for specific folders (legacy) |
| **ElementIdGenerator** | `[SwapElementSource]` | Element ID constants for specific folders (legacy) |

---

## Auto-Scan Generator (Recommended)

**Zero configuration** — views and elements are scanned automatically.

### How It Works

Swap.Htmx includes a `.targets` file that auto-includes common view folders:
- `Views/**/*.cshtml`
- `Modules/**/Views/**/*.cshtml`
- `Pages/**/*.cshtml`
- `Components/**/*.cshtml`
- `Areas/**/Views/**/*.cshtml`

Just reference the package and build:

```bash
dotnet add package Swap.Htmx
dotnet build
```

### Generated Output

**SwapViews** — Grouped by controller folder:

```csharp
public static class SwapViews
{
    public static class Home
    {
        public const string Index = "Index";
        public const string Dashboard = "Dashboard";
    }
    
    public static class Products
    {
        public const string Index = "Index";
        public const string _ProductList = "_ProductList";
        public const string _ProductRow = "_ProductRow";
    }
    
    public static class Shared
    {
        public const string _Layout = "_Layout";
    }
}
```

**SwapElements** — All element IDs:

```csharp
public static class SwapElements
{
    public const string ProductList = "product-list";
    public const string ProductCount = "product-count";
    public const string MainContent = "main-content";
}
```

### Usage

```csharp
public class ProductsController : SwapController
{
    [HttpPost]
    public IActionResult Add(Product product)
    {
        SaveProduct(product);
        
        return SwapResponse()
            .WithView(SwapViews.Products._ProductRow, product)
            .AlsoUpdate(SwapElements.ProductCount, SwapViews.Products._ProductCount, GetCount())
            .WithSuccessToast("Product added!")
            .Build();
    }
}
```

See [AutoScanGenerator.md](AutoScanGenerator.md) for full documentation.

---

## Event Source Generator

Creates type-safe event keys from string constants with dot-notation.

### 1. Define Events

Create a partial class with the `[SwapEventSource]` attribute:

```csharp
using Swap.Htmx.Attributes;

[SwapEventSource]
public static partial class DomainEvents
{
    public const string UserSignedUp = "user.signed_up";
    public const string UserUpdated = "user.updated";
    public const string OrderCreated = "order.created";
    public const string OrderShipped = "order.shipped";
}
```

### 2. Generated Output

The generator parses dot-notation and creates nested classes:

```csharp
// Auto-generated
public static partial class DomainEvents
{
    public static partial class User
    {
        public static readonly EventKey SignedUp = new EventKey("user.signed_up");
        public static readonly EventKey Updated = new EventKey("user.updated");
    }
    
    public static partial class Order
    {
        public static readonly EventKey Created = new EventKey("order.created");
        public static readonly EventKey Shipped = new EventKey("order.shipped");
    }
}
```

### 3. Usage

**In Controllers:**

```csharp
// Before
return this.SwapResponse()
    .WithTrigger("user.signed_up")
    .Build();

// After
return this.SwapResponse()
    .WithTrigger(DomainEvents.User.SignedUp)
    .Build();
```

**In Event Handlers:**

```csharp
[SwapHandler]
public class UserEventHandler : ISwapEventHandler<UserPayload>
{
    public string EventName => DomainEvents.User.SignedUp.Name;
    
    public Task HandleAsync(UserPayload payload, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder
            .WithView(SwapViews.Users._UserRow, payload.User)
            .AlsoUpdate(SwapElements.UserCount, SwapViews.Users._UserCount, payload.Count);
        
        return Task.CompletedTask;
    }
}
```

**In Razor Views:**

```html
<div hx-trigger="@DomainEvents.User.SignedUp from:body">
    <!-- Re-fetches when user signs up -->
</div>
```

---

## Legacy Attribute-Based Generators

For explicit control over which folders to scan, use attribute-based generators.

### View Path Generator

```csharp
using Swap.Htmx.Attributes;

[SwapViewSource("Views/Products")]
public static partial class ProductViews { }

[SwapViewSource("Views/Admin", IncludeSubdirectories = true)]
public static partial class AdminViews { }
```

Requires `<AdditionalFiles>` in your `.csproj`:

```xml
<ItemGroup>
    <AdditionalFiles Include="Views\**\*.cshtml" />
</ItemGroup>
```

### Element ID Generator

```csharp
using Swap.Htmx.Attributes;

[SwapElementSource("Views/Products")]
public static partial class ProductIds { }

[SwapElementSource("Views/Products", Prefix = "product-")]
public static partial class ProductPrefixedIds { }
```

### When to Use Legacy Generators

- You want explicit control over which folders generate constants
- You need folder-specific prefixes or filtering
- You're migrating from an older version

**For new projects, use auto-scan** — it's simpler and always in sync.

---

## Handler Validation Analyzer

A Roslyn diagnostic analyzer that validates event configurations at compile-time.

### Diagnostics

| Code | Severity | Message |
|------|----------|---------|
| `SWAP001` | Warning | Event '{0}' is triggered but no ISwapEventConfiguration handles it |
| `SWAP002` | Warning | Event key '{0}' is referenced but not defined in any [SwapEventSource] class |
| `SWAP003` | Warning | Event chain for '{0}' may create a circular dependency |
| `SWAP004` | Info | Event '{0}' has multiple handlers in the same configuration |

### Example: SWAP001

```csharp
// ⚠️ SWAP001: Event 'product.updated' is triggered but no handler exists
return SwapResponse()
    .WithTrigger("product.updated")
    .Build();
```

**Fix:** Add a handler:

```csharp
[SwapHandler]
public class ProductHandler : ISwapEventHandler<ProductPayload>
{
    public string EventName => "product.updated";
    // ...
}
```

### Suppressing Warnings

For client-side only events:

```csharp
#pragma warning disable SWAP001
return SwapResponse()
    .WithTrigger("client.side.only")
    .Build();
#pragma warning restore SWAP001
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.SWAP001.severity = none
```

---

## Viewing Generated Code

Enable generated file output in your `.csproj`:

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

---

## Troubleshooting

### Constants Not Generating

1. **Rebuild** — `dotnet build --no-incremental`
2. **Check Dependencies → Analyzers** — Verify `Swap.Htmx.Generators` appears
3. **Check folder structure** — Files must be in Views, Pages, Components, or Areas

### IntelliSense Not Working

Restart your IDE after building. Generated code is cached.

### Duplicate Definitions

If you see duplicate errors, you may have both:
- `Swap.Htmx` (includes generators)
- `Swap.Htmx.Generators` (standalone)

Remove the standalone package — generators are included in `Swap.Htmx`.

---

## See Also

- [AutoScanGenerator.md](AutoScanGenerator.md) — Detailed auto-scan documentation
- [Events](Events.md) — Event handling patterns
- [OutOfBandSwaps](OutOfBandSwaps.md) — OOB swap patterns

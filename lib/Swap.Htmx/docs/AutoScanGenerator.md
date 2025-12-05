# Auto-Generated View & Element Constants

Swap.Htmx can automatically generate type-safe constants for your views and HTMX elements at compile time — **without requiring any attributes**. This eliminates magic strings and prevents runtime errors from typos.

## Quick Start

### 1. Add Generator Reference

In your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="Swap.Htmx.Generators" Version="1.0.0" 
                      PrivateAssets="all" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
</ItemGroup>

<!-- Enable scanning of .cshtml files -->
<ItemGroup>
    <AdditionalFiles Include="Views\**\*.cshtml" />
    <AdditionalFiles Include="Modules\**\Views\**\*.cshtml" />
</ItemGroup>
```

### 2. Build — That's It!

The generator automatically scans all `.cshtml` files and generates:

- **SwapViews** — Constants for every view file
- **SwapElements** — Constants for every `id` attribute in your views

## Generated Output

Given this folder structure:

```
Views/
    Home/
        Index.cshtml      (contains <div id="user-panel">)
        Dashboard.cshtml  (contains <div id="stats-grid">)
    Products/
        Index.cshtml      (contains <div id="product-list">)
        Details.cshtml    (contains <div id="product-details" id="reviews">)
    Shared/
        _Layout.cshtml    (contains <main id="main-content">)
```

The generator creates:

```csharp
// Auto-generated SwapViews.cs
public static class SwapViews
{
    public static class Home
    {
        public const string Index = "~/Views/Home/Index.cshtml";
        public const string Dashboard = "~/Views/Home/Dashboard.cshtml";
    }
    
    public static class Products
    {
        public const string Index = "~/Views/Products/Index.cshtml";
        public const string Details = "~/Views/Products/Details.cshtml";
    }
    
    public static class Shared
    {
        public const string _Layout = "~/Views/Shared/_Layout.cshtml";
    }
}

// Auto-generated SwapElements.cs
public static class SwapElements
{
    public static class Home
    {
        public static class Index
        {
            public const string UserPanel = "#user-panel";
        }
        public static class Dashboard
        {
            public const string StatsGrid = "#stats-grid";
        }
    }
    
    public static class Products
    {
        public static class Index
        {
            public const string ProductList = "#product-list";
        }
        public static class Details
        {
            public const string ProductDetails = "#product-details";
            public const string Reviews = "#reviews";
        }
    }
    
    public static class Shared
    {
        public static class _Layout
        {
            public const string MainContent = "#main-content";
        }
    }
}
```

## Usage

### Type-Safe View References

```csharp
// ❌ Before: Magic strings
return View("~/Views/Products/Details.cshtml", product);

// ✅ After: Compile-time safety
return View(SwapViews.Products.Details, product);
```

### Type-Safe Element Targets

```csharp
// ❌ Before: Magic strings, easy to typo
return SwapResponse()
    .WithTarget("#product-list")
    .WithView("_ProductList", products)
    .Build();

// ✅ After: Compile-time safety
return SwapResponse()
    .WithTarget(SwapElements.Products.Index.ProductList)
    .WithView(SwapViews.Products._ProductList, products)
    .Build();
```

### With SwapResponse

```csharp
public IActionResult UpdateDashboard()
{
    var stats = _service.GetStats();
    var notifications = _service.GetNotifications();
    
    return SwapResponse()
        // Primary response
        .WithTarget(SwapElements.Home.Dashboard.StatsGrid)
        .WithView(SwapViews.Home._Stats, stats)
        
        // Out-of-band update
        .WithOutOfBand(oob => oob
            .WithTarget(SwapElements.Shared._Layout.NotificationBadge)
            .WithContent(notifications.Count.ToString())
        )
        .Build();
}
```

## Module Support

For modular monolith structures, the generator recognizes `Modules/*/Views/` patterns:

```
Modules/
    Inventory/
        Views/
            Products/
                Index.cshtml
                Details.cshtml
    Orders/
        Views/
            Index.cshtml
            Details.cshtml
```

Generates:

```csharp
public static class SwapViews
{
    public static class Inventory
    {
        public static class Products
        {
            public const string Index = "~/Modules/Inventory/Views/Products/Index.cshtml";
            public const string Details = "~/Modules/Inventory/Views/Products/Details.cshtml";
        }
    }
    
    public static class Orders
    {
        public const string Index = "~/Modules/Orders/Views/Index.cshtml";
        public const string Details = "~/Modules/Orders/Views/Details.cshtml";
    }
}
```

## Naming Conventions

### Views

| File Path | Generated Constant |
|-----------|-------------------|
| `Views/Home/Index.cshtml` | `SwapViews.Home.Index` |
| `Views/Products/_List.cshtml` | `SwapViews.Products._List` |
| `Views/Shared/_Layout.cshtml` | `SwapViews.Shared._Layout` |
| `Modules/Sales/Views/Orders/Index.cshtml` | `SwapViews.Sales.Orders.Index` |

### Elements

| HTML ID | Generated Constant |
|---------|-------------------|
| `id="user-panel"` | `SwapElements.{Folder}.{View}.UserPanel` |
| `id="product-list"` | `SwapElements.{Folder}.{View}.ProductList` |
| `id="main-content"` | `SwapElements.{Folder}.{View}.MainContent` |
| `id="stats_grid"` | `SwapElements.{Folder}.{View}.StatsGrid` |

- Kebab-case (`user-panel`) → PascalCase (`UserPanel`)
- Snake_case (`user_panel`) → PascalCase (`UserPanel`)

## Configuration

### Including Files

The generator processes files added via `<AdditionalFiles>`:

```xml
<ItemGroup>
    <!-- Standard MVC Views -->
    <AdditionalFiles Include="Views\**\*.cshtml" />
    
    <!-- Razor Pages -->
    <AdditionalFiles Include="Pages\**\*.cshtml" />
    
    <!-- Modular structure -->
    <AdditionalFiles Include="Modules\**\Views\**\*.cshtml" />
    
    <!-- Areas -->
    <AdditionalFiles Include="Areas\**\Views\**\*.cshtml" />
</ItemGroup>
```

### Excluding Files

Use standard MSBuild patterns:

```xml
<ItemGroup>
    <AdditionalFiles Include="Views\**\*.cshtml" 
                     Exclude="Views\Generated\**\*.cshtml" />
</ItemGroup>
```

## Comparison with Attribute-Based Approach

### Auto-Scan (Recommended)

```xml
<!-- .csproj -->
<AdditionalFiles Include="Views\**\*.cshtml" />
```

- ✅ Zero attributes needed
- ✅ Scans all views automatically
- ✅ Extracts all IDs automatically
- ✅ Always in sync with actual files

### Attribute-Based (Legacy)

```csharp
[SwapViewSource("~/Views/Home")]
[SwapElementSource("~/Views/Home")]
public static partial class HomeViews { }
```

- ⚠️ Requires attributes per folder
- ⚠️ Must add partial classes manually
- ⚠️ Can become out of sync

**Both approaches work** — use whichever fits your project.

## Troubleshooting

### Constants Not Generating

1. **Check AdditionalFiles** — Verify your `.csproj` includes the files:
   ```xml
   <AdditionalFiles Include="Views\**\*.cshtml" />
   ```

2. **Rebuild the project** — Source generators run during build:
   ```
   dotnet build --no-incremental
   ```

3. **Check for errors** — Generator errors appear in build output

### IntelliSense Not Working

1. **Restart VS Code/Visual Studio** — IDEs cache generator output
2. **Check Dependencies** folder — Look for `Swap.Htmx.Generators` under Analyzers

### Duplicate Definitions

If you see duplicate definition errors:
- The generator now automatically deduplicates based on relative path
- Make sure you're on the latest version of `Swap.Htmx.Generators`

### Module Paths Incorrect

Ensure your `AdditionalFiles` pattern matches your structure:
```xml
<!-- For Modules/X/Views/... structure -->
<AdditionalFiles Include="Modules\**\Views\**\*.cshtml" />
```

## Generated File Locations

The generator creates:
- `SwapViews.g.cs` — In the project's generated code folder
- `SwapElements.g.cs` — In the project's generated code folder

View them in your IDE under Dependencies → Analyzers → Swap.Htmx.Generators.

## Best Practices

### 1. Use Meaningful IDs

```html
<!-- Good: Descriptive IDs -->
<div id="product-search-results">...</div>
<div id="user-notifications-list">...</div>

<!-- Avoid: Generic IDs -->
<div id="content">...</div>
<div id="list">...</div>
```

### 2. Consistent Naming

Use kebab-case for IDs (HTML convention):
```html
<div id="shopping-cart-summary">...</div>
```

### 3. One ID Per Purpose

Don't reuse IDs across views for different purposes:
```html
<!-- View A: search-results for products -->
<div id="product-search-results">...</div>

<!-- View B: search-results for orders -->  
<div id="order-search-results">...</div>
```

## See Also

- [SwapResponse Builder](SwapResponseBuilder.md) — Building HTMX responses
- [Out-of-Band Swaps](OutOfBandSwaps.md) — Multi-element updates
- [`<swap-nav>` Tag Helper](SwapNavTagHelper.md) — Simplified navigation

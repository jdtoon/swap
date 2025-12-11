# Auto-Generated View & Element Constants

Swap.Htmx automatically generates type-safe constants for your views and HTMX elements at compile time — **no configuration required**. This eliminates magic strings and prevents runtime errors from typos.

## Zero Configuration

As of v1.0.6, Swap.Htmx **automatically scans** your view folders. No `<AdditionalFiles>` needed!

The package includes a `.targets` file that auto-includes:
- `Views/**/*.cshtml` — Standard MVC
- `Modules/**/Views/**/*.cshtml` — Modular monoliths
- `Pages/**/*.cshtml` — Razor Pages
- `Components/**/*.cshtml` — View Components
- `Areas/**/Views/**/*.cshtml` — MVC Areas

Just reference the package and build:

```bash
dotnet add package Swap.Htmx
dotnet build
```

## Generated Output

### SwapViews — Grouped by Controller

Views are grouped by their **controller folder**, making them intuitive to use:

```
Views/
    Home/
        Index.cshtml
        Dashboard.cshtml
    Products/
        Index.cshtml
        Details.cshtml
        _ProductList.cshtml
        _ProductRow.cshtml
    Shared/
        _Layout.cshtml
        _Sidebar.cshtml
```

Generates:

```csharp
public static class SwapViews
{
    /// <summary>Views for Home</summary>
    public static class Home
    {
        public const string Index = "Index";
        public const string Dashboard = "Dashboard";
    }
    
    /// <summary>Views for Products</summary>
    public static class Products
    {
        public const string Index = "Index";
        public const string Details = "Details";
        public const string _ProductList = "_ProductList";
        public const string _ProductRow = "_ProductRow";
    }
    
    /// <summary>Views for Shared</summary>
    public static class Shared
    {
        public const string _Layout = "_Layout";
        public const string _Sidebar = "_Sidebar";
    }
}
```

### SwapElements — Flat List of IDs

All element IDs are extracted into a single class:

```html
<!-- Views/Products/Index.cshtml -->
<div id="product-list">...</div>
<div id="product-count">...</div>

<!-- Views/Shared/_Layout.cshtml -->
<main id="main-content">...</main>
```

Generates:

```csharp
public static class SwapElements
{
    /// <summary>From Index</summary>
    public const string ProductList = "product-list";
    
    /// <summary>From Index</summary>
    public const string ProductCount = "product-count";
    
    /// <summary>From _Layout</summary>
    public const string MainContent = "main-content";
}
```

## Usage

### In Controllers

```csharp
public class ProductsController : SwapController
{
    public IActionResult List()
    {
        var products = GetProducts();
        
        return SwapResponse()
            .WithView(SwapViews.Products._ProductList, products)
            .Build();
    }

    [HttpPost]
    public IActionResult Add(Product product)
    {
        SaveProduct(product);
        var count = GetProductCount();
        
        return SwapResponse()
            .WithView(SwapViews.Products._ProductRow, product)
            .AlsoUpdate(SwapElements.ProductCount, SwapViews.Products._ProductCount, count)
            .WithSuccessToast("Product added!")
            .Build();
    }
}
```

### Benefits

```csharp
// ❌ Before: Magic strings - typos cause silent failures
return SwapResponse()
    .WithView("_ProductLst", products)  // Typo!
    .AlsoUpdate("prodct-count", "_ProductCount", count)  // Typo!
    .Build();

// ✅ After: Compile-time safety
return SwapResponse()
    .WithView(SwapViews.Products._ProductList, products)
    .AlsoUpdate(SwapElements.ProductCount, SwapViews.Products._ProductCount, count)
    .Build();
```

## Modular Monolith Support

For modular structures like `Modules/SuperAdmin/Views/TenantsManagement/`, the generator uses the **controller folder** (not the module name):

```
Modules/
    SuperAdmin/
        Views/
            TenantsManagement/
                Index.cshtml
                Details.cshtml
                _TenantList.cshtml
            SuperAdminDashboard/
                Index.cshtml
            Shared/
                _Layout.cshtml
```

Generates:

```csharp
namespace MyApp.Modules.SuperAdmin
{
    public static class SwapViews
    {
        public static class TenantsManagement
        {
            public const string Index = "Index";
            public const string Details = "Details";
            public const string _TenantList = "_TenantList";
        }
        
        public static class SuperAdminDashboard
        {
            public const string Index = "Index";
        }
        
        public static class Shared
        {
            public const string _Layout = "_Layout";
        }
    }
}
```

**Each module generates its own classes** in its own namespace. When referencing from another project:

```csharp
using MyApp.Modules.SuperAdmin;

// Now SwapViews refers to that module's views
SwapViews.TenantsManagement._TenantList
```

## Areas Support

Areas create combined class names:

```
Areas/
    Admin/
        Views/
            Dashboard/
                Index.cshtml
            Users/
                Index.cshtml
```

Generates:

```csharp
public static class SwapViews
{
    public static class Admin_Dashboard
    {
        public const string Index = "Index";
    }
    
    public static class Admin_Users
    {
        public const string Index = "Index";
    }
}
```

## Razor Pages Support

Pages are grouped by folder:

```
Pages/
    Account/
        Login.cshtml
        Register.cshtml
    Index.cshtml
```

Generates:

```csharp
public static class SwapViews
{
    public static class Account
    {
        public const string Login = "Login";
        public const string Register = "Register";
    }
    
    public static class Pages
    {
        public const string Index = "Index";
    }
}
```

## Naming Conventions

### Views

| File | Constant Name | Value |
|------|---------------|-------|
| `Index.cshtml` | `Index` | `"Index"` |
| `_ProductList.cshtml` | `_ProductList` | `"_ProductList"` |
| `_Layout.cshtml` | `_Layout` | `"_Layout"` |
| `user-profile.cshtml` | `UserProfile` | `"user-profile"` |

- **Underscores preserved** — `_TenantList` stays as `_TenantList` (prevents clash with `TenantList`)
- **Kebab-case converted** — `user-profile` becomes `UserProfile` constant

### Elements

| HTML ID | Constant Name | Value |
|---------|---------------|-------|
| `id="product-list"` | `ProductList` | `"product-list"` |
| `id="main-content"` | `MainContent` | `"main-content"` |
| `id="user_panel"` | `UserPanel` | `"user_panel"` |

**Filtered out automatically:**
- Numeric IDs (`id="3"`)
- Single character IDs (`id="x"`)
- Framework IDs (`id="__RequestVerificationToken"`)
- Dynamic Razor expressions (`id="@Model.Id"`)

## Custom View Paths

Need to include additional folders? Add them in your `.csproj`:

```xml
<ItemGroup>
    <AdditionalFiles Include="CustomViews/**/*.cshtml" />
</ItemGroup>
```

## Troubleshooting

### Constants Not Generating

1. **Rebuild the project** — Source generators run during build
   ```bash
   dotnet build --no-incremental
   ```

2. **Check the analyzer** — Look under Dependencies → Analyzers → Swap.Htmx.Generators

3. **Verify folder structure** — Files must be in recognized folders (Views, Pages, Components, Areas, Modules)

### IntelliSense Not Working

Restart your IDE after building. IDEs cache generator output.

### Viewing Generated Code

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

## Best Practices

### 1. Use Meaningful Element IDs

```html
<!-- ✅ Good: Descriptive IDs -->
<div id="tenant-summary">...</div>
<div id="product-search-results">...</div>

<!-- ❌ Avoid: Generic IDs -->
<div id="content">...</div>
<div id="list">...</div>
```

### 2. Use Kebab-Case for IDs

HTML convention for IDs:

```html
<div id="shopping-cart-summary">...</div>
```

### 3. Keep ID Definitions in Literals

The generator extracts IDs from literal strings, not Razor expressions:

```html
<!-- ✅ Generator extracts this -->
<div id="product-grid">...</div>

<!-- ❌ Generator cannot extract this -->
<div id="@SwapElements.ProductGrid">...</div>
```

Use constants only when **referencing** IDs:

```html
<button hx-target="#@SwapElements.ProductGrid">Refresh</button>
```

## See Also

- [Source Generators](SourceGenerators.md) — Full generator documentation
- [SwapResponse Builder](OutOfBandSwaps.md) — Building HTMX responses
- [Events](Events.md) — Type-safe event handling

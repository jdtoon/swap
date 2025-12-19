# `<swap-nav>` Tag Helper

The `<swap-nav>` tag helper simplifies SPA-style navigation in Swap.Htmx applications. It automatically generates HTMX attributes for navigation links, reducing boilerplate and ensuring consistency.

## The Problem

Traditional HTMX navigation requires verbose, repetitive markup:

```html
<!-- ❌ Before: Verbose and error-prone -->
<a href="/products" 
   hx-get="/products" 
   hx-target="#main-content" 
   hx-push-url="true">Products</a>

<a href="/orders" 
   hx-get="/orders" 
   hx-target="#main-content" 
   hx-push-url="true">Orders</a>
```

This pattern is:
- **Repetitive** — Same attributes on every nav link
- **Error-prone** — Easy to forget `hx-target` or `hx-push-url`
- **Hard to maintain** — Changing the target requires updating every link

## The Solution

```html
<!-- ✅ After: Clean and simple -->
<swap-nav to="/products">Products</swap-nav>
<swap-nav to="/orders">Orders</swap-nav>
```

The tag helper automatically adds:
- `hx-get` from the `to` attribute
- `hx-target` from global configuration (default: `#main-content`)
- `hx-push-url="true"` for browser history

## Setup

### 1. Configure in Program.cs

```csharp
builder.Services.AddSwapHtmx(options =>
{
    // Default target for <swap-nav> links
    options.DefaultNavigationTarget = "#main-content";
    
    // Auto-suppress layout for HTMX requests (recommended)
    options.AutoSuppressLayout = true;
});
```

### 2. Add Tag Helpers (in _ViewImports.cshtml)

```razor
@addTagHelper *, Swap.Htmx
```

### 3. Configure _ViewStart.cshtml

When `AutoSuppressLayout = true`, add this to your `_ViewStart.cshtml`:

```razor
@using Swap.Htmx
@{
    Layout = Context.ShouldSuppressLayout() ? null : "_Layout";
}
```

This ensures:
- **Full page requests** (browser navigation) → Use layout
- **HTMX requests** (swap-nav clicks) → Return partial only

## Usage

### Basic Navigation

```html
<swap-nav to="/dashboard">Dashboard</swap-nav>
<swap-nav to="/products">Products</swap-nav>
<swap-nav to="/settings">Settings</swap-nav>
```

**Renders as:**
```html
<a hx-get="/dashboard" hx-target="#main-content" hx-push-url="true">Dashboard</a>
<a hx-get="/products" hx-target="#main-content" hx-push-url="true">Products</a>
<a hx-get="/settings" hx-target="#main-content" hx-push-url="true">Settings</a>
```

### With Query Parameters

```html
<swap-nav to="/products?category=electronics">Electronics</swap-nav>
<swap-nav to="/search?q=htmx">Search for "htmx"</swap-nav>
```

### With Styling

All HTML attributes pass through:

```html
<swap-nav to="/featured" class="btn btn-primary">
    ⭐ Featured Products
</swap-nav>

<swap-nav to="/sale" class="nav-item active" style="font-weight: bold;">
    🏷️ On Sale
</swap-nav>
```

### With HTMX Attributes

You can add any `hx-*` attribute:

```html
<!-- Pass values with request -->
<swap-nav to="/search" hx-vals='{"q": "htmx", "page": 1}'>Search</swap-nav>

<!-- Custom swap mode -->
<swap-nav to="/notifications" hx-swap="beforeend">Notifications</swap-nav>

<!-- With indicator -->
<swap-nav to="/reports" hx-indicator="#spinner">Reports</swap-nav>
```

### Custom Target

Override the default target for specific links:

```html
<!-- Load into sidebar instead of main content -->
<swap-nav to="/quick-view" hx-target="#sidebar">Quick View</swap-nav>

<!-- Load into modal -->
<swap-nav to="/modal-content" hx-target="#modal-slot">Open Modal</swap-nav>
```

### Disable URL Push

For in-page updates that shouldn't change the URL:

```html
<swap-nav to="/tab-content" push-url="false">Tab 2</swap-nav>
<swap-nav to="/modal" push-url="false" hx-target="#modal">Open Modal</swap-nav>
```

## Attributes Reference

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `to` | `string` | (required) | The URL to navigate to |
| `push-url` | `bool` | `true` | Whether to update browser history |
| `class` | `string` | — | CSS classes (passed through) |
| `style` | `string` | — | Inline styles (passed through) |
| `hx-target` | `string` | from config | Override the target element |
| `hx-swap` | `string` | — | Custom swap mode |
| `hx-vals` | `string` | — | Values to pass with request |
| `hx-*` | `string` | — | Any HTMX attribute |

## Configuration Options

```csharp
builder.Services.AddSwapHtmx(options =>
{
    // Target element for swap-nav links (default: "#main-content")
    options.DefaultNavigationTarget = "#main-content";
    
    // Auto-suppress layout for HTMX requests (default: false)
    // When true, HTMX requests return partial views without layout
    options.AutoSuppressLayout = true;
});
```

## How Auto-Layout Suppression Works

When `AutoSuppressLayout = true`:

1. **SwapLayoutFilter** runs on every request
2. For HTMX requests (has `HX-Request` header, not `HX-Boosted`):
   - Sets `HttpContext.Items["Swap.SuppressLayout"] = true`
3. Your `_ViewStart.cshtml` checks this flag:
   ```razor
   Layout = Context.ShouldSuppressLayout() ? null : "_Layout";
   ```
4. Result: HTMX requests get partials, browser requests get full pages

This eliminates the need for per-module `_ViewStart.cshtml` files.

## Complete Example

### _Layout.cshtml

```html
<!DOCTYPE html>
<html>
<head>
    <title>@ViewData["Title"]</title>
    <script src="https://unpkg.com/htmx.org@2.0.8"></script>
    <script src="/_content/Swap.Htmx/js/swap.client.js"></script>
</head>
<body>
    <nav>
        <swap-nav to="/" class="nav-link">Home</swap-nav>
        <swap-nav to="/products" class="nav-link">Products</swap-nav>
        <swap-nav to="/orders" class="nav-link">Orders</swap-nav>
        <swap-nav to="/settings" class="nav-link">Settings</swap-nav>
    </nav>
    
    <main id="main-content">
        @RenderBody()
    </main>
</body>
</html>
```

### Controller

```csharp
public class ProductsController : Controller
{
    public IActionResult Index()
    {
        var products = _service.GetAll();
        return View(products);  // Returns full page or partial automatically
    }
    
    public IActionResult Details(int id)
    {
        var product = _service.GetById(id);
        return View(product);
    }
}
```

### Views/Products/Index.cshtml

```html
<h1>Products</h1>

<div class="product-grid">
    @foreach (var product in Model)
    {
        <div class="product-card">
            <h3>@product.Name</h3>
            <p>@product.Price.ToString("C")</p>
            <swap-nav to="/products/@product.Id" class="btn">View Details</swap-nav>
        </div>
    }
</div>
```

## Best Practices

### 1. Use Consistent Targets

Configure one main content target and use it consistently:

```csharp
options.DefaultNavigationTarget = "#main-content";
```

### 2. Keep URLs RESTful

```html
<!-- Good: Clean URLs -->
<swap-nav to="/products">Products</swap-nav>
<swap-nav to="/products/123">Product Details</swap-nav>

<!-- Avoid: Query strings for navigation -->
<swap-nav to="/page?view=products">Products</swap-nav>
```

### 3. Use Active States

```html
<swap-nav to="/products" 
          class="nav-link @(ViewContext.RouteData.Values["controller"]?.ToString() == "Products" ? "active" : "")">
    Products
</swap-nav>
```

### 4. Combine with Events

For complex UI updates after navigation, combine with events:

```csharp
return SwapResponse()
    .WithView("_ProductDetails", product)
    .WithTrigger("productViewed", new { id = product.Id })
    .Build();
```

## See Also

- [Navigation](Navigation.md) — `WithNavigation()` for programmatic navigation
- [Auto-Generated Constants](AutoScanGenerator.md) — Type-safe view paths
- [Out-of-Band Swaps](OutOfBandSwaps.md) — Update multiple elements

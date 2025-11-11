# Getting Started with Swap.Htmx

This guide walks you through setting up toast notifications and out-of-band swaps in your ASP.NET Core MVC application.

## Prerequisites

- .NET 8.0 or higher
- ASP.NET Core MVC project
- HTMX 2.0+ (via CDN or LibMan)

## Installation

### 1. Install NuGet Package

```bash
dotnet add package Swap.Htmx
```

### 2. Add HTMX and Bulma via LibMan

Create or update `libman.json` in your project root:

```json
{
  "version": "1.0",
  "defaultProvider": "cdnjs",
  "libraries": [
    {
      "library": "htmx@2.0.7",
      "destination": "wwwroot/lib/htmx",
      "files": [
        "htmx.min.js"
      ]
    },
    {
      "library": "bulma@1.0.4",
      "destination": "wwwroot/lib/bulma",
      "files": [
        "css/bulma.min.css"
      ]
    }
  ]
}
```

Restore libraries:
```bash
libman restore
```

## Basic Setup

### 1. Configure Services and Middleware

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(); // Add Swap.Htmx services

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmxShell(); // Validates HTMX responses
app.UseSwapHtmx();      // Adds event handling
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### 2. Create Base Layout

**Views/Shared/_Layout.cshtml:**
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - My App</title>
    <link rel="stylesheet" href="~/lib/bulma/css/bulma.min.css" />
    <link rel="stylesheet" href="~/css/htmx-toast.css" />
    <script src="~/lib/htmx/htmx.min.js"></script>
</head>
<body>
    <nav class="navbar is-dark">
        <div class="container">
            <div class="navbar-brand">
                <a class="navbar-item" href="/">My App</a>
            </div>
        </div>
    </nav>

    <main class="section">
        <div class="container">
            @RenderBody()
        </div>
    </main>

    @await Html.PartialAsync("_ToastContainer")
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### 3. Add Toast Container

**Views/Shared/_ToastContainer.cshtml:**
```html
<div id="toast-container"></div>

<script>
    document.body.addEventListener('showToast', function(event) {
        const detail = event.detail.value || event.detail || {};
        const container = document.getElementById('toast-container');
        
        if (detail.position) {
            container.setAttribute('data-position', detail.position);
        }
        
        const toast = document.createElement('div');
        toast.className = 'notification is-' + (detail.type || 'info');
        toast.textContent = detail.message || 'Operation completed';
        container.appendChild(toast);
        
        setTimeout(() => toast.remove(), 3500);
    });
</script>
```

### 4. Add Toast CSS

**wwwroot/css/htmx-toast.css:**
```css
#toast-container {
    position: fixed;
    top: 1rem;
    right: 1rem;
    z-index: 9999;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    pointer-events: none;
}

#toast-container[data-position="top-left"] {
    top: 1rem;
    left: 1rem;
    right: auto;
}

#toast-container[data-position="bottom-right"] {
    top: auto;
    bottom: 1rem;
    right: 1rem;
}

#toast-container[data-position="bottom-left"] {
    top: auto;
    bottom: 1rem;
    left: 1rem;
    right: auto;
}

.notification {
    pointer-events: auto;
    min-width: 300px;
    animation: slideIn 0.3s ease-out, fadeOut 0.5s ease-in 3s forwards;
}

@keyframes slideIn {
    from {
        transform: translateX(400px);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

@keyframes fadeOut {
    to {
        opacity: 0;
        transform: translateX(400px);
    }
}
```

## Using Toast Notifications

### In Controllers

Inherit from `SwapController` and use toast methods:

```csharp
using Swap.Htmx;

public class ProductController : SwapController
{
    public async Task<IActionResult> Index()
    {
        var products = await _service.GetAllAsync();
        return SwapView(products);
    }
    
    public async Task<IActionResult> Create(ProductDto dto)
    {
        await _service.CreateAsync(dto);
        
        Response.ShowSuccessToast("Product created successfully!");
        
        return SwapView("Created");
    }
    
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            Response.ShowSuccessToast("Product deleted!");
            return SwapView("Deleted");
        }
        catch (Exception ex)
        {
            Response.ShowErrorToast($"Delete failed: {ex.Message}");
            return SwapView("Error");
        }
    }
}
```

### In Views

```html
<div id="product-list">
    <h1 class="title">Products</h1>
    
    @foreach (var product in Model)
    {
        <div class="box">
            <h3>@product.Name</h3>
            <p>$@product.Price</p>
            
            <button class="button is-danger" 
                    hx-post="/products/delete/@product.Id" 
                    hx-target="#product-list"
                    hx-confirm="Delete this product?">
                Delete
            </button>
        </div>
    }
</div>
```

## Using Out-of-Band Swaps

### Update Multiple Elements

```csharp
public async Task<IActionResult> AddToCart(int productId)
{
    await _cartService.AddItemAsync(productId);
    
    // Main content
    var main = SwapView("ItemAdded");
    
    // Update cart badge in header (out-of-band)
    var total = await _cartService.GetTotalAsync();
    ViewData["OobCartBadge"] = $@"
        <span id=""cart-badge"" hx-swap-oob=""innerHTML"">
            {total.ItemCount}
        </span>";
    
    return main;
}
```

**In your view:**
```razor
<div class="notification is-success">
    Item added to cart!
</div>

@Html.Raw(ViewData["OobCartBadge"])
```

**In your layout header:**
```html
<div class="navbar-item">
    <span class="icon">🛒</span>
    <span id="cart-badge">0</span>
</div>
```

## Verification

### Test Toasts

1. Run your app
2. Trigger an action that shows a toast
3. You should see a notification slide in from top-right
4. It should auto-dismiss after 3 seconds

### Test OOB Swaps

1. Add an element with an `id` in your layout (e.g., cart badge)
2. Return OOB content from a controller action
3. The element should update without page refresh

## Next Steps

- [Toast Notifications Documentation](../framework/Swap.Htmx/TOASTS.md) - Complete toast API
- [OOB Swaps Documentation](../framework/Swap.Htmx/OOB-SWAPS.md) - Advanced OOB patterns
- [Event System](../framework/Swap.Htmx/EVENTS.md) - Chain domain events to UI updates
- [Example App](../framework/Swap.Htmx.TestApp) - See working examples

## Troubleshooting

### Toasts Don't Appear

1. Check browser console for JavaScript errors
2. Verify `_ToastContainer.cshtml` is included in layout
3. Ensure `htmx-toast.css` is loaded
4. Check that HTMX script is loaded before toast container

### OOB Swaps Don't Work

1. Verify target element has matching `id` attribute
2. Check that `hx-swap-oob` attribute is set correctly
3. Ensure element is in the DOM when response arrives
4. Use browser dev tools to inspect HTMX response

### CSS Not Loading

1. Run `libman restore` to ensure libraries are downloaded
2. Check that paths in layout match library destinations
3. Verify `UseStaticFiles()` is called in Program.cs

## Common Patterns

### Success Message + Redirect

```csharp
Response.ShowSuccessToast("Changes saved!");
Response.HxRedirect("/products");
return Ok();
```

### Toast + OOB + Main Content

```csharp
Response.ShowSuccessToast("Order completed!");

var main = SwapView("OrderComplete", order);

var orderCount = await _service.GetPendingCountAsync();
ViewData["OobOrderCount"] = $@"
    <span id=""order-count"" hx-swap-oob=""innerHTML"">
        {orderCount}
    </span>";

return main;
```

### Error Handling

```csharp
try
{
    await _service.ProcessAsync(data);
    Response.ShowSuccessToast("Processing complete!");
    return SwapView("Success");
}
catch (ValidationException ex)
{
    Response.ShowWarningToast(ex.Message);
    return SwapView("Invalid", data);
}
catch (Exception ex)
{
    Response.ShowErrorToast("An error occurred");
    _logger.LogError(ex, "Processing failed");
    return SwapView("Error");
}
```

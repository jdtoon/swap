# Out-of-Band (OOB) Swaps

## Overview

Out-of-band swaps allow you to update multiple parts of the page in a single HTMX response. The primary content swaps into the target specified in the HTMX request, while OOB content swaps into other elements on the page.

## Basic Usage

### Controller Example

```csharp
public class CartController : SwapController
{
    public async Task<IActionResult> AddToCart(int productId)
    {
        await _cartService.AddItemAsync(productId);
        
        // Main content: success message
        var mainResult = SwapView("ItemAdded");
        
        // OOB update: cart total in header
        var cartTotal = await _cartService.GetTotalAsync();
        ViewData["OobCartTotal"] = await RenderPartialToStringAsync("_CartTotal", cartTotal);
        
        return mainResult;
    }
}
```

### View Composition

The OOB partial view must include the `hx-swap-oob` attribute on its root element:

**Views/Cart/_CartTotal.cshtml:**
```html
@model CartTotalViewModel
<div id="cart-total" hx-swap-oob="@ViewData["HxSwapOob"]">
    <span class="badge">@Model.ItemCount items</span>
    <span>$@Model.Total</span>
</div>
```

**Views/Cart/ItemAdded.cshtml:**
```html
<div class="notification is-success">
    Item added to cart!
</div>

@* Include OOB content *@
@Html.Raw(ViewData["OobCartTotal"])
```

## Using SwapOobView Method

The `SwapOobView` method sets the necessary ViewData automatically:

```csharp
public async Task<IActionResult> UpdateDashboard()
{
    // Primary content
    var model = await GetDashboardDataAsync();
    
    // Update notifications OOB
    var notifications = await GetNotificationsAsync();
    ViewData["OobNotifications"] = SwapOobView("notifications", "_Notifications", notifications);
    
    // Update stats OOB
    var stats = await GetStatsAsync();
    ViewData["OobStats"] = SwapOobView("stats-panel", "_Stats", stats, swapStrategy: "innerHTML");
    
    return SwapView(model);
}
```

## Swap Strategies

The `swapStrategy` parameter controls how the OOB content is swapped:

- **`"true"`** (default) - Same as `outerHTML`, replaces entire element
- **`"innerHTML"`** - Replaces element's inner HTML
- **`"outerHTML"`** - Replaces entire element including tag
- **`"beforebegin"`** - Insert before the element
- **`"afterbegin"`** - Insert as first child
- **`"beforeend"`** - Insert as last child
- **`"afterend"`** - Insert after the element
- **`"delete"`** - Deletes the target element
- **`"none"`** - Does not swap

### Examples

```csharp
// Replace entire notifications div
SwapOobView("notifications", "_Notifications", model);

// Add new notification to container
SwapOobView("notifications", "_Notification", newNotification, "beforeend");

// Remove element
SwapOobView("temp-message", swapStrategy: "delete");

// Replace inner content only
SwapOobView("stats-panel", "_Stats", stats, "innerHTML");
```

## Complete Example

### Controller

```csharp
public class ProductController : SwapController
{
    public async Task<IActionResult> UpdateProduct(int id, ProductUpdateDto dto)
    {
        var product = await _productService.UpdateAsync(id, dto);
        
        // Main content: updated product details
        var mainModel = new ProductViewModel { Product = product };
        
        // OOB 1: Update product in listing if currently visible
        ViewData["OobProductCard"] = await RenderOobViewAsync(
            $"product-{id}", 
            "_ProductCard", 
            product
        );
        
        // OOB 2: Update inventory badge
        var inventory = await _inventoryService.GetStockAsync(id);
        ViewData["OobInventory"] = await RenderOobViewAsync(
            "inventory-badge",
            "_InventoryBadge",
            inventory,
            "innerHTML"
        );
        
        // Also show success toast
        Response.ShowSuccessToast("Product updated successfully!");
        
        return SwapView(mainModel);
    }
    
    private async Task<string> RenderOobViewAsync(
        string targetId, 
        string viewName, 
        object model, 
        string strategy = "true")
    {
        // Set ViewData for OOB attributes
        ViewData["HxSwapOob"] = strategy;
        ViewData["OobTargetId"] = targetId;
        
        // Render to string
        return await RenderPartialViewToStringAsync(viewName, model);
    }
}
```

### Views

**_ProductCard.cshtml:**
```html
@model Product
<div id="product-@Model.Id" class="card" hx-swap-oob="@ViewData["HxSwapOob"]">
    <div class="card-content">
        <h3>@Model.Name</h3>
        <p>$@Model.Price</p>
    </div>
</div>
```

**_InventoryBadge.cshtml:**
```html
@model InventoryViewModel
<span id="inventory-badge" hx-swap-oob="@ViewData["HxSwapOob"]">
    @if (Model.InStock)
    {
        <span class="tag is-success">In Stock (@Model.Quantity)</span>
    }
    else
    {
        <span class="tag is-danger">Out of Stock</span>
    }
</span>
```

## HTMX Request

```html
<form hx-post="/product/update/123" hx-target="#product-details">
    <!-- Form fields -->
    <button type="submit">Update Product</button>
</form>

<!-- Primary swap target -->
<div id="product-details">
    <!-- Will be replaced with main content -->
</div>

<!-- OOB targets that will also be updated -->
<div id="product-123" class="card">
    <!-- Will be updated via OOB -->
</div>

<span id="inventory-badge">
    <!-- Will be updated via OOB -->
</span>
```

## Best Practices

### 1. Target IDs Must Be Unique
```html
<!-- ❌ BAD: Duplicate IDs -->
<div id="stats">...</div>
<div id="stats">...</div>

<!-- ✅ GOOD: Unique IDs -->
<div id="stats-sales">...</div>
<div id="stats-users">...</div>
```

### 2. Include hx-swap-oob Attribute
```html
<!-- ❌ BAD: Missing attribute -->
<div id="cart-total">
    $@Model.Total
</div>

<!-- ✅ GOOD: Includes OOB attribute -->
<div id="cart-total" hx-swap-oob="@ViewData["HxSwapOob"]">
    $@Model.Total
</div>
```

### 3. Use ViewData for OOB Content
```csharp
// ❌ BAD: Direct string concatenation
var oobHtml = "<div id='cart'>...</div>";
return Content(mainHtml + oobHtml);

// ✅ GOOD: Use ViewData
ViewData["OobCart"] = await RenderOobViewAsync("cart", "_Cart", model);
return SwapView(mainModel);
```

### 4. Graceful Degradation
OOB swaps are ignored if the target element doesn't exist - HTMX won't throw errors:

```csharp
// Safe even if #cart-total doesn't exist on current page
ViewData["OobCartTotal"] = SwapOobView("cart-total", "_CartTotal", model);
```

## Common Patterns

### Shopping Cart Updates
```csharp
public async Task<IActionResult> AddToCart(int productId)
{
    await _cartService.AddAsync(productId);
    
    // Update cart icon badge
    var cartCount = await _cartService.GetCountAsync();
    ViewData["OobCartBadge"] = SwapOobView("cart-badge", "_CartBadge", cartCount, "innerHTML");
    
    Response.ShowSuccessToast("Added to cart!");
    return SwapView("CartConfirmation");
}
```

### Live Notifications
```csharp
public async Task<IActionResult> MarkAsRead(int notificationId)
{
    await _notificationService.MarkAsReadAsync(notificationId);
    
    // Update unread count
    var unreadCount = await _notificationService.GetUnreadCountAsync();
    ViewData["OobUnreadBadge"] = SwapOobView("unread-count", "_UnreadBadge", unreadCount, "innerHTML");
    
    // Remove from notification list
    ViewData["OobRemoveNotification"] = SwapOobView($"notification-{notificationId}", swapStrategy: "delete");
    
    return SwapView();
}
```

### Dashboard Widgets
```csharp
public async Task<IActionResult> RefreshDashboard()
{
    var dashboard = await GetDashboardAsync();
    
    // Update each widget independently
    ViewData["OobSalesWidget"] = SwapOobView("widget-sales", "_SalesWidget", dashboard.Sales);
    ViewData["OobUsersWidget"] = SwapOobView("widget-users", "_UsersWidget", dashboard.Users);
    ViewData["OobRevenueWidget"] = SwapOobView("widget-revenue", "_RevenueWidget", dashboard.Revenue);
    
    return SwapView("DashboardRefreshed");
}
```

## Testing

OOB functionality is fully tested with 13 unit tests covering:
- Partial view result type
- ViewData attributes set correctly
- Custom swap strategies
- Null models and view names
- All HTMX swap strategies

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~SwapControllerTests"
```

## Further Reading

- [HTMX Out of Band Swaps Documentation](https://htmx.org/attributes/hx-swap-oob/)
- HTMX allows multiple OOB swaps in a single response
- OOB elements can have their own `hx-swap-oob` attributes with different strategies

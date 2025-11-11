# Out-of-Band (OOB) Swaps

Out-of-band swaps allow you to update multiple parts of the page in a single HTMX response. The main content swaps into the target specified by the HTMX request, while additional content updates other elements on the page.

## Quick Start

### Basic Example

```csharp
public class CartController : SwapController
{
    public async Task<IActionResult> AddToCart(int productId)
    {
        await _cartService.AddItemAsync(productId);
        
        // Main content: success message
        var mainResult = SwapView("ItemAdded");
        
        // OOB update: cart total in header (using raw HTML for simplicity)
        var cartTotal = await _cartService.GetTotalAsync();
        ViewData["OobCartTotal"] = $@"
            <div id=""cart-total"" hx-swap-oob=""true"">
                <span>{cartTotal.ItemCount} items - ${cartTotal.Total}</span>
            </div>";
        
        return mainResult;
    }
}
```

**ItemAdded.cshtml:**
```razor
<div class="notification is-success">
    Item added to cart!
</div>

@* Include OOB content *@
@Html.Raw(ViewData["OobCartTotal"])
```

## How It Works

1. HTMX request targets a specific element (e.g., `hx-target="#main-content"`)
2. Server returns HTML with:
   - Main content (swaps into the target)
   - Additional elements with `hx-swap-oob="true"` attribute
3. HTMX swaps main content normally, then finds elements with `hx-swap-oob` and swaps them into matching IDs
4. Page updates in multiple places with one request!

## Common Use Cases

### Update Header Badge

```csharp
public IActionResult MarkNotificationRead(int id)
{
    _service.MarkRead(id);
    
    // Main content
    var result = SwapView("NotificationMarkedRead");
    
    // Update unread count in header
    var unreadCount = _service.GetUnreadCount();
    ViewData["OobBadge"] = $@"
        <span id=""unread-badge"" hx-swap-oob=""innerHTML"">
            {unreadCount}
        </span>";
    
    return result;
}
```

### Update Multiple Panels

```csharp
public async Task<IActionResult> UpdateDashboard()
{
    var stats = await _service.GetStatsAsync();
    
    // Main dashboard content
    var main = SwapView("Dashboard", stats);
    
    // Update stats panels OOB
    ViewData["OobRevenue"] = $@"
        <div id=""revenue-panel"" hx-swap-oob=""true"">
            <h3>Revenue</h3>
            <p>${stats.Revenue:N2}</p>
        </div>";
        
    ViewData["OobOrders"] = $@"
        <div id=""orders-panel"" hx-swap-oob=""true"">
            <h3>Orders</h3>
            <p>{stats.OrderCount}</p>
        </div>";
    
    return main;
}
```

### Refresh Product Card in List

```csharp
public async Task<IActionResult> UpdateProduct(int id, ProductDto dto)
{
    var product = await _service.UpdateAsync(id, dto);
    
    // Main content: edit form confirmation
    var main = SwapView("ProductUpdated", product);
    
    // Also update the product card in the listing (if user has list open)
    ViewData["OobProductCard"] = $@"
        <div id=""product-{id}"" hx-swap-oob=""true"" class=""card"">
            <h4>{product.Name}</h4>
            <p>${product.Price:N2}</p>
        </div>";
    
    return main;
}
```

## Swap Strategies

The `hx-swap-oob` attribute accepts different strategies:

### outerHTML (default)
```html
<div id="target" hx-swap-oob="true">
    <!-- Replaces entire element including the div -->
</div>
```

### innerHTML
```html
<span id="counter" hx-swap-oob="innerHTML">
    <!-- Replaces only inner content, keeps the span -->
    42
</span>
```

### Other Strategies
```html
<div id="list" hx-swap-oob="beforeend">
    <!-- Appends as last child -->
</div>

<div id="list" hx-swap-oob="afterbegin">
    <!-- Prepends as first child -->
</div>

<div id="temp" hx-swap-oob="delete">
    <!-- Deletes the element -->
</div>
```

## Working with Partial Views

For more complex OOB content, use partial views:

```csharp
public async Task<IActionResult> UpdateProduct(int id, ProductDto dto)
{
    var product = await _service.UpdateAsync(id, dto);
    
    // Main content
    var main = SwapView("ProductUpdated");
    
    // Render partial view to string for OOB
    var cardHtml = await RenderPartialToStringAsync("_ProductCard", product);
    ViewData["OobProductCard"] = WrapWithOobAttribute(cardHtml, $"product-{id}");
    
    return main;
}

private string WrapWithOobAttribute(string html, string targetId)
{
    // Ensure root element has id and hx-swap-oob
    // This is a simplified example - production code needs HTML parsing
    return $@"<div id=""{targetId}"" hx-swap-oob=""true"">{html}</div>";
}
```

## Combining with Toast Notifications

OOB swaps work perfectly with toast notifications:

```csharp
public async Task<IActionResult> CompleteOrder(int orderId)
{
    var order = await _service.CompleteAsync(orderId);
    
    // Show success toast
    Response.ShowSuccessToast("Order completed!");
    
    // Update main content
    var main = SwapView("OrderComplete", order);
    
    // Update order count in sidebar (OOB)
    var pendingCount = await _service.GetPendingCountAsync();
    ViewData["OobPendingOrders"] = $@"
        <span id=""pending-count"" hx-swap-oob=""innerHTML"">
            {pendingCount}
        </span>";
    
    return main;
}
```

## Best Practices

✅ **Use consistent IDs** - OOB targets must have matching IDs on the page

✅ **Include fallbacks** - If element doesn't exist, HTMX ignores the OOB swap (no error)

✅ **Keep OOB content focused** - Update specific elements, not entire page sections

✅ **Test with data-test-id** - Include `data-test-id` attributes for testing:
```csharp
$@"<div id=""cart-total"" data-test-id=""cart-total"" hx-swap-oob=""true"">
    {content}
</div>"
```

❌ **Don't overuse** - Too many OOB swaps in one response can be confusing. Consider separate requests or full page refresh.

❌ **Don't nest OOB elements** - Each OOB element should be a separate root element in the response

## Common Patterns

### Update and Refresh Pattern
```csharp
// Update item, refresh list
public async Task<IActionResult> ToggleFavorite(int id)
{
    await _service.ToggleFavoriteAsync(id);
    
    // Main: Updated item details
    var item = await _service.GetAsync(id);
    var main = SwapView("ItemDetails", item);
    
    // OOB: Refreshed favorites list in sidebar
    var favorites = await _service.GetFavoritesAsync();
    ViewData["OobFavorites"] = RenderFavoritesList(favorites);
    
    return main;
}
```

### Delete with Fade Pattern
```csharp
// Delete item and remove from list
public IActionResult Delete(int id)
{
    _service.Delete(id);
    
    // Main: Confirmation message
    var main = SwapView("Deleted");
    
    // OOB: Remove item from list
    ViewData["OobDeleteItem"] = $@"
        <div id=""item-{id}"" hx-swap-oob=""delete"">
        </div>";
    
    return main;
}
```

## See Also

- [Toast Notifications](./toast-notifications.md) - User feedback
- [Event System](./event-system.md) - Chain OOB updates to domain events
- [Full OOB Documentation](https://github.com/jdtoon/swap/blob/main/framework/Swap.Htmx/OOB-SWAPS.md) - Complete guide with advanced examples

# SwapShop Demo

A fully functional e-commerce demo showcasing the **Swap.Htmx** library for building modern, SPA-like web applications with ASP.NET Core and HTMX.

## What This Demonstrates

### Three-Tier API Architecture

SwapShop demonstrates all three tiers of the Swap.Htmx API:

**Tier 1: SwapView** - Simple view rendering
```csharp
public IActionResult Index()
{
    var products = _productService.GetAll();
    return SwapView(products);  // Handles HTMX vs full page automatically
}
```

**Tier 2: SwapResponse** - Coordinated multi-part updates
```csharp
public IActionResult Add(int productId, int quantity)
{
    var cart = _cartService.Add(productId, quantity);
    
    return SwapResponse()
        .AlsoUpdate("cart-badge", "_CartBadge", cart.Items.Count)
        .AlsoUpdate("cart-items", "_CartItems", cart)
        .WithSuccessToast("Item added to cart")
        .Build();
}
```

**Tier 3: SwapEvent** - Event-driven UI updates with event chains
```csharp
public IActionResult Remove(int productId)
{
    _cartService.Remove(productId);
    return SwapEvent(CartEvents.ItemRemoved).Build();
}
```

### Key Features Demonstrated

- ✅ **HTMX SPA Navigation** - Full page loads on first visit, partial updates on navigation
- ✅ **Out-of-Band (OOB) Swaps** - Update cart badge while updating main content
- ✅ **Event Chains** - Configure what happens when events fire (toasts, partials, redirects)
- ✅ **Minimal APIs** - Use `SwapResults` in Minimal API endpoints (Newsletter, System Status)
- ✅ **Toast Notifications** - Success/Error/Warning/Info toasts with auto-dismiss
- ✅ **Automatic Session Management** - Cookie persistence handled automatically via `GetOrInitializeSessionId()`
- ✅ **Configurable View Search Paths** - Cross-controller OOB swaps work seamlessly
- ✅ **Form Submissions** - Add to cart, update quantities, checkout with HTMX
- ✅ **Optimistic UI Updates** - Instant feedback on user actions
- ✅ **History Navigation** - Browser back/forward works correctly
- ✅ **Debug Logging** - Color-coded console logs for development

## Running the Demo

### Prerequisites

- .NET 9.0 SDK or later
- Any modern browser

### Quick Start

```powershell
# Navigate to the demo directory
cd demo/SwapShop/src

# Run the application
dotnet run
```

The app will start on **http://localhost:5120**

### Development Mode with Logging

Enable detailed debug logging to see what's happening:

```powershell
# Set environment variable
$env:SWAP_DEV_LOGGING="true"

# Or add to appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Swap.Htmx": "Debug"
    }
  }
}

# Run
dotnet run
```

You'll see color-coded logs showing:
- 🔵 Event chain execution
- 🟡 Toast notifications
- 🟢 HTTP headers (HX-Trigger, etc.)

## Project Structure

```
SwapShop/
├── src/
│   ├── Controllers/
│   │   ├── ProductsController.cs    # Tier 1: SwapView examples
│   │   ├── CartController.cs        # Tier 2: SwapResponse examples
│   │   └── CheckoutController.cs    # Tier 3: SwapEvent examples
│   ├── Events/
│   │   ├── EventChainConfiguration.cs  # Configure event chains
│   │   └── EventKeys.cs                # Type-safe event definitions
│   ├── Models/
│   │   └── DomainModels.cs          # Product, Cart, Order models
│   ├── Services/
│   │   ├── ProductService.cs        # In-memory product catalog
│   │   ├── CartService.cs           # Session-based cart
│   │   └── OrderService.cs          # Order management
│   ├── Views/
│   │   ├── Products/                # Product listing & details
│   │   ├── Cart/                    # Shopping cart views
│   │   ├── Orders/                  # Order history
│   │   └── Shared/                  # Layout & partials
│   └── wwwroot/
│       ├── css/                     # Pico CSS + custom styles
│       └── js/                      # Toast system
├── tests/
│   ├── SwapShop.UnitTests/          # Unit tests
│   └── SwapShop.IntegrationTests/   # Integration tests
└── README.md                         # This file
```

## Key Concepts Demonstrated

### 1. Event Chains

Configure what happens when events are triggered:

```csharp
// In Program.cs - Configure Swap.Htmx with view search paths and event chains
builder.Services.AddSwapHtmx(options =>
{
    // Configure view search paths for cross-controller OOB swaps
    options.PartialViewSearchPaths.Add("Cart");
    options.PartialViewSearchPaths.Add("Products");
    options.PartialViewSearchPaths.Add("Orders");
    
    // Configure event chains
    EventChainConfiguration.ConfigureEventChains(options.EventBus);
});

// In EventChainConfiguration.cs
config.When(CartEvents.ItemAdded)
    .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => GetCartCount())
    .RefreshPartial(CartElements.Items, CartViews.Items, ctx => GetCart())
    .Toast("Item added to cart", ToastType.Success);

config.When(OrderEvents.Created)
    .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
    .Toast("Order placed successfully!", ToastType.Success)
    .Redirect("/Orders")
    .AlsoTrigger(NotificationEvents.OrderConfirmation);
```

### 2. Out-of-Band Swaps

Update multiple parts of the page simultaneously:

```csharp
return SwapResponse()
    .AlsoUpdate("cart-badge", "_CartBadge", count)      // Update header
    .AlsoUpdate("cart-total", "_CartTotal", cart)       // Update footer
    .WithSuccessToast("Cart updated")                   // Show feedback
    .Build();
```

### 3. HTMX Integration

Views automatically adapt to HTMX requests:

```razor
@* _Layout.cshtml wraps full page loads *@
@* _ViewStart.cshtml detects HTMX and skips layout *@

<a href="/Products" 
   hx-get="/Products" 
   hx-target="main" 
   hx-swap="innerHTML" 
   hx-push-url="true">
    Products
</a>
```

### 4. Toast Notifications

Four types with automatic positioning and dismiss:

```csharp
response.ShowSuccessToast("Item added!");
response.ShowErrorToast("Out of stock");
response.ShowWarningToast("Low stock");
response.ShowInfoToast("Price updated");
```

## Features to Explore

### Shopping Flow

1. **Browse Products** - Grid view with search and categories
2. **View Details** - Click product name for full details
3. **Add to Cart** - Instant feedback with toast and badge update
4. **Manage Cart** - Update quantities, remove items, clear cart
5. **Checkout** - Review order and place
6. **Order History** - View past orders with status simulation

### Testing Event Chains

1. Add item to cart → Watch badge, items list, and toast update together
2. Clear cart → Notice how footer disappears via event chain
3. Place order → Redirects to orders page after toast
4. Simulate order status → Status updates without page reload

### Testing Browser Navigation

1. Navigate through pages using HTMX links
2. Use browser back button → Should work smoothly
3. Navigate back multiple times then add to cart → Toast shows only once (history restore detection)

## Common Patterns

### Pattern 0: Session Helper (New in 0.5.0)
```csharp
// Automatic session cookie persistence - no manual initialization needed
private string SessionId => GetOrInitializeSessionId();

// Old pattern (still works, but not recommended):
private string SessionId
{
    get
    {
        if (!HttpContext.Session.Keys.Contains("_initialized"))
        {
            HttpContext.Session.SetString("_initialized", "true");
        }
        return HttpContext.Session.Id;
    }
}
```

### Pattern 1: Simple Page Load
```csharp
public IActionResult Index() => SwapView(_products);
```

### Pattern 2: Form Submission with Feedback
```csharp
[HttpPost]
public IActionResult Add(int id)
{
    _service.Add(id);
    return SwapResponse()
        .AlsoUpdate("list", "_List", _service.GetAll())
        .WithSuccessToast("Added!")
        .Build();
}
```

### Pattern 3: Event-Driven Update
```csharp
[HttpPost]
public IActionResult Delete(int id)
{
    _service.Delete(id);
    return SwapEvent(MyEvents.Deleted, new { Id = id }).Build();
}
```

## Learning Resources

### In This Demo

- **Controllers/** - See all three API tiers in action
- **Events/** - Learn decentralized event configuration patterns
- **Views/** - HTMX markup examples

### Library Documentation

- **lib/Swap.Htmx/README.md** - Full library documentation
- **lib/Swap.Htmx/docs/Events.md** - Event system deep dive
- **lib/Swap.Htmx/docs/DebuggingAndLogging.md** - Debug logging guide

## Tips for Development

### Enable Debug Logging

See exactly what's happening under the hood:

```json
{
  "Logging": {
    "LogLevel": {
      "Swap.Htmx": "Debug"
    }
  }
}
```

### Use Browser DevTools

- **Network tab** - Watch HX-Request/HX-Trigger headers
- **Console** - See HTMX events firing
- **Elements** - Inspect OOB swap targets

### Common Gotchas

1. **OOB targets must have IDs** - `<div id="cart-badge">` not just `<div>`
2. **Event chains need matching event keys** - Case-sensitive
3. **Partials need Layout = null** - Prevents nested layouts
4. **View search paths** - Configure paths in `AddSwapHtmx()` for cross-controller OOB swaps

## Next Steps

### Extend the Demo

- Add user authentication
- Implement product search
- Add payment processing
- Create admin panel
- Add real-time notifications with SSE

### Apply to Your Project

1. Install `Swap.Htmx` package (version 0.5.0+)
2. Configure in Program.cs:
   ```csharp
   builder.Services.AddSwapHtmx(options =>
   {
       // Optional: Add view search paths for cross-controller OOB swaps
       options.PartialViewSearchPaths.Add("YourController");
       
       // Optional: Configure event chains
       options.EventBus.When(YourEvents.Created)
           .RefreshPartial("target-id", "_PartialView")
           .Toast("Success!", ToastType.Success);
   });
   ```
3. Inherit from `SwapController`
4. Use `GetOrInitializeSessionId()` for automatic session management
5. Use `SwapView()`, `SwapResponse()`, or `SwapEvent()`
6. Configure event chains as needed

## Questions or Issues?

- Check the library README: `lib/Swap.Htmx/README.md`
- Review event documentation: `lib/Swap.Htmx/docs/Events.md`
- Enable debug logging to see what's happening
- Examine the demo controllers for patterns

---

**Happy coding with Swap.Htmx!** 🚀

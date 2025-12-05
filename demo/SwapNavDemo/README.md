# SwapNavDemo

Demonstrates the `<swap-nav>` tag helper and `AutoSuppressLayout` features for SPA-style navigation in Swap.Htmx.

## Features Demonstrated

1. **`<swap-nav>` Tag Helper** — Clean navigation links without verbose HTMX attributes
2. **Auto-Layout Suppression** — HTMX requests get partials, browser requests get full pages
3. **Programmatic Navigation** — `.WithNavigation()` for server-side navigation
4. **Push URL Control** — Navigate with or without URL history
5. **Navigation with Toasts** — Combine navigation with notifications

## Running

```bash
dotnet run
```

Then open http://localhost:5000

## Key Patterns

### `<swap-nav>` Tag Helper

```html
<!-- Simple navigation link -->
<swap-nav to="/products">Products</swap-nav>

<!-- With styling -->
<swap-nav to="/orders" class="nav-link active">Orders</swap-nav>

<!-- Disable URL push (for modals/tabs) -->
<swap-nav to="/modal-content" push-url="false">Open Modal</swap-nav>

<!-- Custom target -->
<swap-nav to="/sidebar-content" hx-target="#sidebar">Load Sidebar</swap-nav>
```

### Configuration

```csharp
// Program.cs
builder.Services.AddSwapHtmx(options =>
{
    options.DefaultNavigationTarget = "#main-content";
    options.AutoSuppressLayout = true;
});
```

### _ViewStart.cshtml

```razor
@using Swap.Htmx
@{
    Layout = Context.ShouldSuppressLayout() ? null : "_Layout";
}
```

### Programmatic Navigation

```csharp
// Simple navigation
return this.SwapResponse()
    .WithNavigation("/inbox")
    .Build();

// Navigation to custom target
return this.SwapResponse()
    .WithNavigation("/settings", target: "#sidebar")
    .Build();

// Navigation with toast
return this.SwapResponse()
    .WithNavigation($"/orders/{order.Id}")
    .WithCreatedToast("Order", order.Number)
    .Build();
```

## See Also

- [SwapNavTagHelper Documentation](../../lib/Swap.Htmx/docs/SwapNavTagHelper.md)
- [Navigation Documentation](../../lib/Swap.Htmx/docs/Navigation.md)

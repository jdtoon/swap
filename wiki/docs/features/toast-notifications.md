# Toast Notifications

Toast notifications provide lightweight, non-intrusive feedback to users. The Swap.Htmx framework includes built-in toast support that works purely through HTMX triggers - no JavaScript configuration required.

## Quick Start

### 1. Add Toast Container to Layout

```razor
<!-- In _Layout.cshtml -->
@await Html.PartialAsync("_ToastContainer")
```

### 2. Show Toasts from Controllers

```csharp
public class ProductController : SwapController
{
    public async Task<IActionResult> Create(ProductDto dto)
    {
        await _service.CreateAsync(dto);
        
        Response.ShowSuccessToast("Product created successfully!");
        
        return SwapView("Success");
    }
}
```

## Toast Types

The framework provides four toast types with different colors:

```csharp
// Green toast for successful operations
Response.ShowSuccessToast("Operation completed!");

// Red toast for errors
Response.ShowErrorToast("Something went wrong!");

// Yellow/orange toast for warnings
Response.ShowWarningToast("Please review your input.");

// Blue toast for informational messages
Response.ShowInfoToast("Processing in background...");
```

## Positioning

Toasts appear in the **top-right corner** by default. You can change position using the `position` parameter:

```csharp
Response.ShowToast("Message", "success", position: "bottom-right");
Response.ShowToast("Message", "info", position: "top-left");
Response.ShowToast("Message", "warning", position: "bottom-left");
```

## How It Works

Toast notifications use HTMX's trigger mechanism:

1. Controller sets `HX-Trigger` header: `{"showToast":{"message":"...","type":"success"}}`
2. HTMX fires `showToast` event on the client
3. Event listener creates toast element and adds to container
4. CSS animations handle slide-in and auto-dismiss after 3 seconds

## Multiple Toasts

Multiple toasts stack vertically in the container:

```csharp
public IActionResult BulkOperation()
{
    // Process items...
    
    Response.ShowSuccessToast($"{successCount} items processed");
    Response.ShowWarningToast($"{warningCount} items need attention");
    
    return SwapView();
}
```

## Advanced: Custom Toast CSS

The default toast styles can be customized in your CSS:

```css
.toast-success {
    background-color: #your-brand-color;
}

.toast {
    border-radius: 8px;
    min-width: 350px;
}
```

## Working with Events

Toasts work alongside the event system:

```csharp
// In Startup/Program.cs
builder.Services.AddSwapHtmx(events =>
{
    // Show toast whenever product is created
    events.Chain(ProductEvents.Created, UiEvents.ShowSuccessToast);
});

// In controller
public async Task<IActionResult> Create(ProductDto dto)
{
    await _service.CreateAsync(dto);
    
    // Emit domain event (automatically triggers toast via chain)
    await _publisher.EmitAsync(ProductEvents.Created);
    
    return SwapView();
}
```

## Client-Side Setup

### Required Files

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
        toast.className = 'toast toast-' + (detail.type || 'info');
        toast.textContent = detail.message || 'Operation completed';
        container.appendChild(toast);
        
        setTimeout(() => toast.remove(), 3500);
    });
</script>
```

**wwwroot/css/htmx-toast.css** (see [full example](https://github.com/jdtoon/swap/blob/main/framework/Swap.Htmx/TOASTS.md))

## Best Practices

✅ **Use appropriate toast types** - Success for confirmations, error for failures, warning for validation issues, info for background processes

✅ **Keep messages concise** - Toasts auto-dismiss, so messages should be brief (under 50 characters ideal)

✅ **Combine with visual changes** - Toast confirms action, but also update the UI to reflect the change

❌ **Don't overuse** - Too many toasts become noise. Reserve for important user actions.

❌ **Don't use for critical errors** - Use modal dialogs or inline validation for errors requiring user action

## See Also

- [Out-of-Band Swaps](./oob-swaps.md) - Update multiple page sections
- [Event System](./event-system.md) - Chain toasts to domain events
- [Full Toast Documentation](https://github.com/jdtoon/swap/blob/main/framework/Swap.Htmx/TOASTS.md) - Complete API reference

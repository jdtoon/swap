# Toast Notification System

## Overview

The Swap.Htmx toast notification system provides server-side toast notifications that work seamlessly with HTMX. Toasts are triggered via HTTP response headers and displayed client-side with minimal JavaScript.

## Server-Side Usage

### Basic Usage

```csharp
public class MyController : SwapController
{
    public IActionResult SaveData()
    {
        // ... save logic ...
        
        Response.ShowSuccessToast("Data saved successfully!");
        return SwapView();
    }
    
    public IActionResult DeleteItem(int id)
    {
        try
        {
            // ... delete logic ...
            Response.ShowSuccessToast("Item deleted");
            return SwapView();
        }
        catch (Exception ex)
        {
            Response.ShowErrorToast($"Delete failed: {ex.Message}");
            return SwapView();
        }
    }
}
```

### Available Methods

```csharp
// Generic method with all options
Response.ShowToast("Message", ToastType.Info, ToastPosition.TopRight);

// Convenience methods
Response.ShowSuccessToast("Operation successful!");
Response.ShowErrorToast("Something went wrong");
Response.ShowWarningToast("Please review this action");
Response.ShowInfoToast("FYI: Something happened");
```

### Toast Types

- `ToastType.Success` - Green, indicates successful operation
- `ToastType.Error` - Red, indicates failure or error
- `ToastType.Warning` - Yellow/orange, indicates caution
- `ToastType.Info` - Blue, informational messages

### Toast Positions

- `ToastPosition.TopRight` (default)
- `ToastPosition.TopLeft`
- `ToastPosition.BottomRight`
- `ToastPosition.BottomLeft`

## Client-Side Integration

### Required HTML

Add a toast container to your layout:

```html
<div id="toast-container" aria-live="polite" aria-atomic="true"></div>
```

### With Bulma CSS

```html
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="~/lib/bulma/css/bulma.min.css" />
    <script src="https://unpkg.com/htmx.org@2.0.0"></script>
    <style>
        #toast-container {
            position: fixed;
            top: 1rem;
            right: 1rem;
            z-index: 9999;
            max-width: 400px;
        }
        .toast {
            margin-bottom: 0.5rem;
            animation: slideIn 0.3s ease-out;
        }
        @@keyframes slideIn {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
    </style>
</head>
<body>
    <!-- Your content -->
    
    <div id="toast-container" aria-live="polite" aria-atomic="true"></div>
    
    <script>
        // Listen for showToast events from server
        document.body.addEventListener('showToast', function(evt) {
            const { type, message, position } = evt.detail;
            
            // Create toast element using Bulma classes
            const toast = document.createElement('div');
            toast.className = `notification is-${type} toast`;
            toast.innerHTML = `
                <button class="delete" onclick="this.parentElement.remove()"></button>
                ${message}
            `;
            
            // Add to container
            const container = document.getElementById('toast-container');
            container.appendChild(toast);
            
            // Auto-remove after 5 seconds
            setTimeout(() => {
                toast.style.transition = 'opacity 0.3s';
                toast.style.opacity = '0';
                setTimeout(() => toast.remove(), 300);
            }, 5000);
        });
    </script>
</body>
</html>
```

### Custom CSS

If not using Bulma, provide your own toast styles:

```css
#toast-container {
    position: fixed;
    top: 1rem;
    right: 1rem;
    z-index: 9999;
    max-width: 400px;
}

.toast {
    padding: 1rem 1.5rem;
    margin-bottom: 0.5rem;
    border-radius: 0.375rem;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    background: white;
    border-left: 4px solid;
    animation: slideIn 0.3s ease-out;
}

.toast.success {
    border-color: #10b981;
    background-color: #d1fae5;
    color: #065f46;
}

.toast.error {
    border-color: #ef4444;
    background-color: #fee2e2;
    color: #991b1b;
}

.toast.warning {
    border-color: #f59e0b;
    background-color: #fef3c7;
    color: #92400e;
}

.toast.info {
    border-color: #3b82f6;
    background-color: #dbeafe;
    color: #1e40af;
}

@keyframes slideIn {
    from {
        transform: translateX(100%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}
```

## How It Works

1. **Server calls** `Response.ShowToast()` which sets the `HX-Trigger` response header
2. **HTMX receives** the response and fires a custom `showToast` event
3. **JavaScript listener** catches the event and creates/displays the toast element
4. **Auto-dismiss** timer removes the toast after 5 seconds

## Header Merging

The toast system intelligently merges with existing `HX-Trigger` headers:

```csharp
// Both events will be triggered
Response.Headers["HX-Trigger"] = "refreshData";
Response.ShowSuccessToast("Data saved!");
// Result: HX-Trigger: {"refreshData": null, "showToast": {...}}
```

## Testing

The toast system is fully tested with 18 unit tests covering:
- Header creation and merging
- JSON escaping
- All toast types
- All toast positions
- Convenience methods
- Multiple sequential toasts

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~SwapToastExtensionsTests"
```

## Isolated Operation

The toast system works **completely independently** of the Swap event system:
- No event chains required
- No EventKey definitions needed
- Pure HTMX header-based communication
- Zero framework coupling

Perfect for simple notifications without event orchestration overhead.

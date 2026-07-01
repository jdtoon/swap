# SwapErrorBoundaries: Graceful Error Handling

When an HTMX request fails (e.g., throwing a 500 Exception), ASP.NET Core usually renders a full HTML error page (Developer Exception Page or Custom Error Page).

If the request was targeting a specific element (like `hx-target="#panel"`), this full HTML page gets inserted **inside** that element, breaking your layout and user experience.

SwapErrorBoundaries solves this by intercepting exceptions in HTMX requests and returning a **Toast Notification** or **Modal** instead.

---

## Quick Start

### 1. Enable in Program.cs

Configure error handling when adding SwapHtmx services:

```csharp
builder.Services.AddSwapHtmx(options => 
{
    // Enable HTMX-specific error handling
    options.ErrorHandling.Enabled = true;
    
    // Optional: Show exception message in toast (useful for Dev)
    options.ErrorHandling.ShowExceptionDetails = builder.Environment.IsDevelopment();
    
    // Optional: Custom error view (default is "_SwapErrorToast")
    options.ErrorHandling.ErrorViewName = "_MyErrorAlert"; 
});
```

### 2. Add the Toast Container

The middleware expects a container in your layout to verify OOB swaps against, although best practice is to include an empty container where the toast can land (if using OOB) or simply let the toast append itself.

Ideally, use `hx-swap-oob="true"` in your error view.

### 3. Create the Error View

Create `Views/Shared/_SwapErrorToast.cshtml`. The model passed is `SwapErrorModel`.

```html
@using Swap.Htmx.Models
@model SwapErrorModel

<!-- Use OOB Swap to append this toast to your toast container -->
<div id="toast-container" hx-swap-oob="beforeend">
    <div class="toast error">
        <strong>Error:</strong> @Model.Message
        @if (Model.Exception != null) {
            <small>@Model.Exception.GetType().Name</small>
        }
        <button onclick="this.parentElement.remove()">Dismiss</button>
    </div>
</div>
```

---

## How It Works

1.  ** interception**: The `SwapErrorMiddleware` wraps the request pipeline.
2.  **Detection**: If an unhandled exception occurs **AND** `Request.IsHtmxRequest()` is true:
    - It catches the exception.
    - It logs the error using `ILogger`.
    - It clears the response.
    - It renders the configured Partial View (e.g., `_SwapErrorToast`).
3.  **Response**: The response is sent with `200 OK` (so HTMX processes it) but usually with `hx-reswap="none"` to prevent the main target from being overwritten, relying on the **OOB Swap** in the error view to show the notification.

---

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `Enabled` | `false` | Master switch for the feature. |
| `ErrorViewName` | `_SwapErrorToast` | Name of the partial view to render. |
| `ShowExceptionDetails` | `false` | If true, `Model.Message` contains the (HTML-encoded) exception message. If false, a generic message plus a request correlation id is shown, and full details are logged server-side. |

---

## Safe Output (Built-in Fallback)

The built-in fallback error response is safe by default:

- **All values are HTML-encoded.** Interpolated content (including any exception text) is HTML-encoded, so a crafted exception message cannot inject markup or script into the page — the fallback is no longer reflected-XSS-vulnerable.
- **The raw exception message is not shown by default.** With `ShowExceptionDetails = false`, the client sees a generic message plus a request **correlation id**; the full exception (message, stack trace) is logged **server-side** via `ILogger`. Surface the correlation id to users so support can find the matching log entry.
- Enable `ShowExceptionDetails` only in Development.

---

## Migration from Client-Side Handling

Previously, you might have used `htmx:responseError` events in JavaScript to handle 500s.

```javascript
// Old way (Client-side)
document.body.addEventListener('htmx:responseError', function(evt) {
    alert("Server Error!");
});
```

**New way (Server-side):**
Middleware handles it automatically. No JavaScript required.

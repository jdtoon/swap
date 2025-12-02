# Navigation with .WithNavigation()

The `.WithNavigation()` method provides fluent API access to HTMX's `HX-Location` header for SPA-style navigation.

## Why Use WithNavigation()?

When you need to navigate the user to a new URL after an action **and** show a toast or trigger events, using a regular redirect loses any response headers. The `HX-Location` header tells HTMX to perform an AJAX navigation instead, preserving the ability to show toasts and trigger client events.

```csharp
// ❌ Problem: Redirect loses toast
return Redirect("/dashboard");
Response.HxTrigger("showToast", "Saved!"); // This won't work!

// ✅ Solution: WithNavigation keeps everything
return this.SwapResponse()
    .WithNavigation("/dashboard")
    .WithSuccessToast("Saved!")
    .Build();
```

## Basic Usage

### Simple Navigation

```csharp
public IActionResult Save(MyForm form)
{
    _service.Save(form);
    
    return this.SwapResponse()
        .WithNavigation("/items")
        .WithSuccessToast("Saved successfully!")
        .Build();
}
```

### Navigation with Target

Navigate and swap into a specific element (useful for partial page updates):

```csharp
return this.SwapResponse()
    .WithNavigation("/inbox", target: "#main-content")
    .Build();
```

### Navigation with Swap Mode

Control how the content is swapped:

```csharp
return this.SwapResponse()
    .WithNavigation("/notifications", target: "#alerts", swap: SwapMode.BeforeEnd)
    .Build();
```

## Full Control with NavigationOptions

For advanced scenarios, use the `NavigationOptions` record:

```csharp
return this.SwapResponse()
    .WithNavigation(new NavigationOptions
    {
        Path = "/orders/123",
        Target = "#order-detail",
        Swap = SwapMode.OuterHTML,
        Select = "#order-content",     // CSS selector to pick from response
        Values = new { refresh = true }, // Additional values to send
        Headers = new { ["X-Custom"] = "value" }
    })
    .Build();
```

### NavigationOptions Properties

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | The URL to navigate to (required) |
| `Target` | `string?` | CSS selector for target element |
| `Swap` | `SwapMode?` | How to swap content (OuterHTML, InnerHTML, etc.) |
| `Source` | `string?` | Element that triggered the request |
| `Select` | `string?` | CSS selector to pick content from response |
| `Values` | `object?` | Additional values to include in request |
| `Headers` | `object?` | Additional headers to include |

## Common Patterns

### Post-Save Redirect with Toast

```csharp
[HttpPost]
public IActionResult CreateOrder(OrderForm form)
{
    var order = _orderService.Create(form);
    
    return this.SwapResponse()
        .WithNavigation($"/orders/{order.Id}")
        .WithCreatedToast("Order", order.OrderNumber)
        .Build();
}
```

### Wizard Step Navigation

```csharp
public IActionResult CompleteStep1(Step1Form form)
{
    // Save step data
    _wizard.SaveStep1(form);
    
    return this.SwapResponse()
        .WithNavigation("/wizard/step2", target: "#wizard-content")
        .WithInfoToast("Step 1 complete!")
        .Build();
}
```

### Conditional Navigation

```csharp
[HttpPost]
public IActionResult Checkout()
{
    if (_cart.IsEmpty)
    {
        return this.SwapResponse()
            .WithNavigation("/products")
            .WithWarningToast("Your cart is empty")
            .Build();
    }
    
    return this.SwapResponse()
        .WithNavigation("/checkout/payment")
        .Build();
}
```

### Modal Close with Navigation

```csharp
[HttpPost]
public IActionResult SaveAndClose(EditForm form)
{
    _service.Update(form);
    
    return this.SwapResponse()
        .WithNavigation("/items")
        .WithSuccessToast("Changes saved!")
        .WithTrigger("closeModal")  // Close modal first
        .Build();
}
```

## How It Works

When you call `.WithNavigation()`, the builder stores the navigation options. When `.Build()` executes:

1. If navigation is set, the `HX-Location` header is added with JSON containing the path and options
2. No HTML content is returned (HTMX handles the navigation)
3. Other headers (toasts, triggers) are still sent and processed

The client receives something like:

```http
HX-Location: {"path":"/orders/123","target":"#main-content"}
HX-Trigger: {"showToast":{"message":"Order created!","type":"success"}}
```

HTMX then:
1. Shows the toast
2. Makes an AJAX request to `/orders/123`
3. Swaps the response into `#main-content`

## Demo

See `demo/SwapNavDemo` for a complete working example with:
- Basic navigation
- Navigation with targets and swap modes
- Wizard flow
- Modal integration
- Conditional navigation based on state

## See Also

- [Out-of-Band Swaps](OutOfBandSwaps.md) - Update multiple elements
- [CRUD Toasts](CrudToasts.md) - Standard toast messages
- [State Management](StateManagement.md) - Preserving state across navigations

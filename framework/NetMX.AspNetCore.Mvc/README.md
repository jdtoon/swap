# NetMX.AspNetCore.Mvc

**ASP.NET Core MVC extensions and HTMX helpers for building server-side HTML with AJAX.**

This package provides MVC extensions specifically designed for HTMX-first development, including request detection, type-safe event triggering, and response header helpers.

## Overview

NetMX.AspNetCore.Mvc enhances ASP.NET Core MVC with:
- **HTMX Request Detection**: Know when requests come from HTMX
- **Type-Safe Event Triggering**: Use `Events.*` pattern for compile-time safety
- **HTMX Response Headers**: Easy helpers for `HX-Retarget`, `HX-Reswap`, etc.
- **Multiple Events**: Trigger multiple HTMX events in a single response

Perfect for building dynamic UIs without heavy JavaScript frameworks.

## Installation

```bash
dotnet add package NetMX.AspNetCore.Mvc
```

## Key Features

### 1. HTMX Request Detection

Check if a request came from HTMX:

```csharp
public IActionResult Index()
{
    if (Request.IsHtmx())
    {
        return PartialView("_UserList", users);  // Just the content
    }
    
    return View(users);  // Full page with layout
}
```

### 2. Type-Safe Event Triggering

Trigger HTMX events with compile-time safety using the Event Registry:

```csharp
using NetMX.Events;

[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    var product = await _service.CreateAsync(dto);
    
    // Type-safe event (IntelliSense support!)
    this.HxTrigger(Events.Product.Created, new { productId = product.Id });
    
    return Ok();
}
```

**Benefits**:
- ✅ IntelliSense for event names
- ✅ Compile-time checking (no typos!)
- ✅ Refactoring support
- ✅ Self-documenting code

### 3. HTMX Response Headers

Control HTMX behavior from your controllers:

```csharp
[HttpDelete("{id}")]
public IActionResult Delete(int id)
{
    _service.Delete(id);
    
    this.HxTrigger(Events.Product.Deleted, new { productId = id });
    this.HxReswap(HtmxSwap.Delete);  // Remove the target element
    
    return Ok();
}
```

**Available Headers**:
- `HxRetarget(selector)` - Change the target element
- `HxReswap(swap)` - Change swap behavior
- `HxRedirect(url)` - Client-side redirect
- `HxRefresh()` - Refresh the page

### 4. Multiple Events

Trigger multiple events in one response:

```csharp
[HttpPut("{id}")]
public IActionResult Update(int id, UpdateProductDto dto)
{
    _service.Update(id, dto);
    
    // Trigger multiple events
    this.HxTrigger(Events.Product.Updated, new { productId = id });
    this.HxTrigger(Events.Stats.Changed, null);
    
    return Ok();
}
```

## Usage

### Basic Controller Example

```csharp
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;

public class ProductController : Controller
{
    private readonly IProductService _service;
    
    public ProductController(IProductService service)
    {
        _service = service;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        var products = _service.GetAll();
        
        if (Request.IsHtmx())
        {
            return PartialView("_List", products);
        }
        
        return View(products);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return PartialView("_Form", dto);
        
        var product = await _service.CreateAsync(dto);
        
        // Type-safe event triggering
        this.HxTrigger(Events.Product.Created, new { productId = product.Id });
        
        return Ok();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        
        // Trigger event and remove element
        this.HxTrigger(Events.Product.Deleted, new { productId = id });
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
```

### View Example with Type-Safe Events

```html
@using NetMX.Events

<!-- List container that auto-refreshes on events -->
<div id="list-container" 
     hx-get="/Product/List" 
     hx-trigger="load, @Events.Product.Created from:body, @Events.Product.Updated from:body">
</div>

<!-- Edit button -->
<button hx-get="/Product/Edit/@item.Id" 
        hx-target="#form-container">
    Edit
</button>

<!-- Delete button with confirmation -->
<button hx-delete="/Product/Delete/@item.Id" 
        hx-target="#row-@item.Id"
        hx-confirm="Are you sure?">
    Delete
</button>
```

## API Reference

### Request Extensions

**`Request.IsHtmx()`**  
Returns `true` if the request came from HTMX.

```csharp
if (Request.IsHtmx())
{
    return PartialView("_Content");
}
```

### Controller Extensions

**`HxTrigger(string eventName, object? payload = null)`**  
Triggers a type-safe HTMX event.

```csharp
this.HxTrigger(Events.Product.Created, new { productId = 123 });
```

**`HxRetarget(string selector)`**  
Changes the target element for the response.

```csharp
this.HxRetarget("#different-container");
```

**`HxReswap(HtmxSwap swap)`**  
Changes how content is swapped.

```csharp
this.HxReswap(HtmxSwap.OuterHTML);
this.HxReswap(HtmxSwap.Delete);  // Remove element
```

**`HxRedirect(string url)`**  
Client-side redirect.

```csharp
this.HxRedirect("/products");
```

**`HxRefresh()`**  
Refreshes the page.

```csharp
this.HxRefresh();
```

### HtmxSwap Options

```csharp
public enum HtmxSwap
{
    InnerHTML,   // Replace inner content (default)
    OuterHTML,   // Replace entire element
    BeforeBegin, // Insert before element
    AfterBegin,  // Insert at start of element
    BeforeEnd,   // Insert at end of element
    AfterEnd,    // Insert after element
    Delete,      // Remove the element
    None         // No swap
}
```

## Dependencies

- `NetMX.Core` - Core utilities and conventions
- `NetMX.Events` - Type-safe event system
- `Microsoft.AspNetCore.Mvc` - ASP.NET Core MVC

## Related Packages

- **[NetMX.Htmx](../NetMX.Htmx/)** - HTMX static files and CDN
- **[NetMX.Events](../NetMX.Events/)** - Event Registry system

## Documentation

- [HTMX Patterns Guide](../../docs/HTMX-PATTERNS.md) - Complete HTMX patterns
- [Event Registry Architecture](../../docs/EVENT-REGISTRY-ARCHITECTURE.md) - Type-safe events
- [Quick Start Guide](../../docs/QUICK-START.md) - Getting started

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
# Migration Guide

This guide walks you through migrating an existing ASP.NET Core MVC application to use Swap.Htmx for reactive, partial-based UI updates.

---

## Migration Overview

| Before | After |
|--------|-------|
| Full page reloads | Partial swaps |
| `<a href="...">` | `<a hx-get="..." hx-target="...">`|
| `return View(model)` | `return SwapView(model)` |
| `return Redirect("/path")` | `SwapResponse().WithRedirect("/path")` |
| Scattered AJAX calls | Event-driven updates |
| Client-side state management | Server-rendered state |

---

## Step 1: Install and Configure

### Add the Package

```bash
dotnet add package Swap.Htmx
```

### Update Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();  // Add this

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();  // Add this (before MapControllers)
app.MapControllers();

app.Run();
```

### Update Layout

In `Views/Shared/_Layout.cshtml`, add HTMX and Swap assets:

You can use either CDN assets or LibMan-managed local assets. See [Client Assets](ClientAssets.md).

```html
<head>
    <!-- Existing content... -->
    
    <!-- Add Swap.Htmx styles (for toasts) -->
    <link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />
    
    <!-- Add HTMX -->
    <script src="https://unpkg.com/htmx.org@2.0.8"></script>
    
    <!-- Add Swap.Htmx client script -->
    <script src="~/_content/Swap.Htmx/js/swap.client.js"></script>
</head>
```

---

## Step 2: Migrate Controllers

### Option A: Inherit from SwapController

Change your controller base class:

```csharp
// BEFORE
public class ProductsController : Controller

// AFTER
public class ProductsController : SwapController
```

### Option B: Use Extension Methods (No Inheritance)

If you can't change inheritance, use extension methods:

```csharp
public class ProductsController : Controller
{
    public IActionResult Index()
    {
        return this.SwapView(model);  // Extension method
    }
}
```

---

## Step 3: Migrate Views

### Replace View() with SwapView()

```csharp
// BEFORE: Always returns full page
public IActionResult Details(int id)
{
    var product = _db.Products.Find(id);
    return View(product);
}

// AFTER: Partial for HTMX, full for browser
public IActionResult Details(int id)
{
    var product = _db.Products.Find(id);
    return SwapView(product);
}
```

### Create Partial Versions of Views

For any view that should be swappable, ensure it can render without layout:

```html
<!-- Views/Products/Details.cshtml -->
@model Product

<!-- This view should work with or without layout -->
<div id="product-details">
    <h2>@Model.Name</h2>
    <p>@Model.Description</p>
    <p class="price">$@Model.Price</p>
</div>
```

The `SwapView()` method automatically:
- Returns the view **with layout** for normal browser requests
- Returns the view **without layout** for HTMX requests

---

## Step 4: Migrate Navigation

### Convert Links

```html
<!-- BEFORE: Full page reload -->
<a href="/products">Products</a>

<!-- AFTER: Partial swap -->
<a hx-get="/products"
   hx-target="#main-content"
   hx-push-url="true">
    Products
</a>
```

### Convert Forms

```html
<!-- BEFORE: Full page submission -->
<form method="post" action="/products/create">
    <!-- fields -->
    <button type="submit">Create</button>
</form>

<!-- AFTER: Partial swap with feedback -->
<form hx-post="/products/create"
      hx-target="#product-form"
      hx-swap="outerHTML">
    <!-- fields -->
    <button type="submit">Create</button>
</form>
```

### Update Form Handlers

```csharp
// BEFORE
[HttpPost]
public IActionResult Create(Product product)
{
    if (!ModelState.IsValid)
        return View(product);
    
    _db.Products.Add(product);
    _db.SaveChanges();
    return RedirectToAction("Index");
}

// AFTER
[HttpPost]
public IActionResult Create(Product product)
{
    if (!ModelState.IsValid)
    {
        return SwapResponse()
            .WithView("_ProductForm", product)
            .WithErrorToast("Please fix validation errors")
            .Build();
    }
    
    _db.Products.Add(product);
    _db.SaveChanges();
    
    return SwapResponse()
        .WithTrigger(ProductEvents.Created)
        .WithSuccessToast("Product created!")
        .Build();
}
```

---

## Step 5: Add Event Coordination

### Define Events

```csharp
// Events/ProductEvents.cs
using Swap.Htmx.Events;

public static class ProductEvents
{
    public static readonly EventKey Created = new("product.created");
    public static readonly EventKey Updated = new("product.updated");
    public static readonly EventKey Deleted = new("product.deleted");
}
```

### Make Components Listen for Events

```html
<!-- Products list reloads when products change -->
<div id="products-list"
     hx-get="/products/list"
     hx-trigger="product.created from:body, product.updated from:body, product.deleted from:body">
    @await Html.PartialAsync("_ProductList", Model.Products)
</div>
```

### Trigger Events from Actions

```csharp
[HttpPost]
public IActionResult Create(Product product)
{
    _db.Products.Add(product);
    _db.SaveChanges();
    
    return SwapResponse()
        .WithTrigger(ProductEvents.Created, new { id = product.Id })
        .WithSuccessToast("Product created!")
        .Build();
}
```

---

## Step 6: Migrate Multi-Part Updates

### BEFORE: Multiple AJAX Calls

```javascript
// Old approach: separate calls to update each part
function addToCart(productId) {
    $.post('/cart/add', { productId }, function() {
        $.get('/cart/count', function(html) {
            $('#cart-count').html(html);
        });
        $.get('/cart/total', function(html) {
            $('#cart-total').html(html);
        });
        showToast('Added to cart!');
    });
}
```

### AFTER: Single Response with OOB Swaps

```csharp
[HttpPost]
public IActionResult AddToCart(int productId)
{
    _cart.Add(productId);
    
    return SwapResponse()
        .WithView("_ProductAdded", productId)
        .AlsoUpdate("cart-count", "_CartCount", _cart.Count)
        .AlsoUpdate("cart-total", "_CartTotal", _cart.Total)
        .WithSuccessToast("Added to cart!")
        .Build();
}
```

```html
<button hx-post="/cart/add"
        hx-vals='{"productId": @product.Id}'
        hx-target="#product-feedback">
    Add to Cart
</button>
```

---

## Step 7: Migrate Modals/Dialogs

### BEFORE: JavaScript Modal

```javascript
function showEditModal(productId) {
    $.get('/products/edit/' + productId, function(html) {
        $('#modal-container').html(html);
        $('#edit-modal').modal('show');
    });
}
```

### AFTER: HTMX-Driven Modal

```html
<!-- Trigger button -->
<button hx-get="/products/edit/@product.Id"
        hx-target="#modal-container"
        hx-swap="innerHTML">
    Edit
</button>

<!-- Modal container -->
<div id="modal-container"></div>
```

```csharp
public IActionResult Edit(int id)
{
    var product = _db.Products.Find(id);
    return PartialView("_EditModal", product);
}

[HttpPost]
public IActionResult Edit(Product product)
{
    _db.Products.Update(product);
    _db.SaveChanges();
    
    return SwapResponse()
        .WithView("_ModalClosed")  // Clears modal
        .WithTrigger(ProductEvents.Updated)
        .WithSuccessToast("Product updated!")
        .Build();
}
```

---

## Step 8: Migrate Validation

### BEFORE: Full Page Reload on Error

```csharp
if (!ModelState.IsValid)
    return View(model);  // Full page with errors
```

### AFTER: Partial with Toast

```csharp
if (!ModelState.IsValid)
{
    return SwapResponse()
        .WithView("_ProductForm", model)  // Just the form
        .WithErrorToast("Please fix validation errors")
        .Build();
}
```

Or use the validation extension:

```csharp
if (!ModelState.IsValid)
{
    return this.SwapValidationErrors(ModelState)
        .AlsoUpdate("product-form", "_ProductForm", model)
        .Build();
}
```

---

## Migration Checklist

### Phase 1: Setup
- [ ] Install Swap.Htmx package
- [ ] Configure services in Program.cs
- [ ] Add HTMX and Swap assets to layout

### Phase 2: Controllers
- [ ] Change base class to SwapController OR use extension methods
- [ ] Replace `View()` with `SwapView()`
- [ ] Replace `Redirect()` with `SwapResponse().WithRedirect()`

### Phase 3: Views
- [ ] Ensure views work without layout (for HTMX requests)
- [ ] Add `id` attributes to swappable elements
- [ ] Create partial views for components that update independently

### Phase 4: Navigation
- [ ] Convert `<a href>` to `<a hx-get hx-target hx-push-url>`
- [ ] Convert forms to use `hx-post` and `hx-target`
- [ ] Add loading indicators with `hx-indicator`

### Phase 5: Events
- [ ] Define EventKey constants for domain events
- [ ] Add `hx-trigger` listeners to components
- [ ] Trigger events from controller actions

### Phase 6: Advanced
- [ ] Convert multi-AJAX patterns to OOB swaps
- [ ] Migrate modals to HTMX-driven pattern
- [ ] Add proper validation feedback

---

## Common Migration Issues

### Issue: Double Layout Rendering

**Symptom:** HTMX swaps show duplicate headers/footers.

**Cause:** Using `View()` instead of `SwapView()`.

**Fix:** Always use `SwapView()` or ensure `Request.IsHtmxRequest()` check.

### Issue: Broken Back Button

**Symptom:** Browser back button doesn't work after navigation.

**Cause:** Not using `hx-push-url`.

**Fix:** Add `hx-push-url="true"` to navigation links.

### Issue: Form Submits Full Page

**Symptom:** Form submission causes full page reload.

**Cause:** Form using `action` attribute instead of `hx-post`.

**Fix:** Remove `action`, add `hx-post`, `hx-target`.

### Issue: Components Don't Update

**Symptom:** Related components don't refresh after actions.

**Cause:** No event triggers or listeners.

**Fix:** Add `WithTrigger()` to actions, `hx-trigger` to components.

### Issue: State Lost Between Requests

**Symptom:** Filters/pagination reset unexpectedly.

**Cause:** State not persisted in hidden fields.

**Fix:** See [State Management Guide](StateManagement.md).

---

## Incremental Migration Strategy

You don't have to migrate everything at once. Here's a recommended approach:

### Week 1: Single Page
1. Pick one page (e.g., a list page)
2. Add HTMX to layout
3. Convert that page's controller to SwapController
4. Add `hx-get` to one component

### Week 2: Expand Page
1. Add OOB swaps for related updates
2. Add event triggers
3. Add toasts for user feedback

### Week 3: Second Page
1. Pick another page
2. Apply same patterns
3. Start identifying shared events

### Week 4+: Systematically
1. Convert remaining pages
2. Extract common events
3. Add event chain configurations
4. Remove old AJAX code

---

## Need Help?

- Check the [Anti-Patterns Guide](AntiPatterns.md) for common mistakes
- See [Multi-Component Coordination](MultiComponentCoordination.md) for complex UIs
- Review the demo apps in `demo/` for working examples

---

## Next Steps

After migration, explore:

- [Event Chains](EventChains.md) - Centralize UI update logic
- [Server-Sent Events](ServerSentEvents.md) - Real-time updates
- [Source Generators](SourceGenerators.md) - Type-safe event constants

# Form Validation

Swap.Htmx provides seamless integration for server-side validation with HTMX forms. Validation errors can be displayed inline via OOB swaps or through client-side event handling.

---

## Quick Start

### 1. Add Validation Placeholders

Use the `<swap-validation>` tag helper to create placeholders for error messages:

```html
<form hx-post="/Items/Create" hx-target="#form-container">
    <div class="form-group">
        <label for="Name">Name</label>
        <input asp-for="Name" class="form-control" />
        <swap-validation for="Name" />
    </div>
    
    <div class="form-group">
        <label for="Email">Email</label>
        <input asp-for="Email" class="form-control" />
        <swap-validation for="Email" />
    </div>
    
    <button type="submit">Create</button>
</form>
```

The `<swap-validation>` tag renders as:
```html
<span id="swap-validation-Name" 
      class="swap-validation-message" 
      data-swap-validation="Name"></span>
```

### 2. Return Validation Errors

In your controller, use `SwapValidationErrors()` when ModelState is invalid:

```csharp
[HttpPost]
public IActionResult Create(CreateItemDto dto)
{
    if (!ModelState.IsValid)
    {
        return this.SwapValidationErrors(ModelState)
            .WithView("_CreateForm", dto)
            .Build();
    }
    
    _service.Create(dto);
    return this.SwapRedirect("/Items", "Item created!");
}
```

This:
- Displays a warning toast: "Please correct the errors below."
- Triggers a `validationFailed` event with error details
- Client-side JS automatically updates the validation placeholders

---

## Validation Methods

### SwapValidationErrors (Event-Based)

The simplest approach - triggers a client-side event that updates validation elements:

```csharp
// Uses ModelState from controller
return this.SwapValidationErrors(ModelState)
    .WithView("_Form", model)
    .Build();

// Custom message
return this.SwapValidationErrors(ModelState, "Please fix the errors")
    .WithView("_Form", model)
    .Build();
```

The client-side `swap.client.js` listens for `validationFailed` and updates elements with matching `data-swap-validation` attributes.

### SwapValidationErrorsOob (OOB-Based)

For more control, use OOB swaps to update each validation element server-side:

```csharp
return this.SwapValidationErrorsOob(ModelState)
    .WithView("_Form", model)
    .Build();
```

This sends each error as an OOB swap, which is useful when:
- You need server-rendered error messages with complex formatting
- You want to update elements that aren't just text spans
- The client-side JS isn't loaded

### WithValidationErrors (Builder Method)

Add validation errors to an existing response builder:

```csharp
return this.SwapResponse()
    .WithView("_Results", data)
    .WithValidationErrors(ModelState)  // Add validation OOB swaps
    .WithWarningToast("Some fields need attention")
    .Build();
```

### ClearValidationErrors

Clear validation messages for specific fields (useful for field-level validation):

```csharp
return this.SwapResponse()
    .WithView("_NameField", validatedName)
    .ClearValidationErrors(new[] { "Name" })
    .Build();
```

---

## Tag Helper Options

The `<swap-validation>` tag helper supports several attributes:

```html
<!-- Basic usage -->
<swap-validation for="PropertyName" />

<!-- Custom CSS class -->
<swap-validation for="PropertyName" class="text-danger validation-error" />

<!-- Custom ID prefix (default is "swap-validation-") -->
<swap-validation for="PropertyName" id-prefix="err-" />
```

### Initial Error Display

If the ModelState already contains errors (e.g., on a re-rendered form), the tag helper displays them:

```html
<!-- When ModelState["Email"] has an error -->
<span id="swap-validation-Email" 
      class="swap-validation-message field-validation-error"
      data-swap-validation="Email">
    Invalid email format
</span>
```

---

## CSS Styling

Add these styles to your application:

```css
.swap-validation-message {
    display: block;
    font-size: 0.875rem;
    min-height: 1.25rem;
}

.swap-validation-message.field-validation-error {
    color: var(--danger-color, #dc3545);
}

/* Highlight invalid inputs */
input:has(+ .field-validation-error),
input:has(~ .field-validation-error) {
    border-color: var(--danger-color, #dc3545);
}
```

---

## Complete Example

### View

```html
@model CreateProductDto

<div id="create-form">
    <form hx-post="/Products/Create" 
          hx-target="#create-form"
          hx-swap="outerHTML">
        
        <div class="form-group">
            <label asp-for="Name"></label>
            <input asp-for="Name" class="form-control" />
            <swap-validation for="Name" />
        </div>
        
        <div class="form-group">
            <label asp-for="Price"></label>
            <input asp-for="Price" type="number" step="0.01" class="form-control" />
            <swap-validation for="Price" />
        </div>
        
        <div class="form-group">
            <label asp-for="Category"></label>
            <select asp-for="Category" asp-items="Model.Categories" class="form-control">
                <option value="">Select category...</option>
            </select>
            <swap-validation for="Category" />
        </div>
        
        <button type="submit" class="btn btn-primary">Create Product</button>
    </form>
</div>
```

### Controller

```csharp
[HttpPost]
public IActionResult Create(CreateProductDto dto)
{
    if (!ModelState.IsValid)
    {
        dto.Categories = _service.GetCategories();
        return this.SwapValidationErrors(ModelState)
            .WithView("_CreateForm", dto)
            .Build();
    }
    
    var product = _service.Create(dto);
    
    return this.SwapResponse()
        .WithView("_ProductRow", product)
        .WithCreatedToast("Product", product.Name)
        .WithTrigger(ProductEvents.Created)
        .Build();
}
```

### DTO with Validation Attributes

```csharp
public class CreateProductDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = "";
    
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Price must be between $0.01 and $999,999.99")]
    public decimal Price { get; set; }
    
    [Required(ErrorMessage = "Please select a category")]
    public string Category { get; set; } = "";
    
    // For the dropdown
    public SelectList? Categories { get; set; }
}
```

---

## Razor Pages Support

The validation extensions also work with Razor Pages:

```csharp
public class CreateModel : PageModel
{
    [BindProperty]
    public CreateProductDto Product { get; set; } = new();
    
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.SwapValidationErrors(ModelState)
                .WithView("_CreateForm", Product)
                .Build();
        }
        
        // ...
    }
}
```

---

## Field-Level Validation

For real-time validation as users type:

```html
<input asp-for="Email" 
       hx-post="/Validate/Email"
       hx-trigger="change, keyup changed delay:500ms"
       hx-target="#email-validation"
       hx-swap="innerHTML" />
<span id="email-validation"></span>
```

```csharp
[HttpPost]
public IActionResult Email([FromForm] string email)
{
    if (string.IsNullOrEmpty(email))
    {
        return Content("Email is required", "text/html");
    }
    
    if (!IsValidEmail(email))
    {
        return Content("<span class='text-danger'>Invalid email format</span>", "text/html");
    }
    
    if (_service.EmailExists(email))
    {
        return Content("<span class='text-danger'>Email already registered</span>", "text/html");
    }
    
    return Content("<span class='text-success'>✓ Available</span>", "text/html");
}
```

---

## See Also

- [CRUD Toast Presets](CrudToasts.md) - Standard success messages for CRUD operations
- [State Management](StateManagement.md) - Managing form state across requests
- [Anti-Patterns](AntiPatterns.md) - Common validation mistakes to avoid

# CRUD Toast Presets

Swap.Htmx provides convenient methods for displaying standard success messages after CRUD operations. These ensure consistent messaging across your application.

---

## Quick Start

```csharp
// After creating an item
return this.SwapResponse()
    .WithView("_ProductRow", product)
    .WithCreatedToast("Product", product.Name)  // "Product 'Widget' created successfully"
    .Build();

// After updating
return this.SwapResponse()
    .WithView("_ProductRow", product)
    .WithUpdatedToast("Product", product.Name)  // "Product 'Widget' updated"
    .Build();

// After deleting
return this.SwapResponse()
    .WithDeletedToast("Product")  // "Product deleted"
    .Build();
```

---

## Available Methods

### WithCrudToast (Generic)

```csharp
.WithCrudToast(CrudOperation operation, string entityName, string? itemName = null)
```

| Operation | Without Item Name | With Item Name |
|-----------|------------------|----------------|
| `Created` | "Product created successfully" | "Product 'Widget' created successfully" |
| `Updated` | "Product updated" | "Product 'Widget' updated" |
| `Deleted` | "Product deleted" | "Product 'Widget' deleted" |
| `Saved` | "Product saved" | "Product 'Widget' saved" |
| `Archived` | "Product archived" | "Product 'Widget' archived" |
| `Restored` | "Product restored" | "Product 'Widget' restored" |
| `Duplicated` | "Product duplicated" | "Product 'Widget' duplicated" |

### Shorthand Methods

```csharp
// Created - for new items
.WithCreatedToast("Product")              // "Product created successfully"
.WithCreatedToast("Product", "Widget")    // "Product 'Widget' created successfully"

// Updated - for modifications
.WithUpdatedToast("Settings")             // "Settings updated"
.WithUpdatedToast("User", "john@test.com") // "User 'john@test.com' updated"

// Deleted - for removals
.WithDeletedToast("Item")                 // "Item deleted"
.WithDeletedToast("Record", "R-123")      // "Record 'R-123' deleted"

// Saved - for create-or-update scenarios
.WithSavedToast("Document")               // "Document saved"
.WithSavedToast("Draft", "My Notes")      // "Draft 'My Notes' saved"
```

---

## CrudOperation Enum

```csharp
public enum CrudOperation
{
    Created,    // New item created
    Updated,    // Existing item modified
    Deleted,    // Item removed
    Saved,      // Create or update (ambiguous)
    Archived,   // Soft delete / archive
    Restored,   // Unarchive / restore
    Duplicated  // Copy created
}
```

---

## Usage Examples

### Basic CRUD Controller

```csharp
public class ProductsController : Controller
{
    private readonly IProductService _service;

    [HttpPost]
    public IActionResult Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return this.SwapValidationErrors(ModelState).WithView("_Form", dto).Build();
        
        var product = _service.Create(dto);
        
        return this.SwapResponse()
            .WithView("_ProductRow", product)
            .AlsoUpdate("product-count", "_ProductCount", _service.Count())
            .WithCreatedToast("Product", product.Name)
            .WithTrigger(ProductEvents.Created)
            .Build();
    }

    [HttpPost]
    public IActionResult Update(int id, UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
            return this.SwapValidationErrors(ModelState).WithView("_Form", dto).Build();
        
        var product = _service.Update(id, dto);
        
        return this.SwapResponse()
            .WithView("_ProductRow", product)
            .WithUpdatedToast("Product", product.Name)
            .Build();
    }

    [HttpDelete]
    public IActionResult Delete(int id)
    {
        var product = _service.GetById(id);
        _service.Delete(id);
        
        return this.SwapResponse()
            .AlsoUpdate($"product-{id}", "_Empty", null, SwapMode.Delete)
            .AlsoUpdate("product-count", "_ProductCount", _service.Count())
            .WithDeletedToast("Product", product.Name)
            .WithTrigger(ProductEvents.Deleted)
            .Build();
    }
}
```

### Archive/Restore Pattern

```csharp
[HttpPost]
public IActionResult Archive(int id)
{
    var record = _service.Archive(id);
    
    return this.SwapResponse()
        .WithView("_RecordRow", record)  // Shows "Archived" badge
        .WithCrudToast(CrudOperation.Archived, "Record", record.Name)
        .Build();
}

[HttpPost]
public IActionResult Restore(int id)
{
    var record = _service.Restore(id);
    
    return this.SwapResponse()
        .WithView("_RecordRow", record)
        .WithCrudToast(CrudOperation.Restored, "Record", record.Name)
        .Build();
}
```

### Duplicate Pattern

```csharp
[HttpPost]
public IActionResult Duplicate(int id)
{
    var original = _service.GetById(id);
    var copy = _service.Duplicate(id);
    
    return this.SwapResponse()
        .AlsoUpdate("item-list", "_ItemRow", copy, SwapMode.AfterBegin)
        .WithCrudToast(CrudOperation.Duplicated, "Template", original.Name)
        .Build();
}
```

---

## SwapRedirect with Toast

For operations that redirect after completion:

```csharp
[HttpPost]
public IActionResult Create(CreateDto dto)
{
    _service.Create(dto);
    
    // Redirect with success message
    return this.SwapRedirect("/Items", "Item created successfully!");
}

// Or use CRUD toast + redirect
[HttpPost]
public IActionResult Delete(int id)
{
    var item = _service.Delete(id);
    
    return this.SwapResponse()
        .WithRedirect("/Items")
        .WithDeletedToast("Item", item.Name)
        .Build();
}
```

---

## Custom Messages

For non-standard messages, use the base toast methods:

```csharp
// Custom success message
.WithSuccessToast("Your changes have been published!")

// Other toast types
.WithInfoToast("Processing may take a few minutes...")
.WithWarningToast("This action cannot be undone")
.WithErrorToast("Failed to connect to server")
```

---

## See Also

- [Form Validation](Validation.md) - Handling validation errors
- [Out-of-Band Swaps](OutOfBandSwaps.md) - Updating multiple elements
- [SwapRedirect](../Extensions/SwapControllerExtensions.cs) - Redirect helpers

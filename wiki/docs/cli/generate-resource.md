---
sidebar_position: 5
---

# swap generate resource

Generate a complete resource (model + controller + views) in a single command. This is the fastest way to scaffold a full CRUD feature.

## Synopsis

```bash
swap generate resource <name> [options]
swap g r <name> [options]  # Short alias
```

## Description

The `generate resource` command combines model and controller generation into one streamlined operation:

1. **Generates a model** with custom fields (or default fields)
2. **Generates a controller** with complete CRUD operations
3. **Generates views** for Index, Create, Edit, Delete, Details
4. **Updates DbContext** to register the entity

This is equivalent to running:
```bash
swap g m Product --fields Name:string,Price:decimal
swap g c Product
```

## Arguments

### `<name>`

**Required.** The name of the entity/resource to generate.

- Must be PascalCase (e.g., `Product`, `BlogPost`)
- Cannot contain spaces
- Used for model class, controller, views, and routes

**Examples:**
```bash
swap g r Product
swap g r Customer
swap g r BlogPost
swap g r OrderItem
```

## Options

### `--fields <specification>`

Define custom fields for the model.

**Format:**
```
FieldName:Type[,FieldName:Type...]
```

**Examples:**
```bash
swap g r Product --fields Name:string,Price:decimal,Stock:int
swap g r User --fields Email:string,Age:int,IsActive:bool,Bio:string?
swap g r Order --fields CustomerId:int,Total:decimal,OrderDate:datetime
```

**Default:** If omitted, generates a model with `Id`, `Title`, and `IsComplete` fields.

See [swap generate model](./generate-model) for complete field type documentation.

## Examples

### Simple Resource

Create a basic resource with default fields:

```bash
swap g r Task
```

**Generated:**
- `Models/Task.cs` - Model with Id, Title, IsComplete
- `Controllers/TaskController.cs` - Full CRUD controller
- `Views/Task/` - All CRUD views
- Updated `Data/AppDbContext.cs`

### E-Commerce Product

```bash
swap g r Product --fields Name:string,Description:string?,Price:decimal,Stock:int,SKU:string
```

**Generated:**
```csharp title="Models/Product.cs"
namespace MyApp.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public required string SKU { get; set; }
}
```

Plus controller and views with all CRUD operations.

### Blog Post

```bash
swap g r BlogPost --fields Title:string,Content:string,AuthorName:string,PublishedAt:datetime?,ViewCount:int
```

### Customer Management

```bash
swap g r Customer --fields FirstName:string,LastName:string,Email:string,Phone:string?,Address:string?,City:string,Country:string
```

### Order Processing

```bash
swap g r Order --fields CustomerId:int,OrderDate:datetime,ShipDate:datetime?,Total:decimal,Status:string,TrackingNumber:string?
```

## Workflow

### Complete Feature in 3 Commands

```bash
# 1. Generate the complete resource
swap g r Product --fields Name:string,Price:decimal,Stock:int

# 2. Create migration
dotnet ef migrations add AddProduct

# 3. Apply migration
dotnet ef database update

# Done! Navigate to http://localhost:5000/Product
```

### Multiple Related Resources

Build an entire feature set:

```bash
# Customer management
swap g r Customer --fields Name:string,Email:string,Phone:string?

# Product catalog
swap g r Product --fields Name:string,Price:decimal,Stock:int,CategoryId:int

# Order processing
swap g r Order --fields CustomerId:int,OrderDate:datetime,Total:decimal

# Order items (line items)
swap g r OrderItem --fields OrderId:int,ProductId:int,Quantity:int,UnitPrice:decimal

# Create and apply migrations
dotnet ef migrations add AddECommerce
dotnet ef database update
```

## Output

The command provides detailed feedback:

```
Generating complete resource: Product
Project: MyApp

Step 1/2: Generating model...
Generating entity model: Product
Project: MyApp
Fields: 3
  • Name: string (required)
  • Price: decimal
  • Stock: int

✓ Model generated successfully!

Step 2/2: Generating controller...
Generating CRUD controller: Product

✓ Controller generated successfully!

✓ Resource generation complete!

Summary:
  ✓ Model: Models/Product.cs
  ✓ Controller: Controllers/ProductController.cs
  ✓ Views: Views/Product/ (Index, Create, Edit, Delete, Details)
  ✓ DbContext: Updated with DbSet<Product>

Next steps:
  dotnet ef migrations add AddProduct
  dotnet ef database update
  # Navigate to /Product in your browser
```

## Generated File Structure

```
MyApp/
├── Models/
│   └── Product.cs                    # Entity model
├── Controllers/
│   └── ProductController.cs          # CRUD controller
├── Views/
│   └── Product/
│       ├── Index.cshtml              # List view
│       ├── Create.cshtml             # Create form
│       ├── Edit.cshtml               # Edit form
│       ├── Delete.cshtml             # Delete confirmation
│       ├── Details.cshtml            # Detail view
│       └── _ProductList.cshtml       # Partial list view
└── Data/
    └── AppDbContext.cs               # Updated with DbSet<Product>
```

## Advantages Over Separate Commands

### Speed

One command instead of two:

```bash
# Instead of:
swap g m Product --fields Name:string,Price:decimal
swap g c Product

# Just use:
swap g r Product --fields Name:string,Price:decimal
```

### Consistency

Ensures model and controller are generated with matching configurations.

### Simplified Workflow

Less typing, fewer steps, faster development.

## When to Use

### ✅ Use `swap generate resource` when:

- Starting a new feature from scratch
- Need both model and controller
- Want the fastest scaffold experience
- Building CRUD features

### ⚠️ Use individual commands when:

- Adding a controller to an existing model
- Creating a model that doesn't need a controller (DTOs, value objects)
- Need fine-grained control over each step

## Customization

After generation, customize the code:

### Add Validation

```csharp title="Models/Product.cs"
using System.ComponentModel.DataAnnotations;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    [Range(0.01, 10000)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }
    
    [Range(0, 10000)]
    public int Stock { get; set; }
}
```

### Add Business Logic

```csharp title="Controllers/ProductController.cs"
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Product product)
{
    if (ModelState.IsValid)
    {
        // Custom logic
        if (await _context.Products.AnyAsync(p => p.Name == product.Name))
        {
            ModelState.AddModelError("Name", "Product name already exists");
            return View(product);
        }
        
        product.CreatedAt = DateTime.UtcNow;
        
        _context.Add(product);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    return View(product);
}
```

### Enhance Views

```cshtml title="Views/Product/Index.cshtml"
@model IEnumerable<MyApp.Models.Product>

@{
    ViewData["Title"] = "Products";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h1>Products</h1>
    <div>
        <input type="text" class="form-control" placeholder="Search products..." id="searchBox" />
        <a asp-action="Create" class="btn btn-primary">Add New Product</a>
    </div>
</div>

@* Rest of the view *@
```

## Troubleshooting

### Duplicate DbSet Error

If you see:
```
error CS0102: The type 'AppDbContext' already contains a definition for 'Products'
```

This means the resource was generated twice. Delete one of the duplicate DbSet lines in `AppDbContext.cs`.

### Files Already Exist

The command warns but doesn't overwrite existing files:

```
Warning: Models/Product.cs already exists
```

Delete the existing file first or use a different name.

## Best Practices

### 1. Plan Your Fields

Think through all fields before generating:

```bash
# Good: Complete field list
swap g r Product --fields Name:string,Description:string?,Price:decimal,Stock:int,SKU:string,CategoryId:int

# Avoid: Multiple regenerations with different fields
```

### 2. Use Meaningful Names

```bash
# Good
swap g r CustomerOrder
swap g r BlogPost
swap g r PaymentTransaction

# Avoid
swap g r Item
swap g r Data
```

### 3. Generate Related Resources Together

Build related resources in sequence:

```bash
swap g r Category --fields Name:string,Description:string?
swap g r Product --fields Name:string,CategoryId:int,Price:decimal
swap g r Review --fields ProductId:int,Rating:int,Comment:string?
```

### 4. Commit Between Generations

```bash
git add .
git commit -m "Add Product resource"
swap g r Category --fields Name:string
```

## Next Steps

- [Model Documentation](./generate-model) - Advanced model features
- [Controller Documentation](./generate-controller) - Controller customization
- [Database Migrations](../database/migrations) - Working with EF Core migrations
- [Validation](../database/validation) - Add data validation

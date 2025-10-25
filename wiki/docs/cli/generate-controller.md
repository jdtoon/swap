---
sidebar_position: 4
---

# swap generate controller

Generate complete CRUD controllers with views, validation, and Entity Framework Core integration.

## Synopsis

```bash
swap generate controller <name> [options]
swap g c <name> [options]  # Short alias
```

## Description

The `generate controller` command creates a full-featured MVC controller with:

- Complete CRUD operations (Create, Read, Update, Delete)
- Async/await patterns for database operations
- Model validation and error handling
- Razor views with Bootstrap 5 styling
- CSRF protection
- Accessible HTML markup
- Entity Framework Core DbContext integration
- Automatic DbContext registration (if entity doesn't exist)

## Arguments

### `<name>`

**Required.** The name of the entity for which to generate a controller.

- Must match an existing model class name (or will use default Todo model)
- Will be used for controller and view names
- Must be PascalCase (e.g., `Product`, `CustomerOrder`)

**Examples:**
```bash
swap g c Product
swap g c Customer
swap g c BlogPost
swap g c TodoItem
```

## Generated Files

### Controller

**Location:** `Controllers/{Name}Controller.cs`

**Contains:**
- `Index()` - List all entities
- `Details(id)` - View single entity
- `Create()` - GET show create form
- `Create(entity)` - POST save new entity
- `Edit(id)` - GET show edit form
- `Edit(id, entity)` - POST update entity
- `Delete(id)` - GET show delete confirmation
- `DeleteConfirmed(id)` - POST delete entity

### Views

**Location:** `Views/{Name}/`

- `Index.cshtml` - Table view of all entities
- `Details.cshtml` - Single entity display
- `Create.cshtml` - Creation form
- `Edit.cshtml` - Edit form
- `Delete.cshtml` - Delete confirmation

## Generated Controller Code

### Full CRUD Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers;

public class ProductController : Controller
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Product
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.ToListAsync();
        return View(products);
    }

    // GET: Product/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // GET: Product/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Product/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // GET: Product/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    // POST: Product/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // GET: Product/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // POST: Product/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}
```

## Generated Views

### Index.cshtml

Lists all entities in a table:

```cshtml
@model IEnumerable<MyApp.Models.Product>

@{
    ViewData["Title"] = "Products";
}

<h1>Products</h1>

<p>
    <a asp-action="Create" class="btn btn-primary">Create New</a>
</p>

<table class="table table-striped">
    <thead>
        <tr>
            <th>@Html.DisplayNameFor(model => model.Name)</th>
            <th>@Html.DisplayNameFor(model => model.Price)</th>
            <th>@Html.DisplayNameFor(model => model.Stock)</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
@foreach (var item in Model) {
        <tr>
            <td>@Html.DisplayFor(modelItem => item.Name)</td>
            <td>@Html.DisplayFor(modelItem => item.Price)</td>
            <td>@Html.DisplayFor(modelItem => item.Stock)</td>
            <td>
                <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-primary">Edit</a>
                <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-info">Details</a>
                <a asp-action="Delete" asp-route-id="@item.Id" class="btn btn-sm btn-danger">Delete</a>
            </td>
        </tr>
}
    </tbody>
</table>
```

### Create.cshtml

Form for creating new entities:

```cshtml
@model MyApp.Models.Product

@{
    ViewData["Title"] = "Create Product";
}

<h1>Create Product</h1>

<hr />
<div class="row">
    <div class="col-md-6">
        <form asp-action="Create">
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            
            <div class="mb-3">
                <label asp-for="Name" class="form-label"></label>
                <input asp-for="Name" class="form-control" />
                <span asp-validation-for="Name" class="text-danger"></span>
            </div>
            
            <div class="mb-3">
                <label asp-for="Price" class="form-label"></label>
                <input asp-for="Price" class="form-control" />
                <span asp-validation-for="Price" class="text-danger"></span>
            </div>
            
            <div class="mb-3">
                <label asp-for="Stock" class="form-label"></label>
                <input asp-for="Stock" class="form-control" />
                <span asp-validation-for="Stock" class="text-danger"></span>
            </div>
            
            <div class="mb-3">
                <button type="submit" class="btn btn-primary">Create</button>
                <a asp-action="Index" class="btn btn-secondary">Back to List</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

### Edit.cshtml

Form for updating entities (similar structure to Create).

### Details.cshtml

Display entity properties in a formatted view.

### Delete.cshtml

Confirmation page before deletion.

## Examples

### Basic Controller Generation

Generate controller for existing model:

```bash
# First create the model
swap g m Product --fields Name:string,Price:decimal,Stock:int

# Then generate controller
swap g c Product
```

### E-Commerce CRUD

```bash
# Products
swap g m Product --fields Name:string,Description:string?,Price:decimal,Stock:int,SKU:string
swap g c Product

# Customers
swap g m Customer --fields Name:string,Email:string,Phone:string?,Address:string?
swap g c Customer

# Orders
swap g m Order --fields CustomerId:int,OrderDate:datetime,Total:decimal,Status:string
swap g c Order
```

### Blog System

```bash
# Posts
swap g m Post --fields Title:string,Content:string,AuthorId:int,PublishedAt:datetime?
swap g c Post

# Comments
swap g m Comment --fields PostId:int,AuthorName:string,Content:string,CreatedAt:datetime
swap g c Comment

# Categories
swap g m Category --fields Name:string,Description:string?
swap g c Category
```

## Workflow

### Complete CRUD Setup

```bash
# 1. Generate model
swap g m Product --fields Name:string,Price:decimal,Stock:int

# 2. Generate controller
swap g c Product

# 3. Create migration
dotnet ef migrations add AddProduct

# 4. Apply migration
dotnet ef database update

# 5. Run application
dotnet run

# 6. Navigate to http://localhost:5000/Product
```

## Customizing Generated Controllers

### Add Authorization

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize]  // Require authentication for all actions
public class ProductController : Controller
{
    // ... existing code

    [AllowAnonymous]  // Allow anonymous access to Index
    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.ToListAsync();
        return View(products);
    }

    [Authorize(Roles = "Admin")]  // Only admins can delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // ... delete logic
    }
}
```

### Add Search and Filtering

```csharp
public async Task<IActionResult> Index(string searchString, string sortOrder)
{
    ViewData["CurrentFilter"] = searchString;
    ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
    ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";

    var products = from p in _context.Products
                   select p;

    if (!String.IsNullOrEmpty(searchString))
    {
        products = products.Where(p => p.Name.Contains(searchString));
    }

    switch (sortOrder)
    {
        case "name_desc":
            products = products.OrderByDescending(p => p.Name);
            break;
        case "Price":
            products = products.OrderBy(p => p.Price);
            break;
        case "price_desc":
            products = products.OrderByDescending(p => p.Price);
            break;
        default:
            products = products.OrderBy(p => p.Name);
            break;
    }

    return View(await products.ToListAsync());
}
```

### Add Pagination

```csharp
using X.PagedList;

public async Task<IActionResult> Index(int? page)
{
    int pageSize = 10;
    int pageNumber = (page ?? 1);
    
    var products = await _context.Products
        .OrderBy(p => p.Name)
        .ToPagedListAsync(pageNumber, pageSize);
        
    return View(products);
}
```

### Add Logging

```csharp
public class ProductController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductController> _logger;

    public ProductController(AppDbContext context, ILogger<ProductController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Product created: {ProductId} - {ProductName}", 
                product.Id, product.Name);
                
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }
}
```

### Add Related Data

```csharp
// Include related entities
public async Task<IActionResult> Details(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var order = await _context.Orders
        .Include(o => o.Customer)        // Include customer
        .Include(o => o.OrderItems)      // Include order items
            .ThenInclude(oi => oi.Product)  // Include products
        .FirstOrDefaultAsync(m => m.Id == id);
        
    if (order == null)
    {
        return NotFound();
    }

    return View(order);
}
```

## Customizing Views

### Add Custom Styling

```cshtml
@* Index.cshtml *@
<div class="container my-5">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h1 class="display-4">Products</h1>
        <a asp-action="Create" class="btn btn-success btn-lg">
            <i class="bi bi-plus-circle"></i> Add Product
        </a>
    </div>

    <div class="card shadow-sm">
        <div class="card-body">
            <table class="table table-hover">
                @* ... table content *@
            </table>
        </div>
    </div>
</div>
```

### Add Client-Side Validation

The generated views include validation scripts:

```cshtml
@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

Customize validation:

```cshtml
<div class="mb-3">
    <label asp-for="Email" class="form-label"></label>
    <input asp-for="Email" class="form-control" type="email" />
    <span asp-validation-for="Email" class="text-danger"></span>
    <div class="form-text">We'll never share your email.</div>
</div>
```

### Add Confirmation Dialogs

```cshtml
@* Index.cshtml *@
<a asp-action="Delete" 
   asp-route-id="@item.Id" 
   class="btn btn-sm btn-danger"
   onclick="return confirm('Are you sure you want to delete this product?')">
    Delete
</a>
```

## Advanced Patterns

### API Controller

Modify for API endpoints:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Product
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products.ToListAsync();
    }

    // GET: api/Product/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return product;
    }

    // POST: api/Product
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // PUT: api/Product/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(int id, Product product)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Product/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}
```

## Troubleshooting

### "No .csproj file found"

Run from project root:

```bash
cd MyApp
swap g c Product
```

### "Model not found"

The controller is generated even if the model doesn't exist (uses Todo model as template). Generate the model first for best results:

```bash
swap g m Product --fields Name:string,Price:decimal
swap g c Product
```

### Views Not Rendering

Ensure your `_Layout.cshtml` exists and includes:

```cshtml
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
</head>
<body>
    @RenderBody()
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

## Best Practices

### 1. Generate Model First

```bash
# Right order
swap g m Product --fields Name:string,Price:decimal
swap g c Product

# Wrong order (works but less ideal)
swap g c Product  # Uses generic Todo template
```

### 2. Commit Before Generating

```bash
git add .
git commit -m "Before generating Product controller"
swap g c Product
```

### 3. Review and Customize

Generated code is a starting point. Add:
- Authorization
- Business logic validation
- Logging
- Error handling
- Related data loading

### 4. Use Routing Conventions

The generated controllers follow ASP.NET Core conventions:

- `/Product` → Index (list)
- `/Product/Details/5` → Details for ID 5
- `/Product/Create` → Create form
- `/Product/Edit/5` → Edit form for ID 5

## Next Steps

- [Model Generation](./generate-model) - Create models with custom fields
- [Resource Generation](./generate-resource) - Generate model + controller together
- [Authorization](../security/authorization) - Add role-based access control
- [Validation](../database/validation) - Add data validation

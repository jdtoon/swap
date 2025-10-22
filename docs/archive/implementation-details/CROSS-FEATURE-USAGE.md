# Using NetMX Modules Across Your Application

**How to integrate and use NetMX modules in your application**

**Last Updated**: October 21, 2025

---

## 🎯 Overview

NetMX modules are designed to be **plug-and-play**. Once installed, they provide:

1. **Services** - Injectable via DI
2. **Attributes** - For controllers/actions
3. **View Components** - For UI
4. **Extension Methods** - For convenience
5. **Events** - For cross-feature communication

---

## 📦 Module Installation

### Step 1: Add Module to Project

```bash
netmx add module Authorization

# Or manually add project references
dotnet add reference ../modules/Authorization/Authorization.Application/Authorization.Application.csproj
dotnet add reference ../modules/Authorization/Authorization.Web/Authorization.Web.csproj
```

### Step 2: Register Module Services

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Authorization module
builder.Services.AddAuthorizationModule(builder.Configuration);

var app = builder.Build();

// Use Authorization module
app.UseAuthorizationModule();

app.Run();
```

---

## 🔐 Example: Using Authorization Module

### 1. Controller Protection (Declarative)

```csharp
using Authorization.Web.Attributes;

[RequirePermission("Users.View")]
public class UsersController : Controller
{
    // All actions require "Users.View" permission
    
    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllAsync();
        return View(users);
    }
    
    [RequirePermission("Users.Create")]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        // Requires BOTH "Users.View" (from class) AND "Users.Create"
        await _userService.CreateAsync(dto);
        return RedirectToAction(nameof(Index));
    }
}
```

### 2. Multiple Permission Requirements

```csharp
using Authorization.Web.Attributes;

// Require ALL permissions (AND logic)
[RequireAllPermissions("Users.View", "Users.Edit", "Users.Delete")]
public class UserManagementController : Controller
{
    // User must have ALL three permissions
}

// Require ANY permission (OR logic)
[RequireAnyPermissions("Users.View", "Reports.View", "Dashboard.View")]
public class HomeController : Controller
{
    // User must have at least ONE of these permissions
}
```

### 3. Programmatic Permission Checks (Imperative)

```csharp
using Authorization.Contracts.Services;

public class ProductService : IProductService
{
    private readonly IPermissionChecker _permissionChecker;
    
    public ProductService(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }
    
    public async Task<List<ProductDto>> GetAllAsync()
    {
        // Check single permission
        if (!await _permissionChecker.IsGrantedAsync("Products.View"))
        {
            throw new UnauthorizedAccessException("Access denied");
        }
        
        // ... fetch products
    }
    
    public async Task DeleteAsync(Guid id)
    {
        // Check multiple permissions (all required)
        if (!await _permissionChecker.IsGrantedAllAsync("Products.Delete", "Products.Manage"))
        {
            throw new UnauthorizedAccessException("Insufficient permissions");
        }
        
        // ... delete product
    }
}
```

### 4. View-Level Permission Checks

```html
@using Authorization.Contracts.Services
@inject IPermissionChecker PermissionChecker

<h1>Product List</h1>

@if (await PermissionChecker.IsGrantedAsync("Products.Create"))
{
    <a href="/Products/Create" class="button is-primary">
        <i class="fas fa-plus"></i> New Product
    </a>
}

<table class="table">
    @foreach (var product in Model)
    {
        <tr>
            <td>@product.Name</td>
            <td>@product.Price</td>
            <td>
                @if (await PermissionChecker.IsGrantedAsync("Products.Edit"))
                {
                    <a href="/Products/Edit/@product.Id">Edit</a>
                }
                
                @if (await PermissionChecker.IsGrantedAsync("Products.Delete"))
                {
                    <button hx-delete="/Products/@product.Id" 
                            hx-confirm="Delete this product?">
                        Delete
                    </button>
                }
            </td>
        </tr>
    }
</table>
```

### 5. Conditional Business Logic

```csharp
public class OrderService : IOrderService
{
    private readonly IPermissionChecker _permissionChecker;
    
    public async Task<OrderDto> GetOrderAsync(Guid id)
    {
        var order = await _repository.GetAsync(id);
        
        // Show different data based on permissions
        var dto = new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Items = order.Items,
            
            // Only show customer details if user has permission
            CustomerName = await _permissionChecker.IsGrantedAsync("Orders.ViewCustomerData")
                ? order.CustomerName
                : "***REDACTED***",
            
            // Only show pricing if user has permission
            Total = await _permissionChecker.IsGrantedAsync("Orders.ViewPricing")
                ? order.Total
                : null
        };
        
        return dto;
    }
}
```

---

## 🎨 Example: Using CMS Module (Future)

### 1. Render Content in View

```html
@using CMS.Contracts.Services
@inject IContentService ContentService

<div class="content">
    @await ContentService.RenderAsync("home-page-banner")
</div>
```

### 2. Manage Content Programmatically

```csharp
public class HomeController : Controller
{
    private readonly IContentService _contentService;
    
    public async Task<IActionResult> Index()
    {
        var banner = await _contentService.GetBySlugAsync("home-page-banner");
        
        ViewBag.BannerHtml = banner.Content;
        
        return View();
    }
}
```

---

## 📧 Example: Using Email Module (Future)

### 1. Send Transactional Email

```csharp
using Email.Contracts.Services;

public class OrderService : IOrderService
{
    private readonly IEmailService _emailService;
    
    public async Task CreateOrderAsync(CreateOrderDto dto)
    {
        var order = // ... create order
        
        // Send confirmation email
        await _emailService.SendAsync(new EmailMessage
        {
            To = order.CustomerEmail,
            Subject = $"Order Confirmation #{order.OrderNumber}",
            Template = "OrderConfirmation",
            Data = new { Order = order }
        });
    }
}
```

### 2. Use Email Templates

```html
<!-- Templates/Email/OrderConfirmation.cshtml -->
<h1>Thank you for your order!</h1>

<p>Order #@Model.Order.OrderNumber has been confirmed.</p>

<ul>
    @foreach (var item in Model.Order.Items)
    {
        <li>@item.ProductName - $@item.Price</li>
    }
</ul>

<p><strong>Total: $@Model.Order.Total</strong></p>
```

---

## 🔔 Example: Using Notification Module (Future)

### 1. Send In-App Notification

```csharp
using Notifications.Contracts.Services;

public class CommentService : ICommentService
{
    private readonly INotificationService _notificationService;
    
    public async Task CreateCommentAsync(CreateCommentDto dto)
    {
        var comment = // ... create comment
        
        // Notify post author
        await _notificationService.NotifyAsync(new Notification
        {
            UserId = comment.Post.AuthorId,
            Type = NotificationType.Comment,
            Message = $"{comment.AuthorName} commented on your post",
            Link = $"/Posts/{comment.PostId}#comment-{comment.Id}"
        });
    }
}
```

---

## 🌐 Cross-Module Communication Patterns

### Pattern 1: Direct Service Injection

When **Module A** needs functionality from **Module B**:

```csharp
// In Products feature (your app)
public class ProductService : IProductService
{
    private readonly IPermissionChecker _permissionChecker; // From Authorization
    private readonly IEmailService _emailService; // From Email module
    
    public ProductService(
        IPermissionChecker permissionChecker,
        IEmailService emailService)
    {
        _permissionChecker = permissionChecker;
        _emailService = emailService;
    }
    
    public async Task CreateAsync(CreateProductDto dto)
    {
        // Check permission (Authorization module)
        if (!await _permissionChecker.IsGrantedAsync("Products.Create"))
            throw new UnauthorizedAccessException();
        
        var product = // ... create product
        
        // Send notification (Email module)
        await _emailService.SendAsync(new EmailMessage
        {
            To = "admin@company.com",
            Subject = "New Product Created",
            Body = $"Product {product.Name} was created"
        });
    }
}
```

### Pattern 2: Event-Driven Communication (HTMX)

For **UI updates** across features:

```csharp
// In ProductController
[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    var product = await _productService.CreateAsync(dto);
    
    // Trigger HTMX events for cross-feature updates
    this.HxTrigger("product-created", new { productId = product.Id });
    this.HxTrigger("inventory-changed");
    this.HxTrigger("stats-refresh");
    
    return Ok();
}
```

```html
<!-- In Dashboard feature -->
<div id="product-stats" 
     hx-get="/Dashboard/ProductStats" 
     hx-trigger="product-created from:body, inventory-changed from:body">
    <!-- Auto-refreshes when products change -->
</div>

<!-- In Inventory feature -->
<div id="inventory-summary" 
     hx-get="/Inventory/Summary" 
     hx-trigger="inventory-changed from:body">
    <!-- Auto-refreshes when inventory changes -->
</div>
```

### Pattern 3: Domain Events (Future - Advanced)

For **backend events** across modules:

```csharp
// Product module publishes event
public class Product : AggregateRoot<Guid>
{
    public void MarkAsOutOfStock()
    {
        IsInStock = false;
        
        // Raise domain event
        AddDomainEvent(new ProductOutOfStockEvent(Id, Name));
    }
}

// Inventory module subscribes to event
public class ProductOutOfStockEventHandler : IDomainEventHandler<ProductOutOfStockEvent>
{
    private readonly IEmailService _emailService;
    
    public async Task HandleAsync(ProductOutOfStockEvent @event)
    {
        // Send email to procurement team
        await _emailService.SendAsync(new EmailMessage
        {
            To = "procurement@company.com",
            Subject = $"Product Out of Stock: {@event.ProductName}",
            Body = "Please reorder this product"
        });
    }
}
```

---

## 🎯 Best Practices

### ✅ DO:

1. **Use dependency injection** for cross-module services
2. **Check permissions early** (controller level if possible)
3. **Use HTMX events** for UI cross-feature updates
4. **Document dependencies** in module.json
5. **Keep modules loosely coupled** (depend on contracts, not implementations)

### ❌ DON'T:

1. **Don't bypass module interfaces** (don't access repositories directly)
2. **Don't create circular dependencies** (Module A → Module B → Module A)
3. **Don't hardcode module paths** (use DI)
4. **Don't duplicate logic** (if Authorization has it, use it)
5. **Don't skip permission checks** (assume nothing)

---

## 📋 Module Usage Checklist

When adding a module to your app:

- [ ] Install module via CLI or project reference
- [ ] Register module services in Program.cs
- [ ] Run migrations (if module has database)
- [ ] Seed initial data (if module provides seeders)
- [ ] Read module README for specific usage
- [ ] Check module.json for dependencies
- [ ] Test module functionality
- [ ] Add module navigation to _Layout.cshtml (if needed)

---

## 🔍 Finding Available Modules

```bash
# List installed modules
netmx list modules

# Output:
# FREE Modules:
#   ✅ Identity v1.0.0 (installed)
#   ✅ Authorization v1.0.0 (installed)
#   📥 Audit v1.0.0 (available)
#   📥 Settings v1.0.0 (available)
#
# PAID Modules:
#   💰 MultiTenancy v1.0.0 - $299
#   💰 BackgroundJobs v1.0.0 - $149
#   💰 Email v1.0.0 - $149

# Search for modules
netmx search email

# Output:
# 💰 Email v1.0.0 - $149
#    Send emails with templates, queuing, and providers
#    Features: SMTP, SendGrid, AWS SES, templating
```

---

## 📚 Module-Specific Documentation

Each module has its own README with detailed usage:

- **Authorization**: `modules/Authorization/README.md` (405 lines)
- **Identity**: `modules/Identity/README.md`
- **Audit**: `modules/Audit/README.md`
- **Settings**: `modules/Settings/README.md`

**Always read the module README before using it!**

---

**Next**: See [EVENT-PIPELINES.md](EVENT-PIPELINES.md) for advanced event-driven patterns

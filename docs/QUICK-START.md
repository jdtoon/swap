# NetMX Quick Start Guide

**Get from zero to a working HTMX app in 5 minutes!**

No architecture knowledge required. Just follow the steps.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) or [Docker](https://www.docker.com/get-started)
- Code editor (VS Code, Visual Studio, Rider)

---

## Step 1: Install the CLI

```bash
# Install NetMX CLI globally
dotnet tool install --global NetMX.CLI

# Verify installation
netmx --help
```

**Expected Output**:
```
 _   _      _   __  ____  __
| \ | | ___| |_|  \/  \ \/ /
|  \| |/ _ \ __| |\/| |\  / 
| |\  |  __/ |_| |  | |/  \ 
|_| \_|\___|\__|_|  |_/_/\_\

ℹ️  The best CLI for .NET + HTMX developers
```

---

## Step 2: Create Your Project

```bash
# Option A: Use template (future)
netmx new modular MyAwesomeApp

# Option B: Clone template (current)
git clone https://github.com/netmx-framework/template-modular.git MyAwesomeApp
cd MyAwesomeApp
```

---

## Step 3: Start Database

```bash
# Option A: Using Docker (recommended)
docker-compose up -d db

# Option B: Using local PostgreSQL
# Create database: myapp_dev
# Update connection string in appsettings.json
```

---

## Step 4: Add Modules (Optional)

Modules provide reusable features. Add what you need:

```bash
cd src/MyAwesomeApp.Web

# Authentication & user management
netmx add module Identity

# Audit logging & compliance
netmx add module Audit

# Content management system
netmx add module CMS
```

**What happens**:
- ✅ Project references added
- ✅ Program.cs updated (commented code for review)
- ✅ Database migrations run
- ✅ Ready to use!

---

## Step 5: Generate Your First Feature

Create a Product catalog with CRUD operations:

```bash
# Generate complete CRUD with HTMX patterns
netmx generate feature Product

# Or with additional options
netmx generate feature Product --search --export
```

**What gets generated**:
- ✅ Product entity with validation
- ✅ 3 DTOs (read, create, update)
- ✅ Service interface + implementation
- ✅ Controller with HTMX helpers
- ✅ 3 views with HTMX patterns
- ✅ **All in 5 seconds!**

---

## Step 6: Add DbSet and Run Migration

```bash
# Open src/MyAwesomeApp.Web/Data/AppDbContext.cs
# Add: public DbSet<Product> Products => Set<Product>();

# Add migration
dotnet ef migrations add AddProduct --context AppDbContext

# Apply migration
dotnet ef database update --context AppDbContext
```

**Future**: `netmx db migrate AddProduct` will do this automatically!

---

## Step 7: Run Your App

```bash
dotnet run
```

Open browser: `http://localhost:5263`

Navigate to: `/Product`

**You should see**:
- Product list (empty at first)
- "New Product" button
- Click to add your first product
- HTMX in action (no page reloads!)

---

## Step 8: Explore the Generated Code

### Entity (`Models/Product.cs`)
```csharp
public class Product
{
    public int Id { get; set; }
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### Controller with HTMX (`Controllers/ProductController.cs`)
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    if (!ModelState.IsValid)
        return PartialView("_Form", dto);

    await _service.CreateAsync(dto);
    
    // Trigger HTMX event to refresh list
    this.HxTrigger("product-created");
    return Ok();
}

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    await _service.DeleteAsync(id);
    
    // Remove row from table (no page reload!)
    this.HxReswap(HtmxSwap.Delete);
    return Ok();
}
```

### View with HTMX (`Views/Product/_List.cshtml`)
```html
<!-- Edit button - loads form inline -->
<button hx-get="/Product/Edit/@item.Id" 
        hx-target="#form-container">
    Edit
</button>

<!-- Delete button - confirms and removes row -->
<button hx-delete="/Product/Delete/@item.Id" 
        hx-target="#row-@item.Id"
        hx-confirm="Are you sure?">
    Delete
</button>

<!-- List container - auto-refreshes on events -->
<div id="list-container" 
     hx-get="/Product/List" 
     hx-trigger="load, product-created from:body">
</div>
```

**Learn by reading!** The generated code shows you HTMX best practices.

---

## Next Steps

### Generate More Features

```bash
netmx generate feature Category
netmx generate feature Order
netmx generate feature Customer
```

### Customize Business Logic

1. Open generated service (`Services/ProductService.cs`)
2. Add custom methods:
```csharp
public async Task<List<ProductDto>> SearchAsync(string query)
{
    return await _context.Products
        .Where(p => p.Name.Contains(query))
        .Select(p => new ProductDto { /* ... */ })
        .ToListAsync();
}
```

3. Add controller action:
```csharp
[HttpGet]
public async Task<IActionResult> Search(string q)
{
    var results = await _service.SearchAsync(q);
    return PartialView("_List", results);
}
```

4. Add to view:
```html
<input type="search" 
       hx-get="/Product/Search" 
       hx-trigger="keyup changed delay:500ms"
       hx-target="#list-container" 
       name="q" />
```

### Learn HTMX Patterns

Navigate to `/Demo` in your app to see 8 interactive HTMX examples:
1. Click-to-edit (inline forms)
2. Delete with confirmation
3. Infinite scroll
4. Debounced search
5. Active tabs
6. Form validation
7. Out-of-band updates
8. Lazy loading

Read `docs/HTMX-PATTERNS.md` for complete guide.

### Build a Module (Advanced)

Want to create reusable features?

```bash
# 1. Create module structure
netmx create module Catalog

# 2. Generate features in module
cd modules/Catalog/Catalog.Web
netmx generate feature Product -m Catalog
netmx generate feature Category -m Catalog

# 3. Package and share
dotnet pack modules/Catalog/Catalog.Web
```

See [TERMINOLOGY.md](TERMINOLOGY.md) for module vs feature distinction.

---

## Common Commands Reference

### CLI Commands
```bash
# Create new project (future)
netmx new modular MyApp

# Add existing module
netmx add module Identity [--source <path>] [--skip-migration]

# Generate feature (CRUD)
netmx generate feature Product [-m Module] [--search] [--export]

# Database helpers (future)
netmx db migrate AddProduct
netmx db update
netmx db reset
netmx db seed

# List available items (future)
netmx list modules
netmx list components
```

### .NET CLI Commands
```bash
# Build project
dotnet build

# Run project
dotnet run

# Add migration
dotnet ef migrations add MigrationName --context AppDbContext

# Apply migrations
dotnet ef database update --context AppDbContext

# Remove last migration
dotnet ef migrations remove --context AppDbContext

# Install packages
dotnet add package PackageName
```

### Docker Commands
```bash
# Start database
docker-compose up -d db

# Stop database
docker-compose down

# Reset database (delete data)
docker-compose down -v && docker-compose up -d db

# View logs
docker-compose logs -f db
```

---

## Troubleshooting

### CLI Not Found

**Problem**: `netmx: command not found`

**Solution**:
```bash
# Install CLI
dotnet tool install --global NetMX.CLI

# If already installed, update PATH
# Restart terminal
```

### Database Connection Failed

**Problem**: `Connection refused` or `database does not exist`

**Solution**:
```bash
# Start Docker database
docker-compose up -d db

# Or verify PostgreSQL is running
# Check connection string in appsettings.json
```

### Migration Errors

**Problem**: `The specified DbContext could not be found`

**Solution**:
```bash
# Use full command with context
dotnet ef database update --context AppDbContext --project src/MyApp.Web
```

### Views Not Found

**Problem**: `The view 'Product/Index' was not found`

**Solution**:
```bash
# Verify files generated
ls Views/Product/

# Rebuild project
dotnet build

# If using Razor class library, check references
```

### HTMX Not Working

**Problem**: Clicks cause full page reload

**Solution**:
1. Check HTMX is loaded: View source, look for `htmx.min.js`
2. Check browser console for errors
3. Verify `hx-` attributes in view
4. Check controller returns partial view for HTMX requests

---

## Learn More

- **Terminology**: [TERMINOLOGY.md](TERMINOLOGY.md) - Module vs Feature vs Component
- **HTMX Patterns**: [HTMX-PATTERNS.md](HTMX-PATTERNS.md) - Complete pattern guide
- **CLI Guide**: [CLI-IMPLEMENTATION.md](CLI-IMPLEMENTATION.md) - CLI reference
- **Contributing**: [CONTRIBUTING.md](../CONTRIBUTING.md) - How to contribute
- **Examples**: `/Demo` page in your app - Live examples

---

## What's Next?

Now that you have a working app:

1. ✅ Generate features for your domain
2. ✅ Customize business logic
3. ✅ Learn HTMX patterns from `/Demo`
4. ✅ Add modules as needed (Identity, Audit, CMS)
5. ✅ Build your app with HTMX-first approach!

**Remember**: Don't create files manually - use the CLI!

---

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/netmx-framework/netmx/issues)
- **Discussions**: [GitHub Discussions](https://github.com/netmx-framework/netmx/discussions)
- **Documentation**: `docs/` folder
- **Examples**: Template project, `/Demo` page

---

**Welcome to NetMX - The best .NET + HTMX framework!** 🚀

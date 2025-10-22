# NetMX.EntityFrameworkCore

**Entity Framework Core integration for NetMX applications.**

This package provides EF Core implementations of repositories, DbContext base classes, and automatic handling of cross-cutting concerns like soft deletes and multi-tenancy.

## Overview

NetMX.EntityFrameworkCore gives you:
- **Repository Implementation**: Generic `EfCoreRepository<TEntity, TKey>`
- **DbContext Base Class**: Automatic query filters and conventions
- **Soft Delete Support**: Automatic filtering of deleted entities
- **Multi-Tenancy**: Automatic tenant isolation
- **Audit Logging**: Automatic timestamps and user tracking
- **Concurrency Control**: Optimistic locking support

Perfect for building data access layers without boilerplate.

## Installation

```bash
dotnet add package NetMX.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL  # or your provider
```

## Key Features

### 1. NetMXDbContext Base Class

Automatic conventions and global filters:

```csharp
using NetMX.EntityFrameworkCore;

public class AppDbContext : NetMXDbContext<AppDbContext>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entities
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });
    }
}
```

**Automatic Features**:
- Soft delete filtering (`ISoftDelete`)
- Multi-tenant filtering (`IMultiTenant`)
- Concurrency checking (`IHasConcurrencyStamp`)
- Audit timestamps (`IAudited`)

### 2. Generic Repository

No need to write CRUD code:

```csharp
using NetMX.EntityFrameworkCore.Repositories;

public class ProductService
{
    private readonly EfCoreRepository<Product, Guid> _repository;
    
    public ProductService(EfCoreRepository<Product, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<List<Product>> GetActiveAsync()
    {
        // Soft-deleted entities automatically excluded
        return await _repository.AsQueryable()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
```

**Available Methods**:
- `AsQueryable()` - Get IQueryable for queries
- `GetAsync(id)` - Get by ID
- `InsertAsync(entity)` - Add new entity
- `UpdateAsync(entity)` - Update existing
- `DeleteAsync(id)` - Delete (soft if ISoftDelete)

### 3. Automatic Soft Delete

Entities implementing `ISoftDelete` are never physically deleted:

```csharp
public class Product : Entity<Guid>, ISoftDelete
{
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
}

// Usage
await _repository.DeleteAsync(productId);
// Entity marked as deleted, not removed from database
// Automatically excluded from all queries
```

### 4. Multi-Tenancy Support

Automatic tenant isolation:

```csharp
public class Product : Entity<Guid>, IMultiTenant
{
    public string Name { get; set; }
    public Guid? TenantId { get; set; }
}

// Queries automatically filtered by current tenant
var products = await _repository.AsQueryable().ToListAsync();
// Only returns products for current tenant
```

### 5. Audit Logging

Automatic timestamps and user tracking:

```csharp
public class Product : Entity<Guid>, IAudited
{
    public string Name { get; set; }
    
    // Automatically set by framework
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

// Framework sets these automatically on SaveChanges
```

## Usage

### Setup in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        b => b.MigrationsAssembly("MyApp.Web")
    );
});

// Add repositories
builder.Services.AddScoped(typeof(IQueryableRepository<,>), typeof(EfCoreRepository<,>));

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.Run();
```

### Creating Migrations

```bash
# Add migration
dotnet ef migrations add AddProduct --context AppDbContext

# Apply migrations
dotnet ef database update --context AppDbContext

# Remove last migration
dotnet ef migrations remove --context AppDbContext
```

### Using Repository Pattern

```csharp
public class ProductAppService : ApplicationService
{
    private readonly IQueryableRepository<Product, Guid> _productRepo;
    private readonly IQueryableRepository<Category, Guid> _categoryRepo;
    
    public ProductAppService(
        IQueryableRepository<Product, Guid> productRepo,
        IQueryableRepository<Category, Guid> categoryRepo)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
    }
    
    public async Task<List<ProductDto>> GetByCategoryAsync(Guid categoryId)
    {
        return await _productRepo.AsQueryable()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.Category.Name
            })
            .ToListAsync();
    }
    
    [UnitOfWork]
    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.CategoryId
        );
        
        await _productRepo.InsertAsync(product);
        
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
}
```

### Custom DbContext Configuration

```csharp
public class AppDbContext : NetMXDbContext<AppDbContext>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Or configure individually
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(x => x.Id);
            
            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            b.HasIndex(x => x.Name);
            
            b.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

## API Reference

### NetMXDbContext<TContext>

**`DbSet<TEntity> Set<TEntity>()`** - Get DbSet  
**`SaveChangesAsync()`** - Save changes with automatic audit

**Override Points**:
- `OnModelCreating()` - Configure entities
- `OnConfiguring()` - Configure options

### EfCoreRepository<TEntity, TKey>

**`AsQueryable()`** - Get IQueryable  
**`GetAsync(id)`** - Get by ID  
**`InsertAsync(entity)`** - Add entity  
**`UpdateAsync(entity)`** - Update entity  
**`DeleteAsync(id)`** - Delete entity

### Cross-Cutting Interfaces

- **`ISoftDelete`** - Soft delete support
- **`IMultiTenant`** - Multi-tenancy isolation
- **`IAudited`** - Audit trail
- **`IHasConcurrencyStamp`** - Optimistic locking

## Dependencies

- `NetMX.Core` - Core utilities
- `NetMX.Ddd.Domain` - Domain abstractions
- `Microsoft.EntityFrameworkCore` - EF Core

## Related Packages

- **[NetMX.Ddd.Domain](../NetMX.Ddd.Domain/)** - Domain layer
- **[NetMX.Ddd.Application](../NetMX.Ddd.Application/)** - Application layer

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [Migrations Guide](../../docs/QUICK-START.md#database--migrations)
- [Quick Start Guide](../../docs/QUICK-START.md)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
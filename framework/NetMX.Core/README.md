# NetMX.Core

**The foundational package for all NetMX applications and modules.**

This package contains core abstractions, utilities, and conventions that every NetMX application depends on. It provides the building blocks for modularity, dependency injection, exception handling, and common patterns.

## Overview

NetMX.Core is the heart of the framework, providing:
- **Modular Architecture**: `NetMXModule` base class for pluggable modules
- **DI Conventions**: Marker interfaces for automatic service registration
- **Guard Clauses**: Defensive programming utilities
- **Common Interfaces**: Repository, unit of work, current user abstractions
- **Exception Handling**: Consistent error handling patterns

Every NetMX application starts here.

## Installation

```bash
dotnet add package NetMX.Core
```

## Key Features

### 1. NetMXModule - Modular Architecture

Create reusable, composable modules:

```csharp
public class MyCustomModule : NetMXModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register module services
        services.AddTransient<IMyService, MyService>();
    }
    
    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        // Initialize module resources
    }
}
```

**Benefits**:
- ✅ Encapsulated module logic
- ✅ Explicit dependencies between modules
- ✅ Clear initialization order
- ✅ Reusable across applications

### 2. Dependency Injection Conventions

Automatic service registration with marker interfaces:

```csharp
// Transient (new instance per request)
public class MyService : IMyService, ITransientDependency
{
    // Automatically registered as transient
}

// Scoped (one instance per HTTP request)
public class UserService : IUserService, IScopedDependency
{
    // Automatically registered as scoped
}

// Singleton (one instance for application lifetime)
public class CacheService : ICacheService, ISingletonDependency
{
    // Automatically registered as singleton
}
```

**Setup**:
```csharp
services.AddNetMXCore();  // Scans and registers all services
```

### 3. Guard Clauses

Defensive programming made easy:

```csharp
using NetMX;

public class Product
{
    public Product(string name, decimal price)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        Price = Guard.GreaterThan(price, 0, nameof(price));
    }
}
```

**Available Guards**:
- `Guard.NotNull(value, paramName)`
- `Guard.NotNullOrEmpty(value, paramName)`
- `Guard.NotNullOrWhiteSpace(value, paramName)`
- `Guard.GreaterThan(value, min, paramName)`
- `Guard.InRange(value, min, max, paramName)`

### 4. Repository Pattern

Generic repository interface for data access:

```csharp
public interface IQueryableRepository<TEntity, TKey>
{
    IQueryable<TEntity> AsQueryable();
    Task<TEntity?> GetAsync(TKey id);
    Task<TEntity> InsertAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TKey id);
}
```

**Usage**:
```csharp
public class ProductService
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public ProductService(IQueryableRepository<Product, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<List<Product>> GetActiveAsync()
    {
        return await _repository.AsQueryable()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
```

### 5. Current User Abstraction

Access current user information:

```csharp
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
```

**Usage**:
```csharp
public class OrderService
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            CreatedBy = _currentUser.UserId!.Value,
            CreatedAt = DateTime.UtcNow
        };
        
        // ...
    }
}
```

## Usage

### Basic Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add NetMX Core services
builder.Services.AddNetMXCore();

var app = builder.Build();

// Initialize modules
app.UseNetMX();

app.Run();
```

### Creating a Custom Module

```csharp
using NetMX;

[DependsOn(typeof(MyOtherModule))]  // Explicit dependencies
public class MyFeatureModule : NetMXModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.Configure<MyOptions>(options => 
        {
            options.Setting = "value";
        });
    }
    
    public override void OnApplicationInitialization(IApplicationBuilder app)
    {
        // Initialize resources (migrations, seed data, etc.)
    }
}
```

### Using DI Conventions

```csharp
// Automatically registered because of IScopedDependency
public class ProductAppService : IScopedDependency
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public ProductAppService(IQueryableRepository<Product, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await _repository.GetAsync(id);
        Guard.NotNull(product, nameof(product));
        
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name
        };
    }
}
```

## API Reference

### NetMXModule

**`ConfigureServices(IServiceCollection services)`**  
Register module services.

**`OnApplicationInitialization(IApplicationBuilder app)`**  
Initialize module resources.

### DI Marker Interfaces

- **`ITransientDependency`** - New instance per request
- **`IScopedDependency`** - One instance per HTTP request
- **`ISingletonDependency`** - One instance for application lifetime

### Guard Class

Static validation utilities for defensive programming.

### ICurrentUser

Abstraction for accessing current user information.

## Dependencies

- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Options`

## Related Packages

- **[NetMX.Ddd.Domain](../NetMX.Ddd.Domain/)** - Domain layer (entities, value objects)
- **[NetMX.Ddd.Application](../NetMX.Ddd.Application/)** - Application layer (services)
- **[NetMX.EntityFrameworkCore](../NetMX.EntityFrameworkCore/)** - EF Core integration

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [Quick Start Guide](../../docs/QUICK-START.md)
- [Terminology](../../docs/TERMINOLOGY.md)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
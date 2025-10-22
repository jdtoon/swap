# NetMX.Data

**Database-agnostic data access abstractions.**

This package provides core interfaces and utilities for data access that work across different database providers.

## Overview

NetMX.Data provides:
- **Data Filters**: Control global query filters
- **Connection String Resolver**: Multi-tenant connection strings
- **Database Provider Abstractions**: Provider-agnostic interfaces
- **Migration Support**: Cross-provider migration utilities

Perfect for flexible data access strategies.

## Installation

```bash
dotnet add package NetMX.Data
```

## Key Features

### 1. Data Filter Control

Enable/disable global filters:

```csharp
public class AdminService
{
    private readonly IDataFilter _dataFilter;
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public AdminService(
        IDataFilter dataFilter,
        IQueryableRepository<Product, Guid> repository)
    {
        _dataFilter = dataFilter;
        _repository = repository;
    }
    
    public async Task<List<Product>> GetAllIncludingDeletedAsync()
    {
        // Disable soft delete filter
        using (_dataFilter.Disable<ISoftDelete>())
        {
            return await _repository.AsQueryable().ToListAsync();
        }
        // Filter re-enabled after using block
    }
}
```

### 2. Connection String Resolver

Multi-database support:

```csharp
public interface IConnectionStringResolver
{
    Task<string> ResolveAsync(string connectionName = "Default");
}

// Usage
public class TenantConnectionStringResolver : IConnectionStringResolver
{
    private readonly ICurrentTenant _currentTenant;
    
    public async Task<string> ResolveAsync(string connectionName = "Default")
    {
        if (_currentTenant.Id.HasValue)
        {
            // Return tenant-specific connection string
            return await GetTenantConnectionStringAsync(_currentTenant.Id.Value);
        }
        
        return GetDefaultConnectionString();
    }
}
```

## Usage

### Disable Soft Delete Filter

```csharp
using (_dataFilter.Disable<ISoftDelete>())
{
    // Query includes soft-deleted entities
    var allProducts = await _repository.AsQueryable().ToListAsync();
}
```

### Disable Multi-Tenancy Filter

```csharp
using (_dataFilter.Disable<IMultiTenant>())
{
    // Query across all tenants
    var allOrders = await _repository.AsQueryable().ToListAsync();
}
```

## API Reference

### IDataFilter

**`Disable<TFilter>()`** - Disable global filter  
**`Enable<TFilter>()`** - Enable global filter  
**`IsEnabled<TFilter>()`** - Check if enabled

### IConnectionStringResolver

**`ResolveAsync(string name)`** - Resolve connection string

## Dependencies

- `NetMX.Core` - Core utilities

## Related Packages

- **[NetMX.EntityFrameworkCore](../NetMX.EntityFrameworkCore/)** - EF Core implementation
- **[NetMX.Ddd.Domain](../NetMX.Ddd.Domain/)** - Domain abstractions

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [Multi-Tenancy Guide](../../docs/COMPLETE-DEVELOPMENT-ROADMAP.md#week-11-12-multi-tenancy-module)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
# NetMX.Ddd.Application.Contracts

**Contracts and DTOs for application layer interfaces.**

This package defines the boundaries between UI/API and application layers through interfaces and Data Transfer Objects (DTOs).

## Overview

NetMX.Ddd.Application.Contracts provides:
- **Service Interfaces**: Contracts for application services
- **Standard DTOs**: EntityDto, PagedResultDto, etc.
- **Paging/Sorting**: Built-in pagination support
- **Validation**: Data annotations for input validation
- **Reusability**: Share contracts across layers

Perfect for clean architectural boundaries.

## Installation

```bash
dotnet add package NetMX.Ddd.Application.Contracts
```

## Key Features

### 1. IApplicationService Interface

Marker interface for convention-based registration:

```csharp
public interface IProductService : IApplicationService
{
    Task<ProductDto> GetAsync(Guid id);
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto> CreateAsync(CreateProductDto input);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input);
    Task DeleteAsync(Guid id);
}
```

### 2. Standard DTOs

```csharp
// Entity DTO with ID
public class ProductDto : EntityDto<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create input
public class CreateProductDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
}
```

### 3. Paging Support

```csharp
// Request DTO
public class GetProductsRequest : PagedResultRequestDto
{
    public string? SearchTerm { get; set; }
}

// Response DTO
public class PagedResultDto<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
}

// Usage
public async Task<PagedResultDto<ProductDto>> GetPagedAsync(GetProductsRequest input)
{
    var query = _repository.AsQueryable();
    
    if (!string.IsNullOrEmpty(input.SearchTerm))
        query = query.Where(p => p.Name.Contains(input.SearchTerm));
    
    var totalCount = await query.CountAsync();
    
    var products = await query
        .Skip(input.SkipCount)
        .Take(input.MaxResultCount)
        .ToListAsync();
    
    return new PagedResultDto<ProductDto>
    {
        Items = ObjectMapper.Map<List<Product>, List<ProductDto>>(products),
        TotalCount = totalCount
    };
}
```

## Usage

### Define Service Interface

```csharp
public interface IOrderService : IApplicationService
{
    Task<OrderDto> GetAsync(Guid id);
    Task<PagedResultDto<OrderDto>> GetPagedAsync(GetOrdersRequest input);
    Task<OrderDto> CreateAsync(CreateOrderDto input);
    Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto input);
    Task DeleteAsync(Guid id);
    Task<OrderDto> SubmitAsync(Guid id);
}
```

### Define DTOs

```csharp
// Read DTO
public class OrderDto : EntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

// Create DTO
public class CreateOrderDto
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    [MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; }
}

// Update DTO
public class UpdateOrderDto
{
    public List<CreateOrderItemDto>? Items { get; set; }
}
```

## API Reference

### IApplicationService

Marker interface for application services.

### EntityDto<TKey>

**`Id`** - Entity identifier

### PagedResultRequestDto

**`SkipCount`** - Number of items to skip  
**`MaxResultCount`** - Maximum items to return

### PagedResultDto<T>

**`Items`** - List of items  
**`TotalCount`** - Total available items

## Dependencies

- `NetMX.Core` - Core utilities
- `System.ComponentModel.Annotations` - Validation

## Related Packages

- **[NetMX.Ddd.Application](../NetMX.Ddd.Application/)** - Application implementation
- **[NetMX.Ddd.Domain](../NetMX.Ddd.Domain/)** - Domain layer

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [Quick Start Guide](../../docs/QUICK-START.md)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
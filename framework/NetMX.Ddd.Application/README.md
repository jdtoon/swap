# NetMX.Ddd.Application

**Application layer implementation for orchestrating business logic.**

This package provides base classes and patterns for implementing application services that coordinate domain objects, repositories, and use cases.

## Overview

NetMX.Ddd.Application gives you:
- **ApplicationService Base Class**: Inheritance for common functionality
- **Unit of Work Pattern**: Transaction management and consistency
- **DTO Mapping**: Transform domain objects to DTOs
- **Use Case Orchestration**: Coordinate domain operations
- **Cross-Cutting Concerns**: Validation, authorization, logging

Perfect for building maintainable application layers.

## Installation

```bash
dotnet add package NetMX.Ddd.Application
```

## Key Features

### 1. ApplicationService Base Class

Common functionality for all application services:

```csharp
using NetMX.Ddd.Application.Services;

public class ProductAppService : ApplicationService
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public ProductAppService(IQueryableRepository<Product, Guid> repository)
    {
        _repository = repository;
    }
    
    [UnitOfWork]
    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        // Validate input
        await ValidateAsync(input);
        
        // Create domain entity
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Price
        );
        
        // Save to repository
        product = await _repository.InsertAsync(product);
        
        // Map to DTO
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
}
```

**Inherited Features**:
- `ObjectMapper` - DTO mapping
- `GuidGenerator` - ID generation
- `CurrentUser` - Current user info
- `Logger` - Structured logging

### 2. Unit of Work

Automatic transaction management:

```csharp
public class OrderAppService : ApplicationService
{
    private readonly IQueryableRepository<Order, Guid> _orderRepo;
    private readonly IQueryableRepository<Product, Guid> _productRepo;
    
    [UnitOfWork]  // Ensures both operations succeed or rollback
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input)
    {
        // Create order
        var order = new Order(GuidGenerator.Create(), input.CustomerId);
        
        // Add items (multiple repository operations)
        foreach (var item in input.Items)
        {
            var product = await _productRepo.GetAsync(item.ProductId);
            order.AddItem(product, item.Quantity);
        }
        
        // Save order
        await _orderRepo.InsertAsync(order);
        
        return ObjectMapper.Map<Order, OrderDto>(order);
        
        // Transaction automatically committed
        // If any error occurs, everything rolls back
    }
}
```

**Benefits**:
- Automatic transaction boundaries
- Rollback on exceptions
- Configurable isolation levels
- Nested unit of work support

### 3. DTO Patterns

Clean separation of domain and API:

```csharp
// Read DTO (what clients see)
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create DTO (what clients send)
public class CreateProductDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
}

// Update DTO (what clients modify)
public class UpdateProductDto
{
    [MaxLength(200)]
    public string? Name { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }
}
```

### 4. Use Case Implementation

Orchestrate complex operations:

```csharp
public class CheckoutService : ApplicationService
{
    private readonly IQueryableRepository<Order, Guid> _orderRepo;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    
    [UnitOfWork]
    public async Task<CheckoutResultDto> CheckoutAsync(CheckoutDto input)
    {
        // 1. Get order
        var order = await _orderRepo.GetAsync(input.OrderId);
        
        // 2. Submit order (domain logic)
        order.Submit();
        await _orderRepo.UpdateAsync(order);
        
        // 3. Process payment (external service)
        var payment = await _paymentService.ChargeAsync(
            order.CustomerId,
            order.Total
        );
        
        // 4. Send confirmation email
        await _emailService.SendOrderConfirmationAsync(order);
        
        // 5. Return result
        return new CheckoutResultDto
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            Success = true
        };
    }
}
```

## Usage

### Basic CRUD Service

```csharp
using NetMX.Ddd.Application.Services;

public class ProductService : ApplicationService, IScopedDependency
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public ProductService(IQueryableRepository<Product, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<List<ProductDto>> GetAllAsync()
    {
        var products = await _repository.AsQueryable()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
            
        return ObjectMapper.Map<List<Product>, List<ProductDto>>(products);
    }
    
    public async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await _repository.GetAsync(id);
        Guard.NotNull(product, nameof(product));
        
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
    
    [UnitOfWork]
    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Price
        );
        
        product = await _repository.InsertAsync(product);
        
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
    
    [UnitOfWork]
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input)
    {
        var product = await _repository.GetAsync(id);
        Guard.NotNull(product, nameof(product));
        
        if (input.Name != null)
            product.UpdateName(input.Name);
            
        if (input.Price.HasValue)
            product.UpdatePrice(input.Price.Value);
        
        product = await _repository.UpdateAsync(product);
        
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
    
    [UnitOfWork]
    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
```

### Complex Use Case

```csharp
public class OrderManagementService : ApplicationService
{
    private readonly IQueryableRepository<Order, Guid> _orderRepo;
    private readonly IQueryableRepository<Product, Guid> _productRepo;
    private readonly IInventoryService _inventoryService;
    
    [UnitOfWork]
    public async Task<OrderDto> CreateAndReserveAsync(CreateOrderDto input)
    {
        // 1. Validate products exist and have stock
        foreach (var item in input.Items)
        {
            var product = await _productRepo.GetAsync(item.ProductId);
            Guard.NotNull(product, nameof(product));
            
            var available = await _inventoryService.CheckStockAsync(
                item.ProductId,
                item.Quantity
            );
            
            if (!available)
                throw new BusinessException($"Insufficient stock for {product.Name}");
        }
        
        // 2. Create order
        var order = new Order(GuidGenerator.Create(), CurrentUser.UserId!.Value);
        
        // 3. Add items
        foreach (var item in input.Items)
        {
            var product = await _productRepo.GetAsync(item.ProductId);
            order.AddItem(product, item.Quantity);
        }
        
        // 4. Reserve inventory
        foreach (var item in order.Items)
        {
            await _inventoryService.ReserveAsync(
                item.ProductId,
                item.Quantity
            );
        }
        
        // 5. Save order
        await _orderRepo.InsertAsync(order);
        
        // 6. Return DTO
        return ObjectMapper.Map<Order, OrderDto>(order);
    }
}
```

## API Reference

### ApplicationService

**`ObjectMapper`** - Maps domain objects to DTOs  
**`GuidGenerator`** - Generates unique IDs  
**`CurrentUser`** - Current user information  
**`Logger`** - Structured logging

### UnitOfWorkAttribute

**`[UnitOfWork]`** - Marks method as transactional  
**`[UnitOfWork(isTransactional: false)]`** - Read-only operation

### IUnitOfWork

**`BeginAsync()`** - Start transaction  
**`CompleteAsync()`** - Commit transaction  
**`RollbackAsync()`** - Rollback transaction

## Dependencies

- `NetMX.Core` - Core utilities
- `NetMX.Ddd.Domain` - Domain abstractions
- `NetMX.Ddd.Application.Contracts` - DTOs and interfaces

## Related Packages

- **[NetMX.Ddd.Domain](../NetMX.Ddd.Domain/)** - Domain layer
- **[NetMX.Ddd.Application.Contracts](../NetMX.Ddd.Application.Contracts/)** - Contracts
- **[NetMX.EntityFrameworkCore](../NetMX.EntityFrameworkCore/)** - Data access

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [DDD Patterns](../../docs/CROSS-FEATURE-USAGE.md)
- [Quick Start Guide](../../docs/QUICK-START.md)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
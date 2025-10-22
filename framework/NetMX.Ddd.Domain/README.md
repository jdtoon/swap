# NetMX.Ddd.Domain

**Domain-Driven Design building blocks for your domain layer.**

This package provides base classes, interfaces, and patterns for implementing a clean, maintainable domain layer using Domain-Driven Design (DDD) principles.

## Overview

NetMX.Ddd.Domain gives you:
- **Base Classes**: `Entity<TKey>` and `AggregateRoot<TKey>` for domain objects
- **Value Objects**: Immutable objects with value semantics
- **Repository Pattern**: Standard abstractions for data access
- **Cross-Cutting Concerns**: Soft delete, multi-tenancy, concurrency
- **Domain Events**: Decouple domain logic

Perfect for building rich, expressive domain models.

## Installation

```bash
dotnet add package NetMX.Ddd.Domain
```

## Key Features

### 1. Entity Base Class

Foundation for all domain entities:

```csharp
using NetMX.Ddd.Domain.Entities;

public class Product : Entity<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    
    // Private constructor for EF Core
    private Product() { }
    
    public Product(Guid id, string name, decimal price) : base(id)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        Price = Guard.GreaterThan(price, 0, nameof(price));
    }
    
    public void UpdatePrice(decimal newPrice)
    {
        Price = Guard.GreaterThan(newPrice, 0, nameof(newPrice));
    }
}
```

**Features**:
- Identity management (`Id` property)
- Equality by ID
- Encapsulated constructors
- Defensive programming

### 2. Aggregate Root

For aggregates that manage clusters of related entities:

```csharp
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    
    public Guid CustomerId { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.Subtotal);
    
    private Order() { }
    
    public Order(Guid id, Guid customerId) : base(id)
    {
        CustomerId = customerId;
    }
    
    public void AddItem(Guid productId, int quantity, decimal price)
    {
        var item = new OrderItem(productId, quantity, price);
        _items.Add(item);
    }
    
    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
            _items.Remove(item);
    }
}
```

**Benefits**:
- Enforces invariants
- Encapsulates related entities
- Single source of truth

### 3. Value Objects

Immutable objects with value semantics:

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = Guard.NotNullOrEmpty(currency, nameof(currency));
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
            
        return new Money(Amount + other.Amount, Currency);
    }
}
```

**Usage**:
```csharp
public class Product : Entity<Guid>
{
    public Money Price { get; private set; }
    
    public Product(Guid id, Money price) : base(id)
    {
        Price = Guard.NotNull(price, nameof(price));
    }
}
```

### 4. Repository Interface

Standard contract for data access:

```csharp
public interface IRepository<TEntity, TKey> where TEntity : Entity<TKey>
{
    Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default);
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TKey id, CancellationToken ct = default);
}

public interface IQueryableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
{
    IQueryable<TEntity> AsQueryable();
}
```

**Usage**:
```csharp
public class ProductService
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public async Task<Product?> GetByNameAsync(string name)
    {
        return await _repository.AsQueryable()
            .FirstOrDefaultAsync(p => p.Name == name);
    }
}
```

### 5. Cross-Cutting Concern Interfaces

Automatically handled by the framework:

```csharp
// Soft Delete (never physically deleted)
public class Product : Entity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
}

// Multi-Tenancy (isolated by tenant)
public class Product : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
}

// Optimistic Concurrency (prevents lost updates)
public class Product : Entity<Guid>, IHasConcurrencyStamp
{
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
}

// Audit Trail (who/when created/modified)
public class Product : Entity<Guid>, IAudited
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

**Framework handles automatically**:
- Soft delete: Filters deleted entities from queries
- Multi-tenancy: Isolates data by tenant
- Concurrency: Detects concurrent updates
- Audit: Sets timestamps and user IDs

## Usage

### Creating a Rich Domain Model

```csharp
using NetMX.Ddd.Domain.Entities;
using NetMX.Ddd.Domain.Values;

public class Order : AggregateRoot<Guid>, IAudited, ISoftDelete
{
    private readonly List<OrderItem> _items = new();
    
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    // IAudited
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // ISoftDelete
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
    
    private Order() { }
    
    public Order(Guid id, Guid customerId) : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        Total = new Money(0, "USD");
    }
    
    public void AddItem(Product product, int quantity)
    {
        Guard.GreaterThan(quantity, 0, nameof(quantity));
        
        var item = new OrderItem(product.Id, quantity, product.Price);
        _items.Add(item);
        
        RecalculateTotal();
    }
    
    public void Submit()
    {
        if (!_items.Any())
            throw new InvalidOperationException("Cannot submit empty order");
            
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order already submitted");
            
        Status = OrderStatus.Submitted;
    }
    
    private void RecalculateTotal()
    {
        var amount = _items.Sum(i => i.Subtotal.Amount);
        Total = new Money(amount, "USD");
    }
}
```

## API Reference

### Entity<TKey>

**`Id`** - Entity identifier  
**`Equals(object obj)`** - Equality by ID  
**`GetHashCode()`** - Hash based on ID

### AggregateRoot<TKey>

Inherits from `Entity<TKey>`, used for aggregate roots.

### ValueObject

**`GetAtomicValues()`** - Override to define value properties  
**`Equals(object obj)`** - Equality by value  
**`GetHashCode()`** - Hash based on values

### Interfaces

- **`ISoftDelete`** - Soft delete support
- **`IMultiTenant`** - Multi-tenancy isolation
- **`IHasConcurrencyStamp`** - Optimistic concurrency
- **`IAudited`** - Audit trail

## Dependencies

- `NetMX.Core` - Core utilities

## Related Packages

- **[NetMX.Ddd.Application](../NetMX.Ddd.Application/)** - Application services
- **[NetMX.EntityFrameworkCore](../NetMX.EntityFrameworkCore/)** - EF Core integration
- **[NetMX.Ddd.Application.Contracts](../NetMX.Ddd.Application.Contracts/)** - Contracts/DTOs

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [DDD Patterns](../../docs/CROSS-FEATURE-USAGE.md)
- [Quick Start Guide](../../docs/QUICK-START.md)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
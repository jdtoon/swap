# NetMX.Ddd.Domain

This package provides the core building blocks for implementing a Domain-Driven Design (DDD) domain layer.

## Key Features

-   **Base Classes:** `Entity<TKey>` and `AggregateRoot<TKey>` provide a common foundation for domain objects.
-   **Repository Abstractions:** The `IRepository<TEntity, TKey>` interface defines the standard contract for data access.
-   **Cross-Cutting Concern Interfaces:** Includes marker interfaces like `ISoftDelete`, `IMultiTenant`, and `IHasConcurrencyStamp` which the framework uses to automatically apply global logic.
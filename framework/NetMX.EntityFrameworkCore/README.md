# NetMX.EntityFrameworkCore

This package provides the concrete implementation of the data access layer using Microsoft Entity Framework Core.

## Key Features

-   **`EfCoreRepository<...>`:** A generic repository implementation that provides standard CRUD operations out-of-the-box, saving developers from writing repetitive data access code.
-   **`NetMXDbContext<T>`:** A base `DbContext` class that automatically applies global query filters for interfaces like `ISoftDelete` and `IMultiTenant`, enforcing architectural consistency by convention.
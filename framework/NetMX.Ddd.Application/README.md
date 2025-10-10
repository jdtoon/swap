# NetMX.Ddd.Application

This package provides the core implementation and orchestration logic for the Application Layer.

## Key Features

-   **`ApplicationService`:** A base class for application services that developers can inherit from.
-   **Unit of Work (UoW) Abstractions:** Defines the `IUnitOfWork`, `IUnitOfWorkManager`, and `UnitOfWorkAttribute` to manage transactions declaratively and ensure data consistency.
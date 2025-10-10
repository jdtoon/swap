# NetMX.Ddd.Application.Contracts

This package contains the contracts (interfaces and DTOs) for the Application Layer in a DDD-based architecture. It acts as a shared boundary between the UI/API layer and the application implementation.

## Key Features

-   **`IApplicationService`:** A marker interface for all application services, enabling convention-based registration.
-   **Standard DTOs:** Provides common Data Transfer Objects like `EntityDto<TKey>`, `PagedResultRequestDto`, and `PagedResultDto<T>` to reduce boilerplate code.
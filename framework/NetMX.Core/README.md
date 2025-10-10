# NetMX.Core

This is the foundational package for the entire NetMX Framework. It contains the most essential, low-level building blocks and conventions that all other NetMX packages and applications depend on.

## Key Features

-   **`NetMXModule`:** The abstract base class for the modularity system. Every module in the ecosystem inherits from this class to participate in the application startup and service configuration lifecycle.

-   **Dependency Injection Conventions:** Defines the marker interfaces (`ITransientDependency`, `IScopedDependency`, `ISingletonDependency`) that enable automatic, convention-over-configuration service registration.
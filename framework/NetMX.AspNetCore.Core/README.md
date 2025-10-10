# NetMX.AspNetCore.Core

This package contains the core integration logic for ASP.NET Core applications. It provides the essential middleware to wire up the framework's features into the request pipeline.

## Key Features

-   **`UnitOfWorkMiddleware`:** Automatically wraps each HTTP request in a Unit of Work, providing transactional safety and consistency for all operations.
-   **`UseNetMX()` Extension Method:** A simple and clean way to register the framework's core middleware.
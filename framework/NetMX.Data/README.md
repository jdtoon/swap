# NetMX.Data

This package contains the core, database-agnostic abstractions for data access.

## Key Features

-   **`IDataFilter`:** Defines the central service for enabling and disabling global data filters (like Soft Delete or Multi-Tenancy) in a controlled scope. This is a key mechanism for handling cross-cutting data concerns.
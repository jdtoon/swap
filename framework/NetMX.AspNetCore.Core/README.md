# NetMX.AspNetCore.Core

**Core ASP.NET Core integration for NetMX applications.**

This package provides middleware, filters, and extensions for integrating NetMX with ASP.NET Core applications.

## Overview

NetMX.AspNetCore.Core provides:
- **Unit of Work Middleware**: Automatic transaction boundaries per request
- **Exception Handling**: Global error handling and standardized responses
- **Validation**: Automatic model validation
- **Current User**: HTTP context integration
- **UseNetMX()**: Simple setup

Perfect for ASP.NET Core applications using NetMX.

## Installation

```bash
dotnet add package NetMX.AspNetCore.Core
```

## Key Features

### 1. Unit of Work Middleware

Automatic transaction per request:

```csharp
app.UseNetMX();  // Wraps every request in UoW
```

**What it does**:
- Begins UoW at request start
- Commits if successful
- Rolls back on exceptions
- Works with `[UnitOfWork]` attribute

### 2. Exception Handling

Global error handling:

```csharp
app.UseNetMX();  // Includes exception handling

// Exceptions are automatically caught and formatted
```

**Error Response Format**:
```json
{
  "error": {
    "code": "BUSINESS_EXCEPTION",
    "message": "Product not found",
    "details": null
  }
}
```

### 3. Validation Middleware

Automatic model validation:

```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    // ModelState automatically validated
    // If invalid, 400 Bad Request returned
    
    return Ok(await _service.CreateAsync(dto));
}
```

## Usage

### Setup in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add NetMX services
builder.Services.AddNetMXCore();
builder.Services.AddNetMXAspNetCore();

var app = builder.Build();

// Use NetMX middleware (UoW, validation, exception handling)
app.UseNetMX();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### Current User Integration

```csharp
public class OrderController : Controller
{
    private readonly ICurrentUser _currentUser;
    private readonly IOrderService _orderService;
    
    public OrderController(ICurrentUser currentUser, IOrderService orderService)
    {
        _currentUser = currentUser;
        _orderService = orderService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        // Current user automatically available
        var userId = _currentUser.UserId;
        
        return Ok(await _orderService.CreateAsync(dto));
    }
}
```

## API Reference

### UseNetMX()

Adds NetMX middleware to the pipeline:
- Unit of Work middleware
- Exception handling middleware
- Validation middleware

## Dependencies

- `NetMX.Core` - Core utilities
- `Microsoft.AspNetCore.Http` - HTTP abstractions

## Related Packages

- **[NetMX.AspNetCore.Mvc](../NetMX.AspNetCore.Mvc/)** - MVC extensions
- **[NetMX.Ddd.Application](../NetMX.Ddd.Application/)** - Application layer

## Documentation

- [Architecture Decisions](../../docs/ARCHITECTURE-DECISIONS.md)
- [Quick Start Guide](../../docs/QUICK-START.md)

## License

MIT License - See [LICENSE](../../LICENSE) file for details.
# Authorization Module

Authorization module for NetMX framework.

## Structure

- **Authorization.Core** - Domain entities and value objects
- **Authorization.Contracts** - DTOs and service interfaces
- **Authorization.Application** - Service implementations
- **Authorization.Web** - Controllers, views, and UI components

## Getting Started

### Generate Features

```bash
cd Authorization.Web
netmx generate crud YourEntity
```

### Integration

Add to your application's `Program.cs`:

```csharp
// Add services
builder.Services.AddAuthorization();

// Configure middleware
app.UseAuthorization();
```

## Features

List your module's features here.

## License

MIT
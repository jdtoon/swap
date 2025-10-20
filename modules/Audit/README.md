# Audit Module

Audit module for NetMX framework.

## Structure

- **Audit.Core** - Domain entities and value objects
- **Audit.Contracts** - DTOs and service interfaces
- **Audit.Application** - Service implementations
- **Audit.Web** - Controllers, views, and UI components

## Getting Started

### Generate Features

```bash
cd Audit.Web
netmx generate crud YourEntity
```

### Integration

Add to your application's `Program.cs`:

```csharp
// Add services
builder.Services.AddAudit();

// Configure middleware
app.UseAudit();
```

## Features

List your module's features here.

## License

MIT
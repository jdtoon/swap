# Settings Module

Settings module for NetMX framework.

## Structure

- **Settings.Core** - Domain entities and value objects
- **Settings.Contracts** - DTOs and service interfaces
- **Settings.Application** - Service implementations
- **Settings.Web** - Controllers, views, and UI components

## Getting Started

### Generate Features

```bash
cd Settings.Web
netmx generate crud YourEntity
```

### Integration

Add to your application's `Program.cs`:

```csharp
// Add services
builder.Services.AddSettings();

// Configure middleware
app.UseSettings();
```

## Features

List your module's features here.

## License

MIT
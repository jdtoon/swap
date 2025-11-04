# Swap.Modularity

Lightweight modularity primitives for assembling modules in a Modular Monolith architecture.

## Concepts

- `IModule`: A module with Name, DependsOn and hooks to configure Services, Endpoints, and (optionally) Event Chains.
- `ModuleCatalog`: Holds ordered modules after dependency validation (topological sort).
- Host extensions: `AddSwapModules`, `MapSwapModuleEndpoints`, and `ConfigureSwapModuleEventChains`.

## Quick start

```csharp
// Program.cs (host)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwapModules(builder.Configuration);

var app = builder.Build();

app.MapSwapModuleEndpoints();
// Optional: if you have an event system registrar
// using var scope = app.Services.CreateScope();
// var registrar = scope.ServiceProvider.GetRequiredService<IEventChainRegistrar>();
// app.Services.ConfigureSwapModuleEventChains(registrar);

app.Run();
```

```csharp
// Example module
public sealed class OrdersModule : IModule
{
    public string Name => "Orders";
    public IReadOnlyList<string> DependsOn => new [] { "Inventory" };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext, handlers, etc.
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        // endpoints.MapControllerRoute(...); or minimal APIs
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // registrar.Register("Orders.OrderCreated", (OrderCreated e) => ...);
    }
}
```

## Notes

- Event chain registration is optional. The interface allows us to plug in the Swap Event System later or an out-of-process bus.
- Module discovery scans loaded assemblies by default; you can pass specific assemblies if needed.
- Dependency cycles and missing dependencies throw informative exceptions.

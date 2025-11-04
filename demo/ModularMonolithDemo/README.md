# Modular Monolith Demo (Preview)

This demo shows how modules plug into a host via `Swap.Modularity`.

- Host: `src/Web` (minimal API)
- Module: `src/Modules/Orders/Orders.Module` with `OrdersModule : IModule`
- Contracts: `src/Modules/Orders/Orders.Contracts`

Run (after adding these projects to a solution):

```pwsh
# From repo root, add projects to a solution (optional during preview)
# dotnet new sln -n ModularMonolithDemo
# dotnet sln add demo/ModularMonolithDemo/src/Web/Web.csproj
# dotnet sln add demo/ModularMonolithDemo/src/Modules/Orders/Orders.Module/Orders.Module.csproj
# dotnet sln add demo/ModularMonolithDemo/src/Modules/Orders/Orders.Contracts/Orders.Contracts.csproj

# Restore/build/run
# dotnet build
# dotnet run --project demo/ModularMonolithDemo/src/Web/Web.csproj
```

Then visit:
- http://localhost:5000
- http://localhost:5000/orders/ping

Notes:
- The demo uses reflection-based module discovery. The host project references `Orders.Module`, which brings its assembly into the app domain.
- `ConfigureEventChains` is a placeholder for the Swap Event System registrar; we’ll wire it next.

Troubleshooting:
- If `/orders/ping` returns 404, ensure the host passes the module assembly to `AddSwapModules`:
	- In `src/Web/Program.cs` we use: `AddSwapModules(builder.Configuration, new[] { typeof(OrdersModule).Assembly })` to guarantee the module assembly is loaded for discovery.

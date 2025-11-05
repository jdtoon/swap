# Web Host – Responsibilities and Wiring

The host composes modules and provides the MVC shell and event system runtime.

## Program.cs essentials

```csharp
var builder = WebApplication.CreateBuilder(args);

// MVC + HTMX + in-memory server event registrar
var mvc = builder.Services.AddControllersWithViews();
builder.Services.AddSwapServerEventChains();
builder.Services.AddSwapHtmx();

// Module discovery (automatic – host references modules)
builder.Services.AddSwapModules(builder.Configuration);

// Auto-discover module RCLs and UI chain contributors in *.Web assemblies
mvc.AddSwapModuleApplicationParts();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmxShell();
app.UseSwapHtmx();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapSwapModuleEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapSwapHtmxDevEndpoints();
}

// Apply server event chains + module UI chains
using (var scope = app.Services.CreateScope())
{
    var registrar = scope.ServiceProvider.GetRequiredService<IEventChainRegistrar>();
    app.Services.ConfigureSwapModuleEventChains(registrar);

    var swapOptions = scope.ServiceProvider.GetRequiredService<Swap.Htmx.Events.SwapEventBusOptions>();
    app.Services.ConfigureSwapModuleUiChains(swapOptions);
}

app.Run();
```

## Configuration

- Data selection via appsettings or env:
  - `Data:Provider` = `Sqlite` | `Postgres` | `SqlServer`
  - `ConnectionStrings:Todos` = provider-specific connection
  - `Data:MigrateOnStartup` = `true` to apply migrations

## Dev endpoints (Development only)

- `/_swap/dev/events` – HTML dashboard
- `/_swap/dev/events.json` – chains JSON
- `/_swap/dev/events.meta.json` – resolution mode/depth
- `/_swap/dev/explain.json?event=...` – server-side resolution preview

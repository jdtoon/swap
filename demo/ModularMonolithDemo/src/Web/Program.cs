using ModularMonolithDemo.Web;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;
using Swap.Htmx;
using Swap.Htmx.ServerEvents;
using Swap.Htmx.Dev;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

var builder = WebApplication.CreateBuilder(args);

// MVC + HTMX + Events
var mvc = builder.Services.AddControllersWithViews();

// Choose in-memory (default) or RabbitMQ distributed based on configuration
builder.Services.AddSwapServerEventChainsFromConfiguration(builder.Configuration);
builder.Services.AddSwapHtmx();

// Register modules – automatic discovery (host references modules so they're already loaded)
builder.Services.AddSwapModules(builder.Configuration);

// Auto-discover MVC parts from any *.Web RCL assemblies
mvc.AddSwapModuleApplicationParts();

var app = builder.Build();

// Static + routing + HTMX shell and events
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmxShell();
app.UseSwapHtmx();

// Map MVC default route
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

// Map any minimal endpoints provided by modules (if any)
app.MapSwapModuleEndpoints();

// Dev endpoints for visualizing event chains (Development only)
if (app.Environment.IsDevelopment())
{
	app.MapSwapHtmxDevEndpoints();
}

// After the app is built, allow modules to register event chains and UI chains
using (var scope = app.Services.CreateScope())
{
	var registrar = scope.ServiceProvider.GetRequiredService<IEventChainRegistrar>();
	app.Services.ConfigureSwapModuleEventChains(registrar);

    // Apply module-contributed HTMX UI chains
    var swapOptions = scope.ServiceProvider.GetRequiredService<Swap.Htmx.Events.SwapEventBusOptions>();
    app.Services.ConfigureSwapModuleUiChains(swapOptions);
}

app.Run();

public partial class Program { }
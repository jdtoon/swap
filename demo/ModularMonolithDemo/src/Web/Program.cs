using ModularMonolithDemo.Modules.Orders.Module;
using ModularMonolithDemo.Modules.Inventory.Module;
using ModularMonolithDemo.Modules.Todos.Module;
using ModularMonolithDemo.Modules.Todos.Web.Controllers;
using ModularMonolithDemo.Modules.Todos.Web.Events;
using ModularMonolithDemo.Modules.Demo.Module;
using ModularMonolithDemo.Modules.Demo.Web.Controllers;
using ModularMonolithDemo.Web;
using ModularMonolithDemo.Modules.Orders.Contracts;
using ModularMonolithDemo.Modules.Inventory.Contracts;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;
using Swap.Htmx;
using Swap.Htmx.Dev;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

var builder = WebApplication.CreateBuilder(args);

// MVC + HTMX + Events
var mvc = builder.Services.AddControllersWithViews();

// Register our simple in-process event registrar
builder.Services.AddSingleton<IEventChainRegistrar, SimpleEventChainRegistrar>();
builder.Services.AddSwapHtmx(opts =>
{
	// Host-level chain example
	opts.Chain(OrderEvents.OrderCreated, InventoryUIEvents.Refresh);
	// Module-owned UI chains registered here
	TodosUiChains.Configure(opts);
});

// Register modules and explicitly include module assemblies so they're loaded for discovery
builder.Services.AddSwapModules(builder.Configuration, new[] { typeof(OrdersModule).Assembly, typeof(InventoryModule).Assembly, typeof(TodosModule).Assembly, typeof(DemoModule).Assembly });

// Ensure MVC discovers controllers/views from module RCLs
mvc.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TodosUiController).Assembly));
mvc.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(DemoController).Assembly));

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

// After the app is built, allow modules to register event chains
using (var scope = app.Services.CreateScope())
{
	var registrar = scope.ServiceProvider.GetRequiredService<IEventChainRegistrar>();
	app.Services.ConfigureSwapModuleEventChains(registrar);
}

app.Run();

using ModularMonolithDemo.Modules.Orders.Module;
using ModularMonolithDemo.Modules.Inventory.Module;
using ModularMonolithDemo.Web;
using ModularMonolithDemo.Modules.Orders.Contracts;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register our simple in-process event registrar
builder.Services.AddSingleton<IEventChainRegistrar, SimpleEventChainRegistrar>();

// Register modules and explicitly include module assemblies so they're loaded for discovery
builder.Services.AddSwapModules(builder.Configuration, new[] { typeof(OrdersModule).Assembly, typeof(InventoryModule).Assembly });

var app = builder.Build();

app.MapSwapModuleEndpoints();

// After the app is built, allow modules to register event chains
using (var scope = app.Services.CreateScope())
{
	var registrar = scope.ServiceProvider.GetRequiredService<IEventChainRegistrar>();
	app.Services.ConfigureSwapModuleEventChains(registrar);
}

app.MapGet("/", () => Results.Content("<h1>Modular Monolith Demo</h1><ul><li><a href=\"/orders/ping\">/orders/ping</a></li><li><a href=\"/inventory/ping\">/inventory/ping</a></li><li><a href=\"/inventory/dashboard\">/inventory/dashboard</a></li></ul><form method=\"post\" action=\"/orders/create\"><button type=\"submit\">Create Order (emit event)</button></form>", "text/html"));

app.Run();

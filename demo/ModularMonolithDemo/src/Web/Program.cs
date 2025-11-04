using ModularMonolithDemo.Modules.Orders.Module;
using ModularMonolithDemo.Modules.Inventory.Module;
using ModularMonolithDemo.Web;
using ModularMonolithDemo.Modules.Orders.Contracts;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;
using Swap.Htmx;
using Swap.Htmx.Dev;
using ModularMonolithDemo.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Register our simple in-process event registrar
builder.Services.AddSingleton<IEventChainRegistrar, SimpleEventChainRegistrar>();
builder.Services.AddSwapHtmx(opts =>
{
	// Example UI chain: when inventory changes, refresh the inventory panel
	opts.Chain(AppEvents.UI.InventoryChanged, AppEvents.UI.InventoryRefresh);
});

// Register modules and explicitly include module assemblies so they're loaded for discovery
builder.Services.AddSwapModules(builder.Configuration, new[] { typeof(OrdersModule).Assembly, typeof(InventoryModule).Assembly });

var app = builder.Build();

// Swap.Htmx middlewares (capture subscriptions and emit HX-Trigger headers)
app.UseSwapHtmx();

app.MapSwapModuleEndpoints();

// Dev endpoints for visualizing event chains (Development only)
app.MapSwapHtmxDevEndpoints();

// After the app is built, allow modules to register event chains
using (var scope = app.Services.CreateScope())
{
	var registrar = scope.ServiceProvider.GetRequiredService<IEventChainRegistrar>();
	app.Services.ConfigureSwapModuleEventChains(registrar);
}

app.MapGet("/", () =>
{
    var refresh = AppEvents.UI.InventoryRefresh;
    var html = @"<!doctype html>
<html>
	<head>
		<meta charset='utf-8' />
		<title>Modular Monolith Demo</title>
		<script src='https://unpkg.com/htmx.org@2.0.3'></script>
		<style>
			body { font-family: system-ui, sans-serif; padding: 1rem; }
			.panel { border: 1px solid #ddd; padding: 1rem; margin-bottom: 1rem; border-radius: 8px; }
			.row { display:flex; gap:1rem; }
			.col { flex:1; }
			button { padding: .5rem 1rem; }
		</style>
	</head>
	<body>
		<h1>Modular Monolith Demo</h1>
		<div class='row'>
			<div class='panel col'>
				<h3>Orders</h3>
				<div id='orders-panel' hx-get='/orders/ping' hx-trigger='load' hx-target='#orders-panel' hx-swap='innerHTML'>Loading Orders...</div>
				<form hx-post='/orders/create' hx-swap='none' style='margin-top: .5rem;'>
					<button type='submit'>Create Order (emit event)</button>
				</form>
			</div>
			<div class='panel col'>
				<h3>Inventory</h3>
				<div id='inventory-panel' hx-get='/inventory/dashboard' hx-trigger='load, {REFRESH} from:body' hx-target='#inventory-panel' hx-swap='innerHTML'>Loading Inventory...</div>
			</div>
		</div>
	</body>
</html>";
    html = html.Replace("{REFRESH}", refresh);
    return Results.Content(html, "text/html");
});

app.Run();

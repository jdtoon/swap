using ModularMonolithDemo.Modules.Orders.Module;
using ModularMonolithDemo.Modules.Inventory.Module;
using ModularMonolithDemo.Web;
using ModularMonolithDemo.Modules.Orders.Contracts;
using ModularMonolithDemo.Modules.Inventory.Contracts;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;
using Swap.Htmx;
using Swap.Htmx.Dev;
// removed root contracts; events are module-owned

var builder = WebApplication.CreateBuilder(args);

// Register our simple in-process event registrar
builder.Services.AddSingleton<IEventChainRegistrar, SimpleEventChainRegistrar>();
builder.Services.AddSwapHtmx(opts =>
{
    // When an order is created, refresh the inventory panel
    opts.Chain(OrderEvents.OrderCreated, InventoryUIEvents.Refresh);
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
		var refresh = InventoryUIEvents.Refresh;
		var html = @"<!doctype html>
<html>
	<head>
		<meta charset='utf-8' />
		<title>Modular Monolith Demo</title>
		<script src='https://unpkg.com/htmx.org@2.0.3'></script>
		<link href='https://cdn.jsdelivr.net/npm/daisyui@4.12.10/dist/full.min.css' rel='stylesheet' type='text/css' />
		<script src='https://cdn.tailwindcss.com'></script>
	</head>
	<body>
		<div class='container mx-auto p-6'>
			<h1 class='text-3xl font-bold mb-6'>Modular Monolith Demo</h1>
			<div class='grid grid-cols-1 md:grid-cols-2 gap-4'>
				<div class='card bg-base-100 shadow'>
					<div class='card-body'>
						<h2 class='card-title'>Orders</h2>
						<div id='orders-panel' class='min-h-[3rem]' hx-get='/orders/ping' hx-trigger='load' hx-target='#orders-panel' hx-swap='innerHTML'>Loading Orders...</div>
						<form hx-post='/orders/create' hx-swap='none' class='mt-2'>
							<button type='submit' class='btn btn-primary'>Create Order (emit event)</button>
						</form>
					</div>
				</div>
				<div class='card bg-base-100 shadow'>
					<div class='card-body'>
						<h2 class='card-title'>Inventory</h2>
						<div id='inventory-panel' class='min-h-[3rem]' hx-get='/inventory/dashboard' hx-trigger='load, {REFRESH} from:body' hx-target='#inventory-panel' hx-swap='innerHTML'>Loading Inventory...</div>
					</div>
				</div>
			</div>
		</div>
	</body>
</html>";
		html = html.Replace("{REFRESH}", refresh);
		return Results.Content(html, "text/html");
});

app.Run();

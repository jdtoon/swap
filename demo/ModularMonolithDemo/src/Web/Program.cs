using ModularMonolithDemo.Modules.Orders.Module;
using Swap.Modularity.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register modules and explicitly include the Orders module assembly so it's loaded for discovery
builder.Services.AddSwapModules(builder.Configuration, new[] { typeof(OrdersModule).Assembly });

var app = builder.Build();

app.MapSwapModuleEndpoints();

app.MapGet("/", () => Results.Content("<h1>Modular Monolith Demo</h1><p>Try <a href=\"/orders/ping\">/orders/ping</a></p>", "text/html"));

app.Run();

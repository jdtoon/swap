using ModularMonolithDemo.Modules.Todos.Module;
using ModularMonolithDemo.Modules.Todos.Web.Controllers;
using ModularMonolithDemo.Modules.Todos.Web.Events;
using ModularMonolithDemo.Modules.Demo.Module;
using ModularMonolithDemo.Modules.Demo.Web.Controllers;
using ModularMonolithDemo.Web;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;
using Swap.Htmx;
using Swap.Htmx.ServerEvents;
using Swap.Htmx.Dev;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using ModularMonolithDemo.Modules.Todos.Module.Persistence;

var builder = WebApplication.CreateBuilder(args);

// MVC + HTMX + Events
var mvc = builder.Services.AddControllersWithViews();

// Register the in-memory server event chain registrar (domain/server events)
builder.Services.AddSwapServerEventChains();
builder.Services.AddSwapHtmx(opts =>
{
	// Module-owned UI chains registered here
	TodosUiChains.Configure(opts);
});

// Register modules and explicitly include module assemblies so they're loaded for discovery
builder.Services.AddSwapModules(builder.Configuration, new[] { typeof(TodosModule).Assembly, typeof(DemoModule).Assembly });

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

	// Optional: apply module migrations/ensure created on startup (SQLite default)
	var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
	var migrate = bool.TryParse(cfg["Data:MigrateOnStartup"], out var m) && m;
	if (migrate && Program.TryInitializeDatabase(scope.ServiceProvider))
	{
		var todosDb = scope.ServiceProvider.GetService<TodosDbContext>();
		if (todosDb is not null)
		{
			// SQLite path uses EnsureCreated for speed; migrations shims can be added per provider later
			todosDb.Database.EnsureCreated();
		}
	}
}

app.Run();

public partial class Program { }
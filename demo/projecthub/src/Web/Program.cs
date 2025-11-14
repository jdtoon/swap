using ProjectHub.Web.Infrastructure;
using Swap.Modularity.Abstractions;
using Swap.Modularity.Hosting;
using Swap.Htmx;
using Swap.Htmx.ServerEvents;
using Swap.Htmx.Dev;
using Swap.Htmx.Events;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

var builder = WebApplication.CreateBuilder(args);

// MVC + HTMX + Events
var mvc = builder.Services.AddControllersWithViews(options =>
{
    // Accept both 123.45 and 123,45 for decimal inputs
    options.ModelBinderProviders.Insert(0, new ProjectHub.Web.Infrastructure.InvariantDecimalModelBinderProvider());
});

// Choose in-memory (default) or RabbitMQ distributed based on configuration
builder.Services.AddSwapServerEventChainsFromConfiguration(builder.Configuration);
builder.Services.AddSwapHtmx();

// Enhanced SSE services with connection management and event-driven broadcasting
builder.Services.AddSseEventBridge();
builder.Services.AddSseFallback(options =>
{
    options.DefaultPollInterval = 3000; // 3 second polling fallback
    options.MaxSseRetries = 5;
    options.EnableFallback = true;
});

// Add memory cache for SSE fallback caching
builder.Services.AddMemoryCache();

// Register real-time event handler for automatic SSE broadcasting
builder.Services.AddScoped<ProjectHub.Web.Infrastructure.RealTimeEventHandler>();

// Register modules – automatic discovery (host references modules so they're already loaded)
builder.Services.AddSwapModules(builder.Configuration);

// Auto-discover MVC parts from any *.Web RCL assemblies
// This also auto-registers ISwapUiChainContributor implementations
mvc.AddSwapModuleApplicationParts();

var app = builder.Build();

// Static + routing + HTMX shell and events
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmxShell();
app.UseSwapHtmx();
app.UseSseEventBridge();

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
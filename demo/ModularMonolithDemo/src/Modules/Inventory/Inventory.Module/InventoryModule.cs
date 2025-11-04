using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithDemo.Modules.Orders.Contracts;
using Swap.Modularity.Abstractions;

namespace ModularMonolithDemo.Modules.Inventory.Module;

public sealed class InventoryModule : IModule
{
    public string Name => "Inventory";
    public IReadOnlyList<string> DependsOn => new[] { "Orders" };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InventoryProjection>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/inventory/ping", () => Results.Content("<div>Inventory module OK</div>", "text/html"));
        endpoints.MapGet("/inventory/dashboard", (InventoryProjection proj) => Results.Content($"<div>Orders processed: <strong>{proj.OrdersCount}</strong></div>", "text/html"));
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        registrar.Register<OrderCreated>(OrderEvents.OrderCreated, async (evt, sp) =>
        {
            var proj = sp.GetRequiredService<InventoryProjection>();
            proj.OrdersCount++;
            await Task.CompletedTask;
        });
    }
}

public sealed class InventoryProjection
{
    public int OrdersCount { get; set; }
}

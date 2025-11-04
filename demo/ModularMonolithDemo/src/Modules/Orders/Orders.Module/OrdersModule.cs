using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithDemo.Modules.Orders.Contracts;
using Swap.Modularity.Abstractions;

namespace ModularMonolithDemo.Modules.Orders.Module;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";
    public IReadOnlyList<string> DependsOn => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOrdersService, OrdersService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/orders/ping", () => Results.Ok(new { ok = true, module = "Orders" }));
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // Example registration: registrar.Register(OrderEvents.OrderCreated, (OrderCreated e) => { /* handle */ });
    }
}

public interface IOrdersService
{
    string Ping();
}

public sealed class OrdersService : IOrdersService
{
    public string Ping() => "Orders service pong";
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithDemo.Modules.Orders.Contracts;
using Swap.Modularity.Abstractions;
using Swap.Htmx.Events;

namespace ModularMonolithDemo.Modules.Orders.Module;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";
    public IReadOnlyList<string> DependsOn => Array.Empty<string>();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOrdersService, OrdersService>();
        services.AddSingleton<ModularMonolithDemo.Modules.Orders.Contracts.IOrdersReadApi>(sp => (OrdersService)sp.GetRequiredService<IOrdersService>());
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
    endpoints.MapGet("/orders/ping", () => Results.Content("<div>Orders module OK</div>", "text/html"));
        endpoints.MapPost("/orders/create", async (HttpContext ctx) =>
        {
            var registrar = ctx.RequestServices.GetRequiredService<IEventChainRegistrar>();
            var payload = new OrderCreated(Guid.NewGuid(), 10m);
            var svc = ctx.RequestServices.GetRequiredService<IOrdersService>();
            svc.SetLatest(new OrderSummaryDto(payload.OrderId, payload.Total));
            await registrar.PublishAsync(OrderEvents.OrderCreated, payload, ctx.RequestServices);
            // Emit the domain event on the HTMX bus; chains map it to an Inventory UI refresh
            var bus = ctx.RequestServices.GetRequiredService<ISwapEventBus>();
            bus.Emit(OrderEvents.OrderCreated);
            return Results.Content("<div>Order created and event published. <a href=\"/inventory/dashboard\">Go to Inventory Dashboard</a></div>", "text/html");
        });
    }

    public void ConfigureEventChains(IEventChainRegistrar registrar)
    {
        // Example registration: registrar.Register(OrderEvents.OrderCreated, (OrderCreated e) => { /* handle */ });
    }
}

public interface IOrdersService
{
    string Ping();
    void SetLatest(OrderSummaryDto dto);
    OrderSummaryDto? GetLatest();
}

public sealed class OrdersService : IOrdersService, ModularMonolithDemo.Modules.Orders.Contracts.IOrdersReadApi
{
    private OrderSummaryDto? _latest;
    public string Ping() => "Orders service pong";
    public void SetLatest(OrderSummaryDto dto) => _latest = dto;
    public OrderSummaryDto? GetLatest() => _latest;
    OrderSummaryDto? ModularMonolithDemo.Modules.Orders.Contracts.IOrdersReadApi.GetLatestOrder() => _latest;
}

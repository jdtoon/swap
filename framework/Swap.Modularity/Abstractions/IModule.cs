using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Modularity.Abstractions;

public interface IModule
{
    string Name { get; }
    IReadOnlyList<string> DependsOn { get; }

    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints);
    void ConfigureEventChains(IEventChainRegistrar registrar) { }
}

public interface IEventChainRegistrar
{
    // Register a handler for a typed event. The service provider is provided for resolving scoped dependencies.
    void Register<TEvent>(string eventKey, Func<TEvent, IServiceProvider, Task> handler);

    // Publish a typed event to all subscribers.
    Task PublishAsync<TEvent>(string eventKey, TEvent payload, IServiceProvider services, CancellationToken ct = default);
}

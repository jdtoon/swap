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
    // Placeholder for integration with the Swap Event System
    // Implementations may be in-process or broker-backed in the future
    void Register(string eventKey, Delegate handler);
}

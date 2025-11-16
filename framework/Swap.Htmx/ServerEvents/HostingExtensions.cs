using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerEvents;

public static class ServerEventHostingExtensions
{
    public static IServiceCollection AddSwapServerEventChainsFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // For now we only support the in-memory server event chains in the core library.
        // Server event chain registration has been removed from the core library for now.
        return services;
    }
}

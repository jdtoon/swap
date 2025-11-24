using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swap.Htmx.Realtime;

namespace Swap.Htmx;

public static class SwapHtmxBackplaneExtensions
{
    /// <summary>
    /// Registers the default in-memory SSE backplane.
    /// Use this for single-server applications.
    /// </summary>
    public static IServiceCollection AddInMemorySseBackplane(this IServiceCollection services)
    {
        services.TryAddSingleton<ISseBackplane, InMemorySseBackplane>();
        return services;
    }
}

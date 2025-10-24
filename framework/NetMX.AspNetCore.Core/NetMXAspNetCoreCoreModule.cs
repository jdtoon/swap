using Microsoft.Extensions.DependencyInjection;
using NetMX.AspNetCore.Core.Uow;
using NetMX.DependencyInjection;

namespace NetMX.AspNetCore.Core;

/// <summary>
/// NetMX module for ASP.NET Core common functionality.
/// </summary>
public class NetMXAspNetCoreCoreModule : NetMXModule, ISingletonDependency
{
    /// <summary>
    /// Configures services for the ASP.NET Core module.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public override void ConfigureServices(IServiceCollection services)
    {
        // UnitOfWorkMiddleware is registered automatically via dependency injection
    }
}
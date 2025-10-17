using Microsoft.Extensions.DependencyInjection;
using NetMX.AspNetCore.Core.Uow;
using NetMX.DependencyInjection;

namespace NetMX.AspNetCore.Core;

public class NetMXAspNetCoreCoreModule : NetMXModule, ISingletonDependency
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // UnitOfWorkMiddleware is registered automatically via dependency injection
    }
}
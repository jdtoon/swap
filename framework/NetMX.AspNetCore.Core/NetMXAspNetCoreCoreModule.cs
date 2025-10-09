using Microsoft.Extensions.DependencyInjection;
using NetMX.AspNetCore.Uow;
using NetMX.DependencyInjection;

namespace NetMX.AspNetCore;

public class NetMXAspNetCoreCoreModule : NetMXModule, ISingletonDependency
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<UnitOfWorkMiddleware>();
    }
}
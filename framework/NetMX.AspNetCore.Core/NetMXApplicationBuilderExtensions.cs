using Microsoft.AspNetCore.Builder;
using NetMX.AspNetCore.Core.Uow;

namespace NetMX.AspNetCore.Core;

public static class NetMXApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNetMX(this IApplicationBuilder app)
    {
        app.UseMiddleware<UnitOfWorkMiddleware>();
        return app;
    }
}
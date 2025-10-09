using Microsoft.AspNetCore.Builder;
using NetMX.AspNetCore.Uow;

namespace NetMX.AspNetCore;

public static class NetMXApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNetMX(this IApplicationBuilder app)
    {
        app.UseMiddleware<UnitOfWorkMiddleware>();
        return app;
    }
}
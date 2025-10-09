using Microsoft.Extensions.DependencyInjection;

namespace NetMX;

public abstract class NetMXModule
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
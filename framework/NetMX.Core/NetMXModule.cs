using Microsoft.Extensions.DependencyInjection;

namespace NetMX;

/// <summary>
/// Base class for NetMX modules that provide infrastructure and feature registration.
/// </summary>
public abstract class NetMXModule
{
    /// <summary>
    /// Configures services for the module. Override this method to register your module's services.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
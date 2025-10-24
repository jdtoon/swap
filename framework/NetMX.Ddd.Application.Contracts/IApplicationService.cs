using NetMX.DependencyInjection;

namespace NetMX.Ddd.Application.Services;

/// <summary>
/// Marker interface for application services. Application services are automatically registered as transient dependencies.
/// </summary>
public interface IApplicationService : ITransientDependency
{
}
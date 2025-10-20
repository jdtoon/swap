namespace NetMX.DependencyInjection;

/// <summary>
/// Marker interface for services that should be registered with Transient lifetime.
/// Classes implementing this interface are automatically registered in the DI container.
/// </summary>
public interface ITransientDependency
{
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Modularity.Abstractions;

/// <summary>
/// Defines a self-contained module that can be registered in a modular application.
/// Modules encapsulate services, endpoints, and event chains.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the unique name of this module.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the names of modules this module depends on. Dependencies are loaded before this module.
    /// </summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Registers services required by this module into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">Application configuration for reading settings.</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// Configures HTTP endpoints (routes, controllers, minimal APIs) for this module.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to add routes to.</param>
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints);
    
    /// <summary>
    /// Registers event chains that map domain events to UI events. Optional - default implementation does nothing.
    /// </summary>
    /// <param name="registrar">The event chain registrar for subscribing to events.</param>
    void ConfigureEventChains(IEventChainRegistrar registrar) { }
}

/// <summary>
/// Provides functionality for registering and publishing typed events within a module.
/// Events allow decoupling between domain logic and UI updates.
/// </summary>
public interface IEventChainRegistrar
{
    /// <summary>
    /// Registers a handler for a typed event with the specified event key.
    /// The service provider is provided for resolving scoped dependencies.
    /// </summary>
    /// <typeparam name="TEvent">The type of event payload.</typeparam>
    /// <param name="eventKey">The unique key identifying this event chain.</param>
    /// <param name="handler">The async handler function that receives the event payload and service provider.</param>
    void Register<TEvent>(string eventKey, Func<TEvent, IServiceProvider, Task> handler);

    /// <summary>
    /// Publishes a typed event to all registered subscribers for the specified event key.
    /// </summary>
    /// <typeparam name="TEvent">The type of event payload.</typeparam>
    /// <param name="eventKey">The unique key identifying this event chain.</param>
    /// <param name="payload">The event data to send to handlers.</param>
    /// <param name="services">The service provider for resolving scoped dependencies in handlers.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishAsync<TEvent>(string eventKey, TEvent payload, IServiceProvider services, CancellationToken ct = default);
}

// Diagnostics types removed to keep abstractions minimal and avoid cross-lib coupling at this stage.

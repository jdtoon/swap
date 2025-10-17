using Microsoft.Extensions.DependencyInjection;
using NetMX.Ddd.Domain.Events;
using NetMX.DependencyInjection;

namespace NetMX.Ddd.Application.Events;

/// <summary>
/// Default implementation of domain event dispatcher using service provider.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher, IScopedDependency
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        
        // Get all handlers for this event type
        var handlers = _serviceProvider.GetServices(handlerType);
        
        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<DomainEvent>.HandleAsync));
            if (handleMethod != null)
            {
                var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                await task;
            }
        }
    }

    public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}

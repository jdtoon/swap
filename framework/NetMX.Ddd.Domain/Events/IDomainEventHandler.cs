namespace NetMX.Ddd.Domain.Events;

/// <summary>
/// Handles a specific type of domain event.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

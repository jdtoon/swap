using NetMX.Ddd.Domain.Events;

namespace NetMX.Ddd.Domain.Entities;

/// <summary>
/// Base class for aggregate roots.
/// Aggregate roots can generate domain events.
/// </summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IGeneratesDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
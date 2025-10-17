namespace NetMX.Ddd.Domain.Events;

/// <summary>
/// Base class for all domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for this domain event.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// When this domain event occurred.
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

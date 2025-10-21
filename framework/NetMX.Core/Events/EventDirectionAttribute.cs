using System;

namespace NetMX.Events;

/// <summary>
/// Decorates event constants to specify their direction in the event graph.
/// Used by EventBus to enforce DAG rules and prevent infinite loops.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class EventDirectionAttribute : Attribute
{
    /// <summary>
    /// The direction of the event (Upstream, Downstream, or Terminal).
    /// </summary>
    public EventDirection Direction { get; }

    /// <summary>
    /// Creates a new EventDirectionAttribute.
    /// </summary>
    /// <param name="direction">The direction of the event.</param>
    public EventDirectionAttribute(EventDirection direction)
    {
        Direction = direction;
    }
}

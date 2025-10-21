namespace NetMX.Events;

/// <summary>
/// Defines the direction of event propagation.
/// Enforces a Directed Acyclic Graph (DAG) to prevent infinite loops.
/// </summary>
public enum EventDirection
{
    /// <summary>
    /// User-initiated events (e.g., "order.created", "product.updated").
    /// Can trigger Downstream events.
    /// Cannot be triggered by Downstream events.
    /// </summary>
    Upstream = 0,

    /// <summary>
    /// System-initiated events (e.g., "inventory.updated", "stats.refreshed").
    /// Can trigger other Downstream events or Terminal events.
    /// Cannot trigger Upstream events (enforced at runtime).
    /// </summary>
    Downstream = 1,

    /// <summary>
    /// End-of-chain events (e.g., "audit.logged", "notification.sent").
    /// Cannot trigger any other events.
    /// Used for side effects that should not cascade.
    /// </summary>
    Terminal = 2
}

namespace Swap.Htmx.Realtime;

/// <summary>
/// Controls how SSE connections handle bursts of outgoing events.
/// </summary>
public sealed class SseBroadcastOptions
{
    /// <summary>
    /// Maximum number of queued events per connection.
    /// When the queue is full, <see cref="DropStrategy"/> is applied.
    /// </summary>
    public int MaxQueuedEventsPerConnection { get; set; } = 100;

    /// <summary>
    /// Maximum size (in UTF-8 bytes) of a single event payload.
    /// If exceeded, the event is dropped.
    /// </summary>
    public int MaxEventBytes { get; set; } = 256 * 1024;

    /// <summary>
    /// Strategy used when the per-connection queue is full.
    /// </summary>
    public SseDropStrategy DropStrategy { get; set; } = SseDropStrategy.DropOldest;
}

/// <summary>
/// How to behave when a connection cannot keep up with outgoing events.
/// </summary>
public enum SseDropStrategy
{
    /// <summary>
    /// Drop the newest event (keep whatever is already queued).
    /// </summary>
    DropNewest,

    /// <summary>
    /// Drop the oldest queued event to make room for the newest.
    /// </summary>
    DropOldest,

    /// <summary>
    /// Disconnect the client when the queue overflows.
    /// </summary>
    Disconnect,
}

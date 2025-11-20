using System.Threading;
using System.Threading.Tasks;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Represents a message to be broadcast via SSE.
/// </summary>
public record SseMessage(
    string EventName,
    string Html,
    SseRecipientType RecipientType,
    string? RecipientValue = null,
    string[]? RecipientValues = null
);

/// <summary>
/// Defines the target audience for an SSE message.
/// </summary>
public enum SseRecipientType
{
    Broadcast,
    Room,
    User,
    Role,
    EventSubscription
}

/// <summary>
/// Abstraction for distributing SSE messages across multiple server instances.
/// </summary>
public interface ISseBackplane
{
    /// <summary>
    /// Publishes a message to the backplane to be distributed to all servers.
    /// </summary>
    Task PublishAsync(SseMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages from the backplane.
    /// </summary>
    /// <param name="handler">The action to execute when a message is received.</param>
    Task SubscribeAsync(Func<SseMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
}

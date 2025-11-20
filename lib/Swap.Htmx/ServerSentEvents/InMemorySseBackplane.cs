using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// In-memory implementation of ISseBackplane for single-server scenarios.
/// </summary>
internal sealed class InMemorySseBackplane : ISseBackplane
{
    private readonly ConcurrentBag<Func<SseMessage, CancellationToken, Task>> _handlers = new();
    private readonly ILogger<InMemorySseBackplane> _logger;

    public InMemorySseBackplane(ILogger<InMemorySseBackplane> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync(SseMessage message, CancellationToken cancellationToken = default)
    {
        // In-memory: just loop through handlers and await them.
        // In a real distributed system, this would push to Redis/NATS.
        foreach (var handler in _handlers)
        {
            try
            {
                await handler(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling SSE message in backplane.");
            }
        }
    }

    public Task SubscribeAsync(Func<SseMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        _handlers.Add(handler);
        return Task.CompletedTask;
    }
}

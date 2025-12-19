using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Represents an active SSE connection with user context and room subscriptions.
/// </summary>
public sealed class SseConnection : IRealtimeConnection
{
    private readonly ServerSentEventStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly HttpContext _httpContext;
    private readonly ConcurrentDictionary<string, bool> _subscribedEvents;
    private readonly ConcurrentDictionary<string, bool> _joinedRooms;

    private readonly object _queueLock = new();
    private readonly Queue<OutgoingMessage> _outgoingQueue = new();
    // Used as a "non-empty" signal (not a count). Capacity 1 avoids signal drift when dropping.
    private readonly SemaphoreSlim _queueSignal = new(0, 1);
    private Task? _writerTask;
    private SseBroadcastOptions _broadcastOptions = new();
    private ILogger<SseConnection>? _logger;

    private bool _disposed;

    public string Id { get; }
    public ClaimsPrincipal? User { get; }
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// Internal access to the SSE stream for advanced scenarios.
    /// </summary>
    internal ServerSentEventStream Stream => _stream;

    public SseConnection(string id, ServerSentEventStream stream, HttpContext httpContext)
    {
        Id = id;
        _stream = stream;
        _httpContext = httpContext;
        _cts = new CancellationTokenSource();
        _subscribedEvents = new ConcurrentDictionary<string, bool>();
        _joinedRooms = new ConcurrentDictionary<string, bool>();
        User = httpContext.User;
        ConnectedAt = DateTime.UtcNow;
    }

    internal void ConfigureBroadcastOptions(SseBroadcastOptions? options, ILogger<SseConnection>? logger)
    {
        if (options != null)
        {
            _broadcastOptions = options;
        }

        _logger = logger;
        EnsureWriterStarted();
    }

    private void EnsureWriterStarted()
    {
        if (_writerTask != null) return;

        // If the user constructed SseConnection manually, RequestServices may be null.
        var services = _httpContext.RequestServices;
        if (services != null)
        {
            if (_logger == null)
            {
                _logger = services.GetService(typeof(ILogger<SseConnection>)) as ILogger<SseConnection>;
            }

            var opts = services.GetService(typeof(IOptions<SseBroadcastOptions>)) as IOptions<SseBroadcastOptions>;
            if (opts?.Value != null)
            {
                _broadcastOptions = opts.Value;
            }
        }

        _writerTask = Task.Run(WriterLoopAsync);
    }

    private async Task WriterLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await _queueSignal.WaitAsync(_cts.Token);

                while (!_cts.Token.IsCancellationRequested)
                {
                    OutgoingMessage? message = null;
                    lock (_queueLock)
                    {
                        if (_outgoingQueue.Count == 0)
                        {
                            break;
                        }

                        message = _outgoingQueue.Dequeue();
                    }

                    try
                    {
                        await _stream.SendEventAsync(message.EventName, message.Html);
                        message.Completion.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        message.Completion.TrySetException(ex);
                        throw;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "SSE writer loop ended for connection {ConnectionId}", Id);
            _cts.Cancel();

            // Fail any pending sends.
            lock (_queueLock)
            {
                while (_outgoingQueue.Count > 0)
                {
                    _outgoingQueue.Dequeue().Completion.TrySetCanceled();
                }
            }
        }
    }

    /// <summary>
    /// Sends an SSE event to this specific connection.
    /// </summary>
    public async Task SendEventAsync(string eventName, string html)
    {
        if (_disposed || _cts.Token.IsCancellationRequested) return;
        EnsureWriterStarted();

        if (string.IsNullOrWhiteSpace(eventName)) return;
        if (html == null) return;

        var maxBytes = _broadcastOptions.MaxEventBytes;
        if (maxBytes > 0)
        {
            var size = Encoding.UTF8.GetByteCount(html);
            if (size > maxBytes)
            {
                _logger?.LogWarning(
                    "Dropping SSE event {EventName} for connection {ConnectionId}: payload size {SizeBytes} exceeds MaxEventBytes {MaxEventBytes}.",
                    eventName,
                    Id,
                    size,
                    maxBytes);
                return;
            }
        }

        var maxQueued = _broadcastOptions.MaxQueuedEventsPerConnection;
        if (maxQueued <= 0)
        {
            // Treat 0 as "no buffering": write inline.
            try
            {
                await _stream.SendEventAsync(eventName, html);
            }
            catch (Exception)
            {
                _cts.Cancel();
            }
            return;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var shouldSignal = false;

        lock (_queueLock)
        {
            var wasEmpty = _outgoingQueue.Count == 0;

            if (_outgoingQueue.Count >= maxQueued)
            {
                switch (_broadcastOptions.DropStrategy)
                {
                    case SseDropStrategy.DropNewest:
                        tcs.TrySetResult();
                        return;

                    case SseDropStrategy.DropOldest:
                        if (_outgoingQueue.Count > 0)
                        {
                            _outgoingQueue.Dequeue().Completion.TrySetResult();
                        }
                        // Still non-empty after replacement, so don't signal.
                        wasEmpty = false;
                        break;

                    case SseDropStrategy.Disconnect:
                        _cts.Cancel();
                        tcs.TrySetCanceled();
                        return;
                }
            }

            _outgoingQueue.Enqueue(new OutgoingMessage(eventName, html, tcs));
            shouldSignal = wasEmpty;
        }

        if (shouldSignal)
        {
            try
            {
                _queueSignal.Release();
            }
            catch
            {
                // Ignore: signal already set.
            }
        }

        await tcs.Task;
    }

    /// <summary>
    /// Joins a room for targeted broadcasting.
    /// </summary>
    public void JoinRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room)) return;
        _joinedRooms.TryAdd(room, true);
    }

    /// <summary>
    /// Leaves a room.
    /// </summary>
    public void LeaveRoom(string room)
    {
        if (string.IsNullOrWhiteSpace(room)) return;
        _joinedRooms.TryRemove(room, out _);
    }

    /// <summary>
    /// Subscribes to specific events.
    /// </summary>
    public void SubscribeToEvent(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        _subscribedEvents.TryAdd(eventName, true);
    }

    /// <summary>
    /// Unsubscribes from specific events.
    /// </summary>
    public void UnsubscribeFromEvent(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;
        _subscribedEvents.TryRemove(eventName, out _);
    }

    /// <summary>
    /// Checks if connection is subscribed to an event.
    /// </summary>
    public bool IsSubscribedToEvent(string eventName)
    {
        return _subscribedEvents.ContainsKey(eventName);
    }

    /// <summary>
    /// Checks if connection is in a specific room.
    /// </summary>
    public bool IsInRoom(string room)
    {
        return _joinedRooms.ContainsKey(room);
    }

    /// <summary>
    /// Gets all rooms this connection has joined.
    /// </summary>
    public IReadOnlyCollection<string> Rooms => _joinedRooms.Keys.ToList();

    /// <summary>
    /// Gets all events this connection is subscribed to.
    /// </summary>
    public IReadOnlyCollection<string> SubscribedEvents => _subscribedEvents.Keys.ToList();

    /// <summary>
    /// Checks if the connection is still active.
    /// </summary>
    public bool IsActive => !_disposed && !_cts.Token.IsCancellationRequested;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        try
        {
            _queueSignal.Release();
        }
        catch
        {
            // ignore
        }

        lock (_queueLock)
        {
            while (_outgoingQueue.Count > 0)
            {
                _outgoingQueue.Dequeue().Completion.TrySetCanceled();
            }
        }

        if (_writerTask != null)
        {
            try
            {
                await _writerTask;
            }
            catch
            {
                // ignore
            }
        }

        await _stream.DisposeAsync();
        _queueSignal.Dispose();
        _cts.Dispose();
    }

    private sealed record OutgoingMessage(string EventName, string Html, TaskCompletionSource Completion);
}
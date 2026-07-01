using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Realtime;

namespace Swap.Htmx.Results;

/// <summary>
/// Options for configuring an SSE connection result.
/// </summary>
public class SwapSseOptions
{
    /// <summary>
    /// The interval at which to send heartbeat comments to keep the connection alive.
    /// Default is 15 seconds.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// A list of rooms to automatically join upon connection.
    /// </summary>
    public IEnumerable<string> AutoSubscribeRooms { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// A list of events to automatically subscribe to upon connection.
    /// </summary>
    public IEnumerable<string> AutoSubscribeEvents { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Optional callback to execute when the connection is established.
    /// </summary>
    public Func<SseConnection, Task>? OnConnected { get; set; }

    /// <summary>
    /// Optional callback to execute when the connection is disconnected.
    /// </summary>
    public Func<string, Task>? OnDisconnected { get; set; }

    /// <summary>
    /// Optional validator to check if a user is allowed to join a specific room.
    /// If provided, this will be called for each room in AutoSubscribeRooms.
    /// Return true to allow, false to deny.
    /// </summary>
    public Func<SseConnection, string, Task<bool>>? CanJoinRoom { get; set; }
}

/// <summary>
/// An IResult implementation that establishes and maintains a Server-Sent Events (SSE) connection.
/// </summary>
public class SwapSseResult : IResult
{
    private readonly ISseConnectionRegistry _registry;
    private readonly SwapSseOptions _options;

    public SwapSseResult(ISseConnectionRegistry registry, Action<SwapSseOptions>? configure = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = new SwapSseOptions();
        configure?.Invoke(_options);
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        var response = context.Response;
        var ct = context.RequestAborted;

        // 1. Set Headers
        response.StatusCode = 200;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no"; // Nginx
        
        // 2. Flush headers immediately to establish connection
        await response.Body.FlushAsync(ct);

        // 3. Disable buffering if possible
        var bufferingFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        // 4. Create Connection
        var stream = new ServerSentEventStream(response, ct);
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new SseConnection(connectionId, stream, context);

        // 5. Register Connection
        _registry.RegisterConnection(connection);

        try
        {
            // 6. Handle Auto-Subscriptions
            foreach (var room in _options.AutoSubscribeRooms)
            {
                if (_options.CanJoinRoom != null)
                {
                    if (!await _options.CanJoinRoom(connection, room))
                    {
                        continue;
                    }
                }
                connection.JoinRoom(room);
            }

            foreach (var evt in _options.AutoSubscribeEvents)
            {
                connection.SubscribeToEvent(evt);
            }

            // 7. Run OnConnected callback
            if (_options.OnConnected != null)
            {
                await _options.OnConnected(connection);
            }

            // 8. Keep Alive Loop
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatInterval, ct);

                // Send an SSE comment (": keepalive") to hold the connection open without surfacing a
                // spurious client-visible event. Routed through the stream's write lock so it can never
                // race the event writer for the response body.
                await stream.SendKeepAliveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal disconnection
        }
        finally
        {
            // 9. Cleanup
            _registry.UnregisterConnection(connectionId);
            
            if (_options.OnDisconnected != null)
            {
                await _options.OnDisconnected(connectionId);
            }
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// Enhanced SSE result that integrates with the connection registry and event bridge.
/// </summary>
public sealed class EnhancedServerSentEventsResult : Microsoft.AspNetCore.Mvc.IActionResult
{
    private readonly Func<SseConnectionBuilder, CancellationToken, Task> _handler;

    public EnhancedServerSentEventsResult(Func<SseConnectionBuilder, CancellationToken, Task> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
    {
        var response = context.HttpContext.Response;

        // Set SSE headers
        response.StatusCode = 200;
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";

        // Disable response buffering for real-time streaming
        var bufferingFeature = context.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        var cancellationToken = context.HttpContext.RequestAborted;
        var stream = new ServerSentEventStream(response, cancellationToken);
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new SseConnection(connectionId, stream, context.HttpContext);

        // Register with connection registry if available
        var registry = context.HttpContext.RequestServices.GetService<ISseConnectionRegistry>();
        registry?.RegisterConnection(connection);

        var builder = new SseConnectionBuilder(connection);

        try
        {
            await _handler(builder, cancellationToken);
        }
        finally
        {
            registry?.UnregisterConnection(connectionId);
            await connection.DisposeAsync();
        }
    }
}

/// <summary>
/// Fluent builder for configuring SSE connections.
/// </summary>
public sealed class SseConnectionBuilder
{
    private readonly SseConnection _connection;

    internal SseConnectionBuilder(SseConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// The underlying SSE connection for direct access.
    /// </summary>
    public SseConnection Connection => _connection;

    /// <summary>
    /// Joins one or more rooms for targeted broadcasting.
    /// </summary>
    public SseConnectionBuilder WithRooms(params string[] rooms)
    {
        foreach (var room in rooms)
        {
            _connection.JoinRoom(room);
        }
        return this;
    }

    /// <summary>
    /// Subscribes to specific events.
    /// </summary>
    public SseConnectionBuilder WithEvents(params string[] events)
    {
        foreach (var eventName in events)
        {
            _connection.SubscribeToEvent(eventName);
        }
        return this;
    }

    /// <summary>
    /// Requires authentication for this connection.
    /// Throws if user is not authenticated.
    /// </summary>
    public SseConnectionBuilder WithAuthentication()
    {
        if (_connection.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("SSE connection requires authentication");
        }
        return this;
    }

    /// <summary>
    /// Sends initial state to the connection.
    /// </summary>
    public async Task<SseConnectionBuilder> WithInitialState(string eventName, string html)
    {
        await _connection.SendEventAsync(eventName, html);
        return this;
    }

    /// <summary>
    /// Sends initial state using a factory function.
    /// </summary>
    public async Task<SseConnectionBuilder> WithInitialState(string eventName, Func<Task<string>> htmlFactory)
    {
        var html = await htmlFactory();
        await _connection.SendEventAsync(eventName, html);
        return this;
    }

    /// <summary>
    /// Keeps the connection alive with periodic heartbeats.
    /// </summary>
    public async Task KeepAlive(TimeSpan? interval = null, CancellationToken cancellationToken = default)
    {
        var heartbeatInterval = interval ?? TimeSpan.FromSeconds(30);

        while (!cancellationToken.IsCancellationRequested && _connection.IsActive)
        {
            try
            {
                await Task.Delay(heartbeatInterval, cancellationToken);
                if (_connection.IsActive)
                {
                    await _connection.Stream.SendKeepAliveAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Connection lost, exit gracefully
                break;
            }
        }
    }
}
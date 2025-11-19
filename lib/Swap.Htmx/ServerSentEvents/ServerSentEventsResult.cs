using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// IActionResult that establishes a Server-Sent Events (SSE) connection.
/// Keeps connection open and streams events to the client.
/// </summary>
public sealed class ServerSentEventsResult : IActionResult
{
    private readonly Func<ServerSentEventStream, CancellationToken, Task> _handler;
    private readonly ILogger<ServerSentEventsResult>? _logger;

    public ServerSentEventsResult(
        Func<ServerSentEventStream, CancellationToken, Task> handler,
        ILogger<ServerSentEventsResult>? logger = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        
        _logger?.LogInformation("[SSE Result] Establishing SSE connection for {Path}", context.HttpContext.Request.Path);
        
        // Set SSE headers per W3C EventSource specification
        response.StatusCode = 200;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no"; // Prevent Nginx buffering
        
        _logger?.LogDebug("[SSE Result] SSE headers set: Content-Type={ContentType}", response.ContentType);
        
        // Disable response buffering for real-time streaming
        var bufferingFeature = context.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();
        _logger?.LogDebug("[SSE Result] Response buffering disabled");

        var cancellationToken = context.HttpContext.RequestAborted;
        var stream = new ServerSentEventStream(response, cancellationToken);

        try
        {
            _logger?.LogDebug("[SSE Result] Invoking SSE handler");
            await _handler(stream, cancellationToken);
            _logger?.LogDebug("[SSE Result] SSE handler completed");
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is normal for SSE
            _logger?.LogDebug("[SSE Result] SSE connection cancelled (client disconnected)");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[SSE Result] Error in SSE handler");
            throw;
        }
        finally
        {
            await stream.DisposeAsync();
            _logger?.LogDebug("[SSE Result] SSE stream disposed");
        }
    }
}

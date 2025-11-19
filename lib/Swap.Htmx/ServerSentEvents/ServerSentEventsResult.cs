using Microsoft.AspNetCore.Mvc;

namespace Swap.Htmx.ServerSentEvents;

/// <summary>
/// IActionResult that establishes a Server-Sent Events (SSE) connection.
/// Keeps connection open and streams events to the client.
/// </summary>
public sealed class ServerSentEventsResult : IActionResult
{
    private readonly Func<ServerSentEventStream, CancellationToken, Task> _handler;

    public ServerSentEventsResult(Func<ServerSentEventStream, CancellationToken, Task> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        
        // Set SSE headers per W3C EventSource specification
        response.StatusCode = 200;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no"; // Prevent Nginx buffering
        
        // Disable response buffering for real-time streaming
        var bufferingFeature = context.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        var cancellationToken = context.HttpContext.RequestAborted;
        var stream = new ServerSentEventStream(response, cancellationToken);

        try
        {
            await _handler(stream, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is normal for SSE
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }
}

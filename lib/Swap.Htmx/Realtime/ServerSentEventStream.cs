using System.Text;
using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Represents a Server-Sent Events (SSE) stream for sending real-time HTML updates to the client.
/// </summary>
public sealed class ServerSentEventStream : IAsyncDisposable
{
    private readonly HttpResponse _response;
    private readonly CancellationToken _cancellationToken;
    private bool _disposed;

    internal ServerSentEventStream(HttpResponse response, CancellationToken cancellationToken)
    {
        _response = response;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Sends an SSE event with HTML content.
    /// HTMX will swap this HTML into the target element.
    /// </summary>
    /// <param name="eventName">The event name (used in hx-sse="swap:eventName")</param>
    /// <param name="html">The HTML content to send</param>
    /// <param name="id">Optional event ID for reconnection support (Last-Event-ID)</param>
    public async Task SendEventAsync(string eventName, string html, string? id = null)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
        
        if (html == null)
            throw new ArgumentNullException(nameof(html));

        try
        {
            var message = FormatSseMessage(eventName, html, id);
            System.Diagnostics.Debug.WriteLine($"[SSE Stream] Sending event '{eventName}' with {html.Length} chars");
            await _response.WriteAsync(message, _cancellationToken);
            await _response.Body.FlushAsync(_cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[SSE Stream] Successfully sent and flushed event '{eventName}'");
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or request cancelled - expected
            System.Diagnostics.Debug.WriteLine($"[SSE Stream] Client disconnected while sending '{eventName}'");
            throw;
        }
        catch (System.IO.IOException ex)
        {
            // Network error - client likely disconnected
            throw new OperationCanceledException("Client disconnected", ex);
        }
    }

    /// <summary>
    /// Sends a comment to keep the connection alive.
    /// Comments are ignored by the client but prevent timeouts.
    /// </summary>
    public async Task SendKeepAliveAsync()
    {
        ThrowIfDisposed();
        await _response.WriteAsync(": keepalive\n\n", _cancellationToken);
        await _response.Body.FlushAsync(_cancellationToken);
    }

    /// <summary>
    /// Sends a retry directive to the client to control reconnection interval.
    /// </summary>
    /// <param name="milliseconds">Reconnection interval in milliseconds</param>
    public async Task SendRetryDirectiveAsync(int milliseconds)
    {
        ThrowIfDisposed();
        await _response.WriteAsync($"retry: {milliseconds}\n\n", _cancellationToken);
        await _response.Body.FlushAsync(_cancellationToken);
    }

    /// <summary>
    /// Formats data as SSE message according to W3C spec.
    /// Format: [id: {id}\n]event: {name}\ndata: {content}\n\n
    /// </summary>
    private static string FormatSseMessage(string eventName, string data, string? id = null)
    {
        var builder = new StringBuilder();
        
        // Optional ID for reconnection support
        if (!string.IsNullOrEmpty(id))
        {
            builder.Append("id: ").Append(id).Append('\n');
        }
        
        builder.Append("event: ").Append(eventName).Append('\n');
        
        // Normalize line endings: W3C SSE spec requires \n only (not \r\n)
        var normalizedData = data.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalizedData.Split('\n');
        
        // SSE spec: multi-line data must have "data: " prefix on each line
        foreach (var line in lines)
        {
            builder.Append("data: ").Append(line).Append('\n');
        }
        
        builder.Append('\n'); // Empty line signals end of message
        return builder.ToString();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServerSentEventStream));
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.ServerSentEvents;

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
    public async Task SendEventAsync(string eventName, string html)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
        
        if (html == null)
            throw new ArgumentNullException(nameof(html));

        var message = FormatSseMessage(eventName, html);
        await _response.WriteAsync(message, _cancellationToken);
        await _response.Body.FlushAsync(_cancellationToken);
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
    /// Formats data as SSE message according to spec.
    /// Format: event: {name}\ndata: {content}\n\n
    /// </summary>
    private static string FormatSseMessage(string eventName, string data)
    {
        var builder = new StringBuilder();
        builder.Append("event: ").Append(eventName).Append('\n');
        
        // SSE spec: multi-line data must have "data: " prefix on each line
        var lines = data.Split('\n');
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

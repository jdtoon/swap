using System.Net.WebSockets;
using System.Text;

namespace Swap.Htmx.WebSockets;

/// <summary>
/// Represents an active WebSocket connection with helper methods for sending messages.
/// </summary>
public class WebSocketConnection
{
    private readonly WebSocket _webSocket;
    private readonly string _connectionId;

    /// <summary>
    /// Gets the unique identifier for this connection.
    /// </summary>
    public string ConnectionId => _connectionId;

    /// <summary>
    /// Gets the underlying WebSocket instance.
    /// </summary>
    public WebSocket WebSocket => _webSocket;

    /// <summary>
    /// Gets whether the WebSocket is currently open and ready to send/receive.
    /// </summary>
    public bool IsOpen => _webSocket.State == WebSocketState.Open;

    public WebSocketConnection(WebSocket webSocket, string? connectionId = null)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _connectionId = connectionId ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Sends a text message over the WebSocket.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsOpen)
            throw new InvalidOperationException("WebSocket is not open");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    /// <summary>
    /// Receives a text message from the WebSocket.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received message, or null if the connection is closing.</returns>
    public async Task<string?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);
        var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        return Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
    }

    /// <summary>
    /// Closes the WebSocket connection gracefully.
    /// </summary>
    /// <param name="status">The close status code.</param>
    /// <param name="description">Optional description of why the connection is closing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CloseAsync(
        WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
        {
            await _webSocket.CloseAsync(status, description, cancellationToken);
        }
    }
}

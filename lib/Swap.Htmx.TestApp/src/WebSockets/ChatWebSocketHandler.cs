using Swap.Htmx.WebSockets;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Swap.Htmx.TestApp.WebSockets;

public class ChatMessage
{
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChatWebSocketHandler : SwapWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();

    protected override Task OnConnectedAsync(WebSocketConnection connection)
    {
        _connections.TryAdd(connection.ConnectionId, connection);
        return Task.CompletedTask;
    }

    protected override async Task OnMessageAsync(WebSocketConnection connection, string message)
    {
        try
        {
            // HTMX ws-send sends form data as JSON object like: {"username":"User","message":"Hello"}
            var formData = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            if (formData == null) return;

            if (!formData.TryGetValue("username", out var username) || 
                !formData.TryGetValue("message", out var messageText))
            {
                return;
            }

            var chatMessage = new ChatMessage
            {
                Username = username,
                Message = messageText,
                Timestamp = DateTime.UtcNow
            };

            // Render message HTML
            var html = await this.RenderPartialToStringAsync(
                "_ChatMessage",
                chatMessage);

            // Broadcast to all connected clients
            var tasks = _connections.Values
                .Where(c => c.IsOpen)
                .Select(c => c.SendAsync(html));

            await Task.WhenAll(tasks);
        }
        catch (JsonException)
        {
            // Invalid JSON - ignore
        }
    }

    protected override Task OnDisconnectedAsync(WebSocketConnection connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        return Task.CompletedTask;
    }
}

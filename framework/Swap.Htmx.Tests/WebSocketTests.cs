using System.Net.WebSockets;
using System.Text;
using Swap.Htmx.WebSockets;
using Xunit;

namespace Swap.Htmx.Tests;

public class WebSocketTests
{
    [Fact]
    public void WebSocketConnection_Constructor_SetsProperties()
    {
        // Arrange
        var webSocket = new TestWebSocket();
        var connectionId = "test-123";

        // Act
        var connection = new WebSocketConnection(webSocket, connectionId);

        // Assert
        Assert.Equal(connectionId, connection.ConnectionId);
        Assert.Same(webSocket, connection.WebSocket);
    }

    [Fact]
    public void WebSocketConnection_GeneratesIdIfNotProvided()
    {
        // Arrange
        var webSocket = new TestWebSocket();

        // Act
        var connection = new WebSocketConnection(webSocket);

        // Assert
        Assert.NotNull(connection.ConnectionId);
        Assert.NotEmpty(connection.ConnectionId);
    }

    [Fact]
    public void WebSocketConnection_IsOpen_ReturnsTrueWhenOpen()
    {
        // Arrange
        var webSocket = new TestWebSocket();
        webSocket.SetState(WebSocketState.Open);
        var connection = new WebSocketConnection(webSocket);

        // Act & Assert
        Assert.True(connection.IsOpen);
    }

    [Fact]
    public void WebSocketConnection_IsOpen_ReturnsFalseWhenClosed()
    {
        // Arrange
        var webSocket = new TestWebSocket();
        webSocket.SetState(WebSocketState.Closed);
        var connection = new WebSocketConnection(webSocket);

        // Act & Assert
        Assert.False(connection.IsOpen);
    }

    [Fact]
    public async Task WebSocketConnection_SendAsync_ThrowsWhenNotOpen()
    {
        // Arrange
        var webSocket = new TestWebSocket();
        webSocket.SetState(WebSocketState.Closed);
        var connection = new WebSocketConnection(webSocket);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.SendAsync("test message"));
    }

    [Fact]
    public async Task WebSocketConnection_SendAsync_SendsMessage()
    {
        // Arrange
        var webSocket = new TestWebSocket();
        webSocket.SetState(WebSocketState.Open);
        var connection = new WebSocketConnection(webSocket);
        var message = "Hello WebSocket";

        // Act
        await connection.SendAsync(message);

        // Assert
        Assert.Equal(message, webSocket.LastSentMessage);
    }

    [Fact]
    public async Task WebSocketConnection_ReceiveAsync_ReturnsMessage()
    {
        // Arrange
        var expectedMessage = "Received message";
        var webSocket = new TestWebSocket();
        webSocket.SetState(WebSocketState.Open);
        webSocket.MessageToReceive = expectedMessage;
        var connection = new WebSocketConnection(webSocket);

        // Act
        var result = await connection.ReceiveAsync();

        // Assert
        Assert.Equal(expectedMessage, result);
    }

    [Fact]
    public async Task WebSocketConnection_ReceiveAsync_ReturnsNullOnClose()
    {
        // Arrange
        var webSocket = new TestWebSocket();
        webSocket.SetState(WebSocketState.Open);
        webSocket.MessageType = WebSocketMessageType.Close;
        var connection = new WebSocketConnection(webSocket);

        // Act
        var result = await connection.ReceiveAsync();

        // Assert
        Assert.Null(result);
    }

    // Test WebSocket implementation for unit testing
    private class TestWebSocket : WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;
        
        public override WebSocketState State => _state;
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;

        public string? LastSentMessage { get; private set; }
        public string? MessageToReceive { get; set; }
        public WebSocketMessageType MessageType { get; set; } = WebSocketMessageType.Text;

        public void SetState(WebSocketState state) => _state = state;

        public override void Abort() { }
        public override void Dispose() { }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (MessageToReceive != null)
            {
                var bytes = Encoding.UTF8.GetBytes(MessageToReceive);
                Array.Copy(bytes, buffer.Array!, bytes.Length);
                return Task.FromResult(new WebSocketReceiveResult(bytes.Length, MessageType, true));
            }

            return Task.FromResult(new WebSocketReceiveResult(0, MessageType, true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            LastSentMessage = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);
            return Task.CompletedTask;
        }
    }
}

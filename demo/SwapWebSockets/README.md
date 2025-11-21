# SwapWebSockets Demo

This demo showcases the WebSocket integration in Swap.Htmx.

## Features
- **Bi-Directional Communication**: Send messages from client to server via WebSockets.
- **Server-Side Event Handling**: Trigger Swap Events from WebSocket messages.
- **Realtime Updates**: Broadcast HTML updates to all connected clients.

## Setup
1. Restore client libraries:
   ```bash
   libman restore
   ```
2. Run the application:
   ```bash
   dotnet run
   ```

## How it Works
1. **Connection**: The client connects to `/swap/ws` using `hx-ext="ws" ws-connect="/swap/ws"`.
2. **Sending**: The form sends a JSON message `{ "event": "chat.message", "payload": "..." }` via `ws-send`.
3. **Processing**: The server receives the message, triggers the `chat.message` event.
4. **Broadcasting**: The `ChatEventConfiguration` resolves the event to render `_ChatMessage.cshtml` and broadcasts it back to all clients.

For more details, see the [Realtime Documentation](../../lib/Swap.Htmx/docs/WebSockets.md).

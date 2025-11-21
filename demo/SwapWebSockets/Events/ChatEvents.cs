using System.Text.Json;
using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapWebSockets.Models;

namespace SwapWebSockets.Events;

public static class ChatEvents
{
    public static readonly EventKey Message = new("chat.message");
}

public class ChatEventConfiguration : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions options)
    {
        options.When(ChatEvents.Message)
            .Broadcast()
            .RefreshPartial("chat-messages", "_ChatMessage", (ctx, payload) => 
            {
                if (payload is JsonElement json)
                {
                    Console.WriteLine($"[ChatEvents] Payload Kind: {json.ValueKind}, Raw: {json}");
                    if (json.ValueKind == JsonValueKind.String)
                    {
                        return new ChatMessage { Payload = json.GetString() ?? "" };
                    }
                    return json.Deserialize<ChatMessage>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                Console.WriteLine($"[ChatEvents] Payload is not JsonElement: {payload?.GetType().Name ?? "null"}");
                return payload;
            }, swapMode: SwapMode.BeforeEnd);
    }
}

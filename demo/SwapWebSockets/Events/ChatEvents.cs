using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

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
            .RefreshPartial("chat-messages", "_ChatMessage", swapMode: SwapMode.BeforeEnd);
    }
}

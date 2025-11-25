using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace SwapPhase15.Handlers;

[SwapHandler(Priority = 1)]
public class UserClickedHandler : ISwapEventHandler<string>
{
    public Task HandleAsync(string @event, SwapResponseBuilder builder, CancellationToken cancellationToken = default)
    {
        // Example: Update a counter or show a toast
        builder.WithSuccessToast($"User clicked: {@event}")
               .WithClientAction("focus", "#input-field");
        return Task.CompletedTask;
    }
}
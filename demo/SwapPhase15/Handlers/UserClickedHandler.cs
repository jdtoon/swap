using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapPhase15.Events;

namespace SwapPhase15.Handlers;

[SwapHandler(Priority = 1)]
public class UserClickedHandler : ISwapEventHandler<UserClickedEvent>
{
    public Task HandleAsync(UserClickedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken = default)
    {
        // Example: Update a counter or show a toast
        builder.WithSuccessToast($"User {@event.Source}: {@event.Message}")
               .WithClientAction("focus", "#input-field");
        return Task.CompletedTask;
    }
}
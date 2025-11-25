using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapPhase15.Events;

namespace SwapPhase15.Handlers;

[SwapHandler(Priority = 2)]
public class CounterUpdatedHandler : ISwapEventHandler<CounterUpdatedEvent>
{
    public Task HandleAsync(CounterUpdatedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken = default)
    {
        builder.WithInfoToast($"Counter is now {@event.Count}")
               .WithClientAction("scroll", "#counter-section", "top");
        return Task.CompletedTask;
    }
}
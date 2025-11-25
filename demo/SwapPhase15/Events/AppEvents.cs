using Swap.Htmx.Attributes;

namespace SwapPhase15.Events;

[SwapEventSource]
public partial class AppEvents
{
    public const string UserClicked = "user.clicked";
    public const string CounterUpdated = "counter.updated";
    public const string StatsUpdated = "stats.updated";
}

using Swap.Htmx.Attributes;

namespace SwapSmallPartials.Modules.Analytics.Events;

[SwapEventSource]
public static partial class AnalyticsEvents
{
    public const string Purchase_Completed = "purchase.completed";
    public const string Cart_Abandoned = "cart.abandoned";
    public const string Stock_Replenished = "stock.replenished";
    public const string Hour_Advanced = "hour.advanced";
}

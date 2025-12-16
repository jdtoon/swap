using Swap.Htmx.Attributes;

namespace SwapDebtors.Events;

/// <summary>
/// Event keys for dashboard and UI actions.
/// "dashboard.refreshed" becomes DashboardEvents.Dashboard.Refreshed
/// </summary>
[SwapEventSource]
public partial class DashboardEvents
{
    public const string DashboardRefreshed = "dashboard.refreshed";
    public const string CurrencyRatesUpdated = "currency.rates.updated";
    public const string StatsUpdated = "stats.updated";
    public const string ActivityLogged = "activity.logged";
}

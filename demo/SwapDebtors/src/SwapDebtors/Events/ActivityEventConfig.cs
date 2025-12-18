using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Realtime;
using SwapDebtors.Models;

namespace SwapDebtors.Events;

/// <summary>
/// SSE-rendered activity feed items.
///
/// We render HTML for the SSE event name "activity.logged". The dashboard subscribes
/// via htmx-ext-sse (sse-swap="activity.logged") and inserts the HTML into
/// #activity-feed (hx-swap="afterbegin").
///
/// Note: This config is intentionally attached to the "sse:broadcast:activity.logged" key,
/// so it is used by Swap.Htmx's realtime bridge when broadcasting, without polluting normal
/// HTTP responses.
/// </summary>
public sealed class ActivityEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(SseEvents.Broadcast(DashboardEvents.ActivityLogged))
            // Empty targetId => realtime bridge emits raw HTML (no hx-swap-oob wrapper)
            .RefreshPartial(string.Empty, SwapViews.Dashboard._ActivityItem, (ctx, payload) =>
            {
                var now = DateTime.UtcNow;

                return payload switch
                {
                    DebtorCreatedEvent e => new ActivityItem($"New debtor '{e.Name}' was added", "➕", now),
                    DebtorUpdatedEvent e => new ActivityItem($"Debtor '{e.Name}' was updated", "✏️", now),
                    DebtorDeletedEvent e => new ActivityItem($"Debtor '{e.Name}' was removed", "🗑️", now),

                    DebtCreatedEvent e => new ActivityItem($"Debt of {e.Amount:N2} ({e.Currency}) recorded for {e.DebtorName}", "💰", now),
                    DebtPaidEvent e => new ActivityItem($"Debt of {e.Amount:N2} ({e.Currency}) paid by {e.DebtorName}", "✅", now),
                    DebtDeletedEvent e => new ActivityItem(
                        string.IsNullOrWhiteSpace(e.DebtorName)
                            ? $"Debt of {e.Amount:N2} deleted"
                            : $"Debt of {e.Amount:N2} ({e.Currency}) deleted for {e.DebtorName}",
                        "🗑️",
                        now),

                    _ => new ActivityItem("Activity", "📌", now)
                };
            });
    }
}

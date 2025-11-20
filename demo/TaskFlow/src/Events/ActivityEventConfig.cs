using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Views;

namespace TaskFlow.Events;

public class ActivityEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        // Activity Logged
        config.When(ActivityEvents.Logged)
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            });
    }
}

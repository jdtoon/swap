using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Views;

namespace TaskFlow.Events;

public class CommentEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        // Comment Added
        config.When(CommentEvents.Added)
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            })
            .Toast("Comment added", ToastType.Info);

        // Comment Deleted
        config.When(CommentEvents.Deleted)
            .Toast("Comment deleted", ToastType.Info);
    }
}

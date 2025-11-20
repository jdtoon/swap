using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Views;

namespace TaskFlow.Events;

public class NotificationEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        // Task Assigned Notification
        config.When(NotificationEvents.TaskAssigned)
            .RefreshPartial(NotificationElements.Bell, NotificationViews.Bell, ctx =>
            {
                // In real app, would use current user ID from auth
                var notificationService = ctx.RequestServices.GetRequiredService<INotificationService>();
                return notificationService.GetUnreadCount("demo-user");
            });

        // Deadline Approaching Notification
        config.When(NotificationEvents.DeadlineApproaching)
            .RefreshPartial(NotificationElements.Bell, NotificationViews.Bell, ctx =>
            {
                var notificationService = ctx.RequestServices.GetRequiredService<INotificationService>();
                return notificationService.GetUnreadCount("demo-user");
            });
    }
}

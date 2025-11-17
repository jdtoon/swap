using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Views;
using TaskFlow.Models;

namespace TaskFlow.Events;

/// <summary>
/// Centralized HTTP event chain configuration for TaskFlow
/// Demonstrates advanced features including payload access, deep event chains, and SSE integration
/// </summary>
public static class EventChainConfiguration
{
    public static void ConfigureEventChains(SwapEventBusOptions config)
    {
        // ================================================================================
        // TASK EVENTS - Demonstrates payload access (NEW in 0.5.0)
        // ================================================================================

        // Task Created - Uses payload to avoid re-fetching data
        config.When(TaskEvents.Created)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                // Payload-aware factory - reuse task from event instead of re-fetching
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            })
            .Toast("Task created successfully", ToastType.Success);

        // Task Status Changed - Complex multi-update with payload
        config.When(TaskEvents.StatusChanged)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            })
            .Toast("Task status updated", ToastType.Info);

        // Task Assigned - Deep event chain with cascading triggers
        config.When(TaskEvents.Assigned)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            })
            .Toast("Task assigned successfully", ToastType.Success)
            .AlsoTrigger(NotificationEvents.TaskAssigned) // Cascade to notification
            .AlsoTrigger(ActivityEvents.Logged); // Cascade to activity log

        // Task Assignment Failed - Warning toast demonstration
        config.When(TaskEvents.AssignmentFailed)
            .Toast("Cannot assign task - team member is overloaded (10+ active tasks)", ToastType.Warning);

        // Task Conflict Detected - Warning toast
        config.When(TaskEvents.ConflictDetected)
            .Toast("Another user is editing this task", ToastType.Warning);

        // Task Completed - Multiple updates with payload
        config.When(TaskEvents.Completed)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Task completed! 🎉", ToastType.Success);

        // Task Overdue - Warning demonstration
        config.When(TaskEvents.Overdue)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Task is overdue!", ToastType.Warning)
            .AlsoTrigger(NotificationEvents.DeadlineApproaching);

        // Task Deleted - Delete swap mode demonstration
        config.When(TaskEvents.Deleted)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Task deleted", ToastType.Info);

        // ================================================================================
        // COMMENT EVENTS - Demonstrates swap modes (BeforeEnd for chronological order)
        // ================================================================================

        config.When(CommentEvents.Added)
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            })
            .Toast("Comment added", ToastType.Info);

        config.When(CommentEvents.Deleted)
            .Toast("Comment deleted", ToastType.Info);

        // ================================================================================
        // PROJECT EVENTS - Demonstrates progress updates
        // ================================================================================

        config.When(ProjectEvents.Created)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Project created", ToastType.Success);

        config.When(ProjectEvents.ProgressChanged)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Project progress updated", ToastType.Info);

        // ================================================================================
        // NOTIFICATION EVENTS - Demonstrates cascading and SSE integration
        // ================================================================================

        config.When(NotificationEvents.TaskAssigned)
            .RefreshPartial(NotificationElements.Bell, NotificationViews.Bell, ctx =>
            {
                // In real app, would use current user ID from auth
                var notificationService = ctx.RequestServices.GetRequiredService<INotificationService>();
                return notificationService.GetUnreadCount("demo-user");
            });

        config.When(NotificationEvents.DeadlineApproaching)
            .RefreshPartial(NotificationElements.Bell, NotificationViews.Bell, ctx =>
            {
                var notificationService = ctx.RequestServices.GetRequiredService<INotificationService>();
                return notificationService.GetUnreadCount("demo-user");
            });

        // ================================================================================
        // ACTIVITY EVENTS - Demonstrates activity logging
        // ================================================================================

        config.When(ActivityEvents.Logged)
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            });
    }
}

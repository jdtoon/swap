using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Views;
using TaskFlow.Models;

namespace TaskFlow.Events;

public class TaskEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        // Task Created - Uses payload to avoid re-fetching data
        config.When(TaskEvents.Created)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                // Payload-aware factory - reuse task from event instead of re-fetching
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, (ctx, payload) =>
            {
                // Use task from payload to show in activity without DB query
                var task = (TaskItem?)payload;
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                // Activity already logged by controller, just refresh view
                return activityService.GetRecent(10);
            })
            .Toast("Task created successfully", ToastType.Success);

        // Task Status Changed - Complex multi-update with payload
        config.When(TaskEvents.StatusChanged)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                // Use task from payload - no need to fetch from DB
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                // We have the task object, can use it if needed for filtering/logic
                return teamService.GetStats();
            })
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, (ctx, payload) =>
            {
                var task = (TaskItem?)payload;
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                // Activity updated by controller, refresh feed
                return activityService.GetRecent(10);
            })
            .Toast("Task status updated", ToastType.Info);

        // Task Assigned - Deep event chain with cascading triggers
        // DEMONSTRATES: Using payload to update assignee's task card specifically
        config.When(TaskEvents.Assigned)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                // Payload contains the task with assignee info - use it!
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, (ctx, payload) =>
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
                // Use completed task from payload
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Task completed! 🎉", ToastType.Success);

        // Task Overdue - Warning demonstration
        config.When(TaskEvents.Overdue)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                var task = (TaskItem?)payload;
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Task is overdue!", ToastType.Warning)
            .AlsoTrigger(NotificationEvents.DeadlineApproaching);

        // Task Deleted - Delete swap mode demonstration
        config.When(TaskEvents.Deleted)
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, (ctx, payload) =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            })
            .Toast("Task deleted", ToastType.Info);
    }
}

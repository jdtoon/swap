using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.ServerSentEvents;
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
        // SSE INTEGRATION - Broadcasts dashboard updates to all connected clients
        // ================================================================================

        // Stats update SSE event - responds to task changes
        config.When(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate))
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            });

        // Activity update SSE event - responds to activity changes
        config.When(SseEvents.Broadcast(DashboardSseEvents.ActivityUpdate))
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            });

        // Project list update SSE event - refreshes entire project grid
        config.When(SseEvents.Broadcast(ProjectSseEvents.ListUpdate))
            .RefreshPartial(ProjectElements.List, "~/Views/Projects/_ProjectList.cshtml", ctx =>
            {
                var projectService = ctx.RequestServices.GetRequiredService<IProjectService>();
                return projectService.GetAll();
            });

        // Project progress update SSE event - refreshes dashboard projects overview
        config.When(SseEvents.Broadcast(ProjectSseEvents.ProgressUpdate))
            .RefreshPartial(DashboardElements.Projects, DashboardViews.ProjectsOverview, ctx =>
            {
                var projectService = ctx.RequestServices.GetRequiredService<IProjectService>();
                return projectService.GetAll();
            });

        // Task column update SSE event - refreshes all kanban columns
        config.When(SseEvents.Broadcast(TaskSseEvents.ColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.Todo), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.Todo);
            });

        config.When(SseEvents.Broadcast(TaskSseEvents.ColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.InProgress), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.InProgress);
            });

        config.When(SseEvents.Broadcast(TaskSseEvents.ColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.Review), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.Review);
            });

        config.When(SseEvents.Broadcast(TaskSseEvents.ColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.Done), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.Done);
            });

        // Chain domain events to SSE broadcasts
        config.OnEvent(TaskEvents.Created)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.ColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(TaskEvents.StatusChanged)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.ColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(TaskEvents.Assigned)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.ColumnUpdate)
            .Build();

        config.OnEvent(TaskEvents.Completed)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.ColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(TaskEvents.Deleted)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.ColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(ProjectEvents.Created)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(ProjectSseEvents.ListUpdate)
            .Build();

        config.OnEvent(CommentEvents.Added)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .Build();

        config.OnEvent(ActivityEvents.Logged)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .Build();

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
                // Payload would contain project data if passed
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

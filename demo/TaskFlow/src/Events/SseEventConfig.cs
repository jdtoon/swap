using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using Swap.Htmx.Realtime;
using Swap.Htmx.Models;
using TaskFlow.Services;
using TaskFlow.Views;
using TaskFlow.Models;

namespace TaskFlow.Events;

public class SseEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        // Stats update SSE event - responds to task changes
        config.When(SseEvents.Broadcast(DashboardSseEvents.StatsUpdate))
            .RefreshPartial(DashboardElements.Stats, DashboardViews.Stats, ctx =>
            {
                var teamService = ctx.RequestServices.GetRequiredService<ITeamService>();
                return teamService.GetStats();
            }, SwapMode.InnerHTML);

        // Activity update SSE event - responds to activity changes
        config.When(SseEvents.Broadcast(DashboardSseEvents.ActivityUpdate))
            .RefreshPartial(DashboardElements.Activity, DashboardViews.Activity, ctx =>
            {
                var activityService = ctx.RequestServices.GetRequiredService<IActivityService>();
                return activityService.GetRecent(10);
            }, SwapMode.InnerHTML);

        // Project list update SSE event - refreshes entire project grid
        config.When(SseEvents.Broadcast(ProjectSseEvents.ListUpdate))
            .RefreshPartial(ProjectElements.List, "~/Views/Projects/_ProjectList.cshtml", ctx =>
            {
                var projectService = ctx.RequestServices.GetRequiredService<IProjectService>();
                return projectService.GetAll();
            }, SwapMode.InnerHTML);

        // Project progress update SSE event - refreshes dashboard projects overview
        config.When(SseEvents.Broadcast(ProjectSseEvents.ProgressUpdate))
            .RefreshPartial(DashboardElements.Projects, DashboardViews.ProjectsOverview, ctx =>
            {
                var projectService = ctx.RequestServices.GetRequiredService<IProjectService>();
                return projectService.GetAll();
            }, SwapMode.InnerHTML);

        // Task column update SSE events - separate event for each column
        config.When(SseEvents.Broadcast(TaskSseEvents.TodoColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.Todo), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.Todo);
            }, SwapMode.InnerHTML);

        config.When(SseEvents.Broadcast(TaskSseEvents.InProgressColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.InProgress), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.InProgress);
            }, SwapMode.InnerHTML);

        config.When(SseEvents.Broadcast(TaskSseEvents.ReviewColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.Review), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.Review);
            }, SwapMode.InnerHTML);

        config.When(SseEvents.Broadcast(TaskSseEvents.DoneColumnUpdate))
            .RefreshPartial(TaskElements.Column(Models.TaskStatus.Done), "~/Views/Tasks/TaskColumn.cshtml", ctx =>
            {
                var taskService = ctx.RequestServices.GetRequiredService<ITaskService>();
                return taskService.GetByStatus(Models.TaskStatus.Done);
            }, SwapMode.InnerHTML);

        // Chain domain events to SSE broadcasts
        config.OnEvent(TaskEvents.Created)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.TodoColumnUpdate)
            .BroadcastSse(TaskSseEvents.InProgressColumnUpdate)
            .BroadcastSse(TaskSseEvents.ReviewColumnUpdate)
            .BroadcastSse(TaskSseEvents.DoneColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(TaskEvents.StatusChanged)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.TodoColumnUpdate)
            .BroadcastSse(TaskSseEvents.InProgressColumnUpdate)
            .BroadcastSse(TaskSseEvents.ReviewColumnUpdate)
            .BroadcastSse(TaskSseEvents.DoneColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(TaskEvents.Assigned)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.TodoColumnUpdate)
            .BroadcastSse(TaskSseEvents.InProgressColumnUpdate)
            .BroadcastSse(TaskSseEvents.ReviewColumnUpdate)
            .BroadcastSse(TaskSseEvents.DoneColumnUpdate)
            .Build();

        config.OnEvent(TaskEvents.Completed)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.TodoColumnUpdate)
            .BroadcastSse(TaskSseEvents.InProgressColumnUpdate)
            .BroadcastSse(TaskSseEvents.ReviewColumnUpdate)
            .BroadcastSse(TaskSseEvents.DoneColumnUpdate)
            .BroadcastSse(ProjectSseEvents.ProgressUpdate)
            .Build();

        config.OnEvent(TaskEvents.Deleted)
            .BroadcastSse(DashboardSseEvents.StatsUpdate)
            .BroadcastSse(DashboardSseEvents.ActivityUpdate)
            .BroadcastSse(TaskSseEvents.TodoColumnUpdate)
            .BroadcastSse(TaskSseEvents.InProgressColumnUpdate)
            .BroadcastSse(TaskSseEvents.ReviewColumnUpdate)
            .BroadcastSse(TaskSseEvents.DoneColumnUpdate)
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
    }
}

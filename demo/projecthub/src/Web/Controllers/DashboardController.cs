using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Workspaces.Contracts;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using Swap.Htmx;
using TaskStatus = ProjectHub.Modules.Tasks.Contracts.TaskStatus;

namespace ProjectHub.Web.Controllers;

[Route("dashboard")]
public class DashboardController : SwapController
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    public DashboardController(
        IWorkspaceService workspaceService,
        IProjectService projectService,
        ITaskService taskService)
    {
        _workspaceService = workspaceService;
        _projectService = projectService;
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var stats = await GetDashboardStatsAsync();
        return SwapView(stats);
    }

    /// <summary>
    /// HTMX polling endpoint for dashboard metrics partial
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics()
    {
        var stats = await GetDashboardStatsAsync();
        return PartialView("_DashboardMetrics", stats);
    }

    /// <summary>
    /// Server-Sent Events endpoint for live dashboard metrics
    /// </summary>
    [HttpGet("live")]
    public IActionResult LiveMetrics()
    {
        return ServerSentEvents(async (stream, ct) =>
        {
            // Send initial state
            var initialStats = await GetDashboardStatsAsync();
            var initialHtml = await this.RenderPartialToStringAsync("_DashboardMetrics", initialStats);
            await stream.SendEventAsync("metrics-update", initialHtml);

            // Stream updates every 3 seconds
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(3000, ct);
                
                var stats = await GetDashboardStatsAsync();
                var html = await this.RenderPartialToStringAsync("_DashboardMetrics", stats);
                await stream.SendEventAsync("metrics-update", html);
            }
        });
    }

    /// <summary>
    /// Server-Sent Events endpoint for recent activity feed
    /// </summary>
    [HttpGet("activity")]
    public IActionResult LiveActivity()
    {
        return ServerSentEvents(async (stream, ct) =>
        {
            var random = new Random();
            
            // Send initial activity
            await stream.SendEventAsync("activity", 
                "<div class=\"notification is-info is-light\">📊 Dashboard connected</div>");

            // Simulate activity updates every 5 seconds
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);

                var activities = new[]
                {
                    "✅ Task \"Implement authentication\" completed",
                    "📝 New task \"Fix login bug\" created",
                    "🚀 Project \"Mobile App\" status changed to In Progress",
                    "💬 Comment added to \"Design homepage\"",
                    "📊 Workspace \"Marketing\" created",
                    "⏰ Task \"Review PR #42\" moved to In Progress"
                };

                var activity = activities[random.Next(activities.Length)];
                var html = $@"
                    <div class=""notification is-light"">
                        <p class=""is-size-7"">{activity}</p>
                        <p class=""has-text-grey is-size-7"">Just now</p>
                    </div>";

                await stream.SendEventAsync("activity", html);
            }
        });
    }

    private async Task<DashboardStatsViewModel> GetDashboardStatsAsync()
    {
        var workspaces = await _workspaceService.GetAllAsync();
        var projects = await _projectService.GetAllAsync();
        var tasks = await _taskService.GetAllAsync();

        return new DashboardStatsViewModel
        {
            WorkspaceCount = workspaces.Count(),
            ProjectCount = projects.Count(),
            TaskCount = tasks.Count(),
            CompletedTaskCount = tasks.Count(t => t.Status == TaskStatus.Done),
            InProgressTaskCount = tasks.Count(t => t.Status == TaskStatus.InProgress),
            TodoTaskCount = tasks.Count(t => t.Status == TaskStatus.Todo),
            BacklogTaskCount = tasks.Count(t => t.Status == TaskStatus.Backlog),
            ReviewTaskCount = tasks.Count(t => t.Status == TaskStatus.Review),
            Timestamp = DateTime.Now
        };
    }
}

public record DashboardStatsViewModel
{
    public int WorkspaceCount { get; init; }
    public int ProjectCount { get; init; }
    public int TaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
    public int InProgressTaskCount { get; init; }
    public int TodoTaskCount { get; init; }
    public int BacklogTaskCount { get; init; }
    public int ReviewTaskCount { get; init; }
    public DateTime Timestamp { get; init; }

    public decimal CompletionPercentage => TaskCount > 0 
        ? Math.Round((decimal)CompletedTaskCount / TaskCount * 100, 1) 
        : 0;
}

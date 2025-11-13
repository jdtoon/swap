using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Workspaces.Contracts;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using Swap.Htmx;
using Swap.Htmx.ServerSentEvents;
using TaskStatus = ProjectHub.Modules.Tasks.Contracts.TaskStatus;

namespace ProjectHub.Web.Controllers;

[Route("dashboard")]
public class EnhancedDashboardController : SwapController
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly ISseConnectionRegistry _connectionRegistry;
    private readonly ISseFallbackService _fallbackService;

    public EnhancedDashboardController(
        IWorkspaceService workspaceService,
        IProjectService projectService,
        ITaskService taskService,
        ISseConnectionRegistry connectionRegistry,
        ISseFallbackService fallbackService)
    {
        _workspaceService = workspaceService;
        _projectService = projectService;
        _taskService = taskService;
        _connectionRegistry = connectionRegistry;
        _fallbackService = fallbackService;
    }

    [HttpGet("enhanced")]
    public async Task<IActionResult> Enhanced()
    {
        var stats = await GetDashboardStatsAsync();
        return SwapView("Enhanced", stats);
    }

    /// <summary>
    /// Enhanced SSE endpoint with automatic event-driven updates and fallback support
    /// </summary>
    [HttpGet("enhanced/live")]
    public async Task<IActionResult> EnhancedLiveMetrics()
    {
        // Check if fallback should be used
        if (_fallbackService.ShouldUsePolling(HttpContext))
        {
            return await this.CachedPollingFallback(
                cacheKey: "dashboard-metrics",
                getContentFunc: async () =>
                {
                    var stats = await GetDashboardStatsAsync();
                    return await this.RenderPartialToStringAsync("_EnhancedDashboardMetrics", stats);
                },
                cacheDuration: TimeSpan.FromSeconds(10)
            );
        }

        // Enhanced SSE with connection management
        return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) =>
        {
            var connection = connectionBuilder.Connection;

            // Join dashboard room for targeted updates
            connectionBuilder.WithRooms("dashboard", "metrics")
                           .WithEventPrefix("ui.dashboard")
                           .WithEventPrefix("ui.stats");

            // Send initial state
            var initialStats = await GetDashboardStatsAsync();
            var initialHtml = await this.RenderPartialToStringAsync("_EnhancedDashboardMetrics", initialStats);
            await connection.SendEventAsync("metrics-update", initialHtml);

            // Keep connection alive with heartbeat
            await connectionBuilder.KeepAlive(TimeSpan.FromSeconds(30), cancellationToken);
        });
    }

    /// <summary>
    /// Real-time notifications with cross-module event integration
    /// </summary>
    [HttpGet("enhanced/notifications")]
    public async Task<IActionResult> EnhancedNotifications()
    {
        if (_fallbackService.ShouldUsePolling(HttpContext))
        {
            return await this.JsonPollingFallback(async lastEventId =>
            {
                // In a real app, you'd query recent notifications from a database
                // For demo, simulate notifications
                var hasNewNotification = Random.Shared.Next(0, 4) == 0;

                if (hasNewNotification)
                {
                    var notifications = new[]
                    {
                        new { type = "project", message = "New project 'Mobile App Redesign' created", icon = "🚀" },
                        new { type = "task", message = "Task 'Fix critical bug' marked as complete", icon = "✅" },
                        new { type = "workspace", message = "Workspace 'Q1 Planning' archived", icon = "📁" },
                        new { type = "system", message = "System maintenance scheduled for tonight", icon = "⚙️" }
                    };

                    var notification = notifications[Random.Shared.Next(notifications.Length)];
                    return new
                    {
                        notification,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                }

                return null; // No new notifications
            });
        }

        // Enhanced SSE for notifications
        return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) =>
        {
            var connection = connectionBuilder.Connection;

            // Subscribe to all notification events
            connectionBuilder.WithRooms("notifications", "global")
                           .WithEventPrefix("notify");

            // Send welcome notification
            var welcomeHtml = @"
                <div class=""notification is-info is-light mb-3"" id=""notification-welcome"">
                    <button class=""delete"" onclick=""this.parentElement.remove()""></button>
                    <p><span class=""icon"">🔔</span> <strong>Connected to live notifications</strong></p>
                    <p class=""is-size-7 has-text-grey"">You'll receive real-time updates from all modules</p>
                </div>";

            await connection.SendEventAsync("notification", welcomeHtml);

            // Keep connection alive and listen for cross-module events
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(30000, cancellationToken); // 30 second heartbeat

                // Simulate periodic notifications (in real app, these would come from events)
                if (Random.Shared.Next(0, 6) == 0) // 1 in 6 chance every 30 seconds
                {
                    var notification = GenerateRandomNotification();
                    await connection.SendEventAsync("notification", notification);
                }
            }
        });
    }

    /// <summary>
    /// Cross-module activity feed with event-driven updates
    /// </summary>
    [HttpGet("enhanced/activity")]
    public IActionResult EnhancedActivity()
    {
        return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) =>
        {
            var connection = connectionBuilder.Connection;

            // Subscribe to all cross-module events for activity feed
            connectionBuilder.WithRooms("activity", "global")
                           .WithEventPattern("*.created")
                           .WithEventPattern("*.updated")
                           .WithEventPattern("*.deleted")
                           .WithEventPattern("*.archived");

            // Send initial activity message
            var initialHtml = @"
                <div class=""box has-background-light mb-3"">
                    <p class=""has-text-centered has-text-grey"">
                        <span class=""icon is-large""><i class=""fas fa-satellite-dish""></i></span><br>
                        <strong>Live Activity Feed Connected</strong><br>
                        <small>Monitoring all module activities in real-time</small>
                    </p>
                </div>";

            await connection.SendEventAsync("activity-update", initialHtml);

            // Keep alive and simulate cross-module activities
            var activityTemplates = new[]
            {
                (icon: "🏢", message: "Workspace '{0}' was created in Workspaces module", module: "workspaces"),
                (icon: "🚀", message: "Project '{0}' started in Projects module", module: "projects"),
                (icon: "📝", message: "Task '{0}' assigned in Tasks module", module: "tasks"),
                (icon: "✅", message: "Task '{0}' completed in Tasks module", module: "tasks"),
                (icon: "🔄", message: "Project '{0}' status updated in Projects module", module: "projects"),
                (icon: "📊", message: "Workspace '{0}' metrics updated in Workspaces module", module: "workspaces")
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(8000, cancellationToken); // Activity every 8 seconds

                var template = activityTemplates[Random.Shared.Next(activityTemplates.Length)];
                var names = new[] { "Mobile App", "Website Redesign", "API Integration", "User Authentication", "Dashboard Cleanup", "Bug Fixes" };
                var name = names[Random.Shared.Next(names.Length)];

                var activityHtml = $@"
                    <div class=""notification is-light mb-2"" style=""border-left: 4px solid hsl(204, 86%, 53%); animation: fadeIn 0.3s ease-in;"">
                        <div class=""level is-mobile"">
                            <div class=""level-left"">
                                <div class=""level-item"">
                                    <span class=""icon"" style=""font-size: 1.2rem;"">{template.icon}</span>
                                </div>
                                <div class=""level-item"">
                                    <div>
                                        <p class=""is-size-7"">{string.Format(template.message, name)}</p>
                                        <p class=""has-text-grey is-size-7"">
                                            <span class=""tag is-small is-light"">{template.module}</span>
                                            <span>{DateTime.Now:HH:mm:ss}</span>
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>";

                await connection.SendEventAsync("activity-update", activityHtml);
            }
        });
    }

    /// <summary>
    /// Demonstrates manual SSE broadcasting for cross-module communication
    /// </summary>
    [HttpPost("enhanced/broadcast")]
    public async Task<IActionResult> BroadcastMessage([FromForm] string message, [FromForm] string type = "info")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest("Message cannot be empty");
        }

        // Broadcast to all dashboard connections
        var notificationHtml = $@"
            <div class=""notification is-{type} is-light mb-3"" id=""notification-{Guid.NewGuid():N}"">
                <button class=""delete"" onclick=""this.parentElement.remove()""></button>
                <p><span class=""icon"">📢</span> <strong>Manual Broadcast:</strong> {message}</p>
                <p class=""is-size-7 has-text-grey"">Sent at {DateTime.Now:HH:mm:ss}</p>
            </div>";

        await _connectionRegistry.BroadcastToRoomsAsync("notification", notificationHtml, new[] { "notifications", "global" });

        // Return success message for the sender
        return PartialView("_BroadcastSuccess", message);
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

    private string GenerateRandomNotification()
    {
        var notifications = new[]
        {
            (icon: "🚀", message: "New project milestone reached", type: "success"),
            (icon: "⚠️", message: "System backup starting in 10 minutes", type: "warning"),
            (icon: "✅", message: "Weekly report has been generated", type: "info"),
            (icon: "👤", message: "New team member joined the workspace", type: "info"),
            (icon: "🔄", message: "Automatic data sync completed", type: "success"),
            (icon: "📊", message: "Monthly analytics are now available", type: "info")
        };

        var notification = notifications[Random.Shared.Next(notifications.Length)];
        var id = Guid.NewGuid().ToString("N");

        return $@"
            <div class=""notification is-{notification.type} is-light mb-3"" id=""notification-{id}"">
                <button class=""delete"" onclick=""this.parentElement.remove()""></button>
                <p><span class=""icon"">{notification.icon}</span> <strong>{notification.message}</strong></p>
                <p class=""is-size-7 has-text-grey"">Just now</p>
            </div>";
    }
}
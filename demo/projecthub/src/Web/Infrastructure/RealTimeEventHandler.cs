using Swap.Htmx.Events;
using Swap.Htmx.ServerSentEvents;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using ProjectHub.Modules.Workspaces.Contracts;

namespace ProjectHub.Web.Infrastructure;

/// <summary>
/// Service that converts domain events to SSE broadcasts.
/// This demonstrates event-driven real-time updates across modules.
/// In a real implementation, this would be called by an event dispatcher or domain event handler.
/// </summary>
public class RealTimeEventHandler
{
    private readonly ISseConnectionRegistry _connectionRegistry;
    private readonly ILogger<RealTimeEventHandler> _logger;

    public RealTimeEventHandler(ISseConnectionRegistry connectionRegistry, ILogger<RealTimeEventHandler> logger)
    {
        _connectionRegistry = connectionRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Handles a domain event and broadcasts appropriate SSE updates.
    /// Call this method when domain events occur to trigger real-time updates.
    /// </summary>
    public async Task HandleEventAsync(EventKey eventKey, object payload)
    {
        try
        {
            // Dashboard metrics updates
            if (ShouldUpdateDashboard(eventKey))
            {
                await BroadcastDashboardRefresh();
            }

            // Activity feed updates
            var activityHtml = GenerateActivityHtml(eventKey, payload);
            if (!string.IsNullOrEmpty(activityHtml))
            {
                await _connectionRegistry.BroadcastToRoomsAsync("activity-update", activityHtml, new[] { "activity", "global" });
            }

            // Notification updates
            var notificationHtml = GenerateNotificationHtml(eventKey, payload);
            if (!string.IsNullOrEmpty(notificationHtml))
            {
                await _connectionRegistry.BroadcastToRoomsAsync("notification", notificationHtml, new[] { "notifications", "global" });
            }

            _logger.LogDebug("Processed event {EventKey} for SSE broadcasting", eventKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventKey} for SSE broadcasting", eventKey);
        }
    }

    private bool ShouldUpdateDashboard(EventKey eventKey)
    {
        return eventKey.Name.Contains("created") ||
               eventKey.Name.Contains("deleted") ||
               eventKey.Name.Contains("archived") ||
               eventKey.Name.Contains("statusChanged");
    }

    private async Task BroadcastDashboardRefresh()
    {
        // Signal dashboard to refresh metrics
        var refreshHtml = @"
            <script>
                // Trigger a refresh of the dashboard metrics
                htmx.trigger('#dashboard-metrics', 'refresh');
            </script>";

        await _connectionRegistry.BroadcastToRoomsAsync("dashboard-refresh", refreshHtml, new[] { "dashboard", "metrics" });
    }

    private string GenerateActivityHtml(EventKey eventKey, object payload)
    {
        var (icon, message, module) = eventKey.Name switch
        {
            var key when key == ProjectEvents.Created.Name =>
                ExtractProjectActivity("🚀", "Project '{0}' was created", "projects", payload),
            var key when key == ProjectEvents.Updated.Name =>
                ExtractProjectActivity("🔄", "Project '{0}' was updated", "projects", payload),
            var key when key == ProjectEvents.Archived.Name =>
                ExtractProjectActivity("📦", "Project '{0}' was archived", "projects", payload),
            var key when key == ProjectEvents.StatusChanged.Name =>
                ExtractProjectStatusActivity(payload),
            var key when key.Contains("task.created") =>
                ExtractTaskActivity("📝", "Task '{0}' was created", "tasks", payload),
            var key when key.Contains("task.completed") =>
                ExtractTaskActivity("✅", "Task '{0}' was completed", "tasks", payload),
            var key when key.Contains("workspace.created") =>
                ExtractWorkspaceActivity("🏢", "Workspace '{0}' was created", "workspaces", payload),
            _ => ("", "", "")
        };

        if (string.IsNullOrEmpty(message)) return "";

        var id = Guid.NewGuid().ToString("N");
        return $@"
            <div class=""notification is-light mb-2"" id=""activity-{id}"" style=""border-left: 4px solid hsl(204, 86%, 53%); animation: fadeIn 0.3s ease-in;"">
                <div class=""level is-mobile"">
                    <div class=""level-left"">
                        <div class=""level-item"">
                            <span class=""icon"" style=""font-size: 1.2rem;"">{icon}</span>
                        </div>
                        <div class=""level-item"">
                            <div>
                                <p class=""is-size-7"">{message}</p>
                                <p class=""has-text-grey is-size-7"">
                                    <span class=""tag is-small is-light"">{module}</span>
                                    <span class=""tag is-small is-success"">live</span>
                                    <span>{DateTime.Now:HH:mm:ss}</span>
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>";
    }

    private string GenerateNotificationHtml(EventKey eventKey, object payload)
    {
        var (icon, message, type) = eventKey.Name switch
        {
            var key when key == ProjectEvents.Created.Name =>
                ("🚀", ExtractProjectName(payload, "New project '{0}' has been created"), "success"),
            var key when key.Contains("task.completed") =>
                ("✅", ExtractTaskName(payload, "Task '{0}' has been completed"), "success"),
            var key when key.Contains("workspace.created") =>
                ("🏢", ExtractWorkspaceName(payload, "New workspace '{0}' is available"), "info"),
            _ => ("", "", "")
        };

        if (string.IsNullOrEmpty(message)) return "";

        var id = Guid.NewGuid().ToString("N");
        return $@"
            <div class=""notification is-{type} is-light mb-3"" id=""notification-{id}"">
                <button class=""delete"" onclick=""this.parentElement.remove()""></button>
                <p><span class=""icon"">{icon}</span> <strong>{message}</strong></p>
                <p class=""is-size-7 has-text-grey"">
                    <span class=""tag is-small is-success"">live event</span>
                    Just now
                </p>
            </div>";
    }

    private (string icon, string message, string module) ExtractProjectActivity(string icon, string template, string module, object payload)
    {
        var name = payload switch
        {
            ProjectEventPayloads.Created created => created.Name,
            ProjectEventPayloads.Updated updated => updated.Name,
            ProjectEventPayloads.Archived archived => archived.Name,
            _ => "Unknown Project"
        };

        return (icon, string.Format(template, name), module);
    }

    private (string icon, string message, string module) ExtractProjectStatusActivity(object payload)
    {
        if (payload is ProjectEventPayloads.StatusChanged statusChanged)
        {
            return ("🔄", $"Project status changed from {statusChanged.OldStatus} to {statusChanged.NewStatus}", "projects");
        }

        return ("", "", "");
    }

    private (string icon, string message, string module) ExtractTaskActivity(string icon, string template, string module, object payload)
    {
        // Note: In a real implementation, you'd extract task name from payload
        // For now, using a placeholder since task events structure would need to be defined
        return (icon, string.Format(template, "Task"), module);
    }

    private (string icon, string message, string module) ExtractWorkspaceActivity(string icon, string template, string module, object payload)
    {
        // Note: Similarly, workspace event payload structure would need to be defined
        return (icon, string.Format(template, "Workspace"), module);
    }

    private string ExtractProjectName(object payload, string template)
    {
        var name = payload switch
        {
            ProjectEventPayloads.Created created => created.Name,
            _ => "Project"
        };

        return string.Format(template, name);
    }

    private string ExtractTaskName(object payload, string template)
    {
        // Placeholder for task name extraction
        return string.Format(template, "Task");
    }

    private string ExtractWorkspaceName(object payload, string template)
    {
        // Placeholder for workspace name extraction
        return string.Format(template, "Workspace");
    }
}
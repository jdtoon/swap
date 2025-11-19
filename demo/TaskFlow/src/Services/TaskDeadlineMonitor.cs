using TaskFlow.Events;
using TaskFlow.Services;

namespace TaskFlow.Services;

/// <summary>
/// Background service that checks for overdue tasks and triggers warning events
/// Demonstrates scheduled warning toast triggers
/// </summary>
public class TaskDeadlineMonitor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskDeadlineMonitor> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public TaskDeadlineMonitor(
        IServiceProvider serviceProvider,
        ILogger<TaskDeadlineMonitor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Deadline Monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                // Check for overdue tasks
                var overdueTasks = taskService.GetOverdue();
                
                if (overdueTasks.Any())
                {
                    _logger.LogWarning("Found {Count} overdue tasks", overdueTasks.Count);
                    
                    // In a real app with SSE, we would:
                    // 1. Trigger TaskEvents.Overdue for each task
                    // 2. Push via SSE to connected clients
                    // For now, this demonstrates the monitoring capability
                }

                // Check for tasks due within 24 hours
                var now = DateTime.UtcNow;
                var allTasks = taskService.GetAll();
                var upcomingDeadlines = allTasks
                    .Where(t => t.DueDate.HasValue && 
                               t.DueDate.Value > now && 
                               t.DueDate.Value <= now.AddHours(24) &&
                               t.Status != Models.TaskStatus.Done)
                    .ToList();

                if (upcomingDeadlines.Any())
                {
                    _logger.LogInformation("Found {Count} tasks with deadlines in next 24h", upcomingDeadlines.Count);
                    
                    // Trigger deadline approaching warnings
                    // In real app: push via SSE to relevant users
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking task deadlines");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Task Deadline Monitor stopped");
    }
}

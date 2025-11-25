using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;
using SwapPhase15.Events;

namespace SwapPhase15.Handlers;

// Handler 1: Responsible for the Task List UI
// It removes the completed item from the DOM.
[SwapHandler]
public class TaskListHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken)
    {
        // OOB Swap: Delete the row with id="task-{id}"
        builder.AlsoUpdate($"task-{@event.TaskId}", "", null, SwapMode.Delete);
        return Task.CompletedTask;
    }
}

// Handler 2: Responsible for the Stats Widget
// It updates the progress bar and counters.
[SwapHandler]
public class TaskStatsHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken)
    {
        // Update the badge
        builder.AlsoUpdate("task-count-badge", "_TaskCountBadge", @event.RemainingTasks);

        // Update the progress bar
        // We reconstruct the view model needed for the partial
        var statsModel = new SwapPhase15.Controllers.TaskBoardViewModel
        {
            CompletedCount = @event.TotalTasks - @event.RemainingTasks,
            TotalTasks = @event.TotalTasks,
            // Tasks list is not needed for the stats partial
        };
        
        builder.AlsoUpdate("project-stats", "_Stats", statsModel);
        
        return Task.CompletedTask;
    }
}

// Handler 3: Responsible for the Activity Log
// It appends a new entry to the sidebar.
[SwapHandler]
public class ActivityLogHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken)
    {
        var logEntry = new LogEntry 
        { 
            Message = $"Completed '{@event.TaskTitle}'", 
            Timestamp = DateTime.Now.ToString("HH:mm:ss") 
        };

        // OOB Swap: Insert AfterBegin of the log container
        builder.AlsoUpdate("activity-log-list", "_LogEntry", logEntry, SwapMode.AfterBegin);
        return Task.CompletedTask;
    }
}

// Handler 4: Gamification / Notifications
// Shows a toast celebration.
[SwapHandler]
public class CelebrationHandler : ISwapEventHandler<TaskCompletedEvent>
{
    public Task HandleAsync(TaskCompletedEvent @event, SwapResponseBuilder builder, CancellationToken cancellationToken)
    {
        if (@event.RemainingTasks == 0)
        {
            builder.WithSuccessToast("All tasks completed! Great job!");
            builder.WithClientAction("confetti", "body"); // Imaginary client action
        }
        else
        {
            builder.WithSuccessToast("Task completed.");
        }
        return Task.CompletedTask;
    }
}

public class LogEntry
{
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

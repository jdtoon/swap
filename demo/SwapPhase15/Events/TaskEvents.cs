using Swap.Htmx.Attributes;
using Swap.Htmx.Events;

namespace SwapPhase15.Events;

[SwapEventSource]
public partial class TaskEvents
{
    public const string TaskCompleted = "task.completed";
}

public class TaskCompletedEvent
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string CompletedBy { get; set; } = "User";
    public int RemainingTasks { get; set; }
    public int TotalTasks { get; set; }
}

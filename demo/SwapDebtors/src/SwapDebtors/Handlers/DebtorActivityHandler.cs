using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace SwapDebtors.Handlers;

/// <summary>
/// Handler that logs activity to the SSE feed when debtors change.
/// Uses [SwapHandler] attribute to auto-register with DI.
/// </summary>
[SwapHandler]
public class DebtorActivityHandler :
    ISwapEventHandler<DebtorCreatedEvent>,
    ISwapEventHandler<DebtorUpdatedEvent>,
    ISwapEventHandler<DebtorDeletedEvent>
{
    public Task HandleAsync(DebtorCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-item", "Dashboard/_ActivityItem", new ActivityItem
        {
            Message = $"New debtor '{e.Name}' was added",
            Icon = "➕",
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task HandleAsync(DebtorUpdatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-item", "Dashboard/_ActivityItem", new ActivityItem
        {
            Message = $"Debtor '{e.Name}' was updated",
            Icon = "✏️",
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task HandleAsync(DebtorDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-item", "Dashboard/_ActivityItem", new ActivityItem
        {
            Message = $"Debtor '{e.Name}' was removed",
            Icon = "🗑️",
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }
}

/// <summary>
/// Model for activity feed items
/// </summary>
public record ActivityItem
{
    public string Message { get; init; } = string.Empty;
    public string Icon { get; init; } = "📌";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// Event types for debtors
public class DebtorCreatedEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DebtorUpdatedEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DebtorDeletedEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}


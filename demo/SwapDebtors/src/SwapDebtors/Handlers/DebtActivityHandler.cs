using Swap.Htmx.Attributes;
using Swap.Htmx.Events;
using Swap.Htmx.Models;

namespace SwapDebtors.Handlers;

/// <summary>
/// Handler that logs activity to the SSE feed when debts change.
/// </summary>
[SwapHandler]
public class DebtActivityHandler :
    ISwapEventHandler<DebtCreatedEvent>,
    ISwapEventHandler<DebtPaidEvent>,
    ISwapEventHandler<DebtDeletedEvent>
{
    public Task HandleAsync(DebtCreatedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-item", "Dashboard/_ActivityItem", new ActivityItem
        {
            Message = $"Debt of {e.Amount:C} ({e.Currency}) recorded for {e.DebtorName}",
            Icon = "💰",
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task HandleAsync(DebtPaidEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-item", "Dashboard/_ActivityItem", new ActivityItem
        {
            Message = $"Debt of {e.Amount:C} ({e.Currency}) paid by {e.DebtorName}",
            Icon = "✅",
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    public Task HandleAsync(DebtDeletedEvent e, SwapResponseBuilder builder, CancellationToken ct)
    {
        builder.AlsoUpdate("activity-item", "Dashboard/_ActivityItem", new ActivityItem
        {
            Message = $"Debt of {e.Amount:C} deleted",
            Icon = "🗑️",
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }
}

// Event types for debts
public class DebtCreatedEvent
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string DebtorName { get; set; } = string.Empty;
}

public class DebtPaidEvent
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string DebtorName { get; set; } = string.Empty;
}

public class DebtDeletedEvent
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}


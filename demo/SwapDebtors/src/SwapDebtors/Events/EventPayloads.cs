namespace SwapDebtors.Events;

// Event payload types used by SwapEventAsync() calls.
// These are POCOs so they can be serialized and routed through Swap.Htmx.

public sealed class DebtorCreatedEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class DebtorUpdatedEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class DebtorDeletedEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class DebtCreatedEvent
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string DebtorName { get; set; } = string.Empty;
}

public sealed class DebtPaidEvent
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string DebtorName { get; set; } = string.Empty;
}

public sealed class DebtDeletedEvent
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? DebtorName { get; set; }
}

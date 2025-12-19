using Swap.Htmx.Attributes;

namespace SwapDebtors.Events;

/// <summary>
/// Event keys for debt-related actions.
/// "debt.created" becomes DebtEvents.Debt.Created
/// </summary>
[SwapEventSource]
public partial class DebtEvents
{
    public const string DebtCreated = "debt.created";
    public const string DebtUpdated = "debt.updated";
    public const string DebtDeleted = "debt.deleted";
    public const string DebtPaid = "debt.paid";
}

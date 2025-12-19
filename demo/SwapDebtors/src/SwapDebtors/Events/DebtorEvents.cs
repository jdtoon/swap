using Swap.Htmx.Attributes;

namespace SwapDebtors.Events;

/// <summary>
/// Event keys for debtor-related actions.
/// The [SwapEventSource] attribute generates EventKey properties:
/// "debtor.created" becomes DebtorEvents.Debtor.Created
/// </summary>
[SwapEventSource]
public partial class DebtorEvents
{
    public const string DebtorCreated = "debtor.created";
    public const string DebtorUpdated = "debtor.updated";
    public const string DebtorDeleted = "debtor.deleted";
    public const string DebtorSelected = "debtor.selected";
}

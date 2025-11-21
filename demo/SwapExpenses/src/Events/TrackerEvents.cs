using Swap.Htmx.Attributes;

namespace SwapExpenses.Events;

[SwapEventSource]
public partial class TrackerEvents
{
    public const string ExpenseAdded = "expense.added";
    public const string ExpenseDeleted = "expense.deleted";
    public const string TotalUpdated = "total.updated";
}

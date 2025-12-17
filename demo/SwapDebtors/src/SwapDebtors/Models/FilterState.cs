using Swap.Htmx.State;

namespace SwapDebtors.Models;

/// <summary>
/// SwapState for debtor list filtering and pagination.
/// Persists filter/sort/page state across HTMX requests.
/// </summary>
public class DebtorFilterState : SwapState
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
    public bool ShowPaidDebts { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
}

/// <summary>
/// SwapState for debt list filtering and pagination.
/// </summary>
public class DebtFilterState : SwapState
{
    public string? Search { get; set; }
    public string Currency { get; set; } = "all";
    public string Status { get; set; } = "all"; // all, paid, unpaid
    public string SortBy { get; set; } = "date";
    public bool SortDesc { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// View model for paginated debtor list
/// </summary>
public class DebtorListViewModel
{
    public required DebtorFilterState State { get; init; }
    public required List<Debtor> Debtors { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)State.PageSize);
}

/// <summary>
/// View model for paginated debt list
/// </summary>
public class DebtListViewModel
{
    public required DebtFilterState State { get; init; }
    public required List<Debt> Debts { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)State.PageSize);
}

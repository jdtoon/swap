using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using Swap.Htmx.State;
using SwapDebtors.Data;
using SwapDebtors.Events;
using SwapDebtors.Models;
using SwapDebtors.Services;

namespace SwapDebtors.Controllers;

/// <summary>
/// Debts controller - CRUD with SwapState for filtering/pagination.
/// Demonstrates multi-currency support and various UX patterns.
/// </summary>
public class DebtsController : SwapController
{
    private readonly DebtorsDbContext _db;
    private readonly ICurrencyService _currency;

    public DebtsController(DebtorsDbContext db, ICurrencyService currency)
    {
        _db = db;
        _currency = currency;
    }

    /// <summary>
    /// List all debts with SwapState filtering/pagination
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var state = new DebtFilterState();
        var (debts, totalCount) = await FilterDebtsAsync(state);

        ViewData["Currencies"] = _currency.GetSupportedCurrencies();

        return SwapView(new DebtListViewModel
        {
            State = state,
            Debts = debts,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Filter debts - called via HTMX with SwapState
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Filter([FromSwapState] DebtFilterState state)
    {
        Console.WriteLine($"[Debts/Filter] Currency={state.Currency}, Status={state.Status}, Page={state.Page}, SortBy={state.SortBy}");

        var (debts, totalCount) = await FilterDebtsAsync(state);

        ViewData["Currencies"] = _currency.GetSupportedCurrencies();

        return PartialView("_FilterContent", new DebtListViewModel
        {
            State = state,
            Debts = debts,
            TotalCount = totalCount
        });
    }

    private async Task<(List<Debt> Items, int TotalCount)> FilterDebtsAsync(DebtFilterState state)
    {
        state.PageSize = Math.Clamp(state.PageSize, 1, 100);
        state.Page = Math.Max(1, state.Page);

        var query = _db.Debts.Include(d => d.Debtor).AsQueryable();

        // Search filter
        if (!string.IsNullOrEmpty(state.Search))
        {
            query = query.Where(d => d.Description.Contains(state.Search) ||
                                     d.Debtor.Name.Contains(state.Search));
        }

        // Currency filter
        if (state.Currency != "all")
        {
            query = query.Where(d => d.Currency == state.Currency);
        }

        // Status filter
        query = state.Status switch
        {
            "paid" => query.Where(d => d.IsPaid),
            "unpaid" => query.Where(d => !d.IsPaid),
            _ => query
        };

        // Sorting
        query = (state.SortBy, state.SortDesc) switch
        {
            ("date", false) => query.OrderBy(d => d.CreatedAt),
            ("date", true) => query.OrderByDescending(d => d.CreatedAt),
            ("amount", false) => query.OrderBy(d => d.Amount),
            ("amount", true) => query.OrderByDescending(d => d.Amount),
            ("debtor", false) => query.OrderBy(d => d.Debtor.Name),
            ("debtor", true) => query.OrderByDescending(d => d.Debtor.Name),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)state.PageSize);
        if (totalPages > 0)
        {
            state.Page = Math.Min(state.Page, totalPages);
        }

        var items = await query
            .Skip((state.Page - 1) * state.PageSize)
            .Take(state.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Recent debts partial for dashboard
    /// </summary>
    public async Task<IActionResult> Recent(int take = 10)
    {
        var debts = await _db.Debts
            .Include(d => d.Debtor)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .ToListAsync();

        return SwapView(SwapViews.Dashboard._RecentDebts, debts);
    }

    /// <summary>
    /// Create form - modal content
    /// </summary>
    public async Task<IActionResult> Create(int? debtorId = null)
    {
        var debtors = await _db.Debtors.OrderBy(d => d.Name).ToListAsync();
        var currencies = _currency.GetSupportedCurrencies();

        ViewData["Debtors"] = debtors;
        ViewData["Currencies"] = currencies;
        ViewData["SelectedDebtorId"] = debtorId;

        return SwapView(SwapViews.Debts._CreateForm, new Debt { DebtorId = debtorId ?? 0 });
    }

    /// <summary>
    /// Create debt - triggers DebtCreated event
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] Debt debt)
    {
        var debtor = await _db.Debtors.FindAsync(debt.DebtorId);
        if (debtor == null)
        {
            return SwapResponse()
                .WithErrorToast("Debtor not found")
                .Build();
        }

        if (debt.Amount <= 0)
        {
            return SwapResponse()
                .WithErrorToast("Amount must be greater than 0")
                .Build();
        }

        debt.CreatedAt = DateTime.UtcNow;
        _db.Debts.Add(debt);
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtEvents.Debt.Created,
            new DebtCreatedEvent { Id = debt.Id, Amount = debt.Amount, Currency = debt.Currency, DebtorName = debtor.Name }))
            .WithClientAction("closeModal")
            .Build();
    }

    /// <summary>
    /// Quick add debt inline
    /// </summary>
    public async Task<IActionResult> QuickAdd(int debtorId)
    {
        var debtor = await _db.Debtors.FindAsync(debtorId);
        if (debtor == null) return NotFound();

        ViewData["Currencies"] = _currency.GetSupportedCurrencies();
        ViewData["DebtorName"] = debtor.Name;
        return SwapView(SwapViews.Debts._QuickAddForm, new Debt { DebtorId = debtorId });
    }

    /// <summary>
    /// Quick add debt - inline creation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> QuickAdd([FromForm] Debt debt)
    {
        var debtor = await _db.Debtors.FindAsync(debt.DebtorId);
        if (debtor == null) return NotFound();

        if (debt.Amount <= 0)
        {
            return SwapResponse()
                .WithErrorToast("Amount must be greater than 0")
                .Build();
        }

        debt.CreatedAt = DateTime.UtcNow;
        _db.Debts.Add(debt);
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtEvents.Debt.Created,
            new DebtCreatedEvent { Id = debt.Id, Amount = debt.Amount, Currency = debt.Currency, DebtorName = debtor.Name }))
            .WithClientAction("closeModal")
            .Build();
    }

    /// <summary>
    /// Mark debt as paid - triggers DebtPaid event
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var debt = await _db.Debts.Include(d => d.Debtor).FirstOrDefaultAsync(d => d.Id == id);
        if (debt == null) return NotFound();

        debt.IsPaid = true;
        debt.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtEvents.Debt.Paid,
            new DebtPaidEvent { Id = debt.Id, Amount = debt.Amount, Currency = debt.Currency, DebtorName = debt.Debtor.Name }))
            .Build();
    }

    /// <summary>
    /// Delete debt - triggers DebtDeleted event
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var debt = await _db.Debts.Include(d => d.Debtor).FirstOrDefaultAsync(d => d.Id == id);
        if (debt == null) return NotFound();

        var amount = debt.Amount;
        var currency = debt.Currency;
        var debtorName = debt.Debtor?.Name;
        _db.Debts.Remove(debt);
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtEvents.Debt.Deleted,
            new DebtDeletedEvent { Id = id, Amount = amount, Currency = currency, DebtorName = debtorName }))
            .Build();
    }

    /// <summary>
    /// Convert debt amount to different currency
    /// </summary>
    public async Task<IActionResult> Convert(int id, string toCurrency)
    {
        var debt = await _db.Debts.FindAsync(id);
        if (debt == null) return NotFound();

        var converted = await _currency.ConvertAsync(debt.Amount, debt.Currency, toCurrency);
        return SwapView(SwapViews.Debts._ConvertedAmount, new { Amount = converted, Currency = toCurrency });
    }
}

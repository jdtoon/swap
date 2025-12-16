using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using SwapDebtors.Data;
using SwapDebtors.Events;
using SwapDebtors.Handlers;
using SwapDebtors.Models;
using SwapDebtors.Services;

namespace SwapDebtors.Controllers;

/// <summary>
/// Debts controller - CRUD operations for debts.
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
    /// List debts for a debtor
    /// </summary>
    public async Task<IActionResult> Index(int? debtorId = null)
    {
        var query = _db.Debts.Include(d => d.Debtor).AsQueryable();

        if (debtorId.HasValue)
        {
            query = query.Where(d => d.DebtorId == debtorId.Value);
        }

        var debts = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        ViewData["DebtorId"] = debtorId;
        return SwapView(debts);
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

        return SwapView("Dashboard/_RecentDebts", debts);
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

        return SwapView("Debts/_CreateForm", new Debt { DebtorId = debtorId ?? 0 });
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
        return SwapView("Debts/_QuickAddForm", new Debt { DebtorId = debtorId });
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
        var debt = await _db.Debts.FindAsync(id);
        if (debt == null) return NotFound();

        var amount = debt.Amount;
        _db.Debts.Remove(debt);
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtEvents.Debt.Deleted,
            new DebtDeletedEvent { Id = id, Amount = amount }))
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
        return SwapView("Debts/_ConvertedAmount", new { Amount = converted, Currency = toCurrency });
    }
}

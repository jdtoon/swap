using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using SwapDebtors.Data;
using SwapDebtors.Events;
using SwapDebtors.Services;

namespace SwapDebtors.Controllers;

/// <summary>
/// Dashboard controller - the main page with stats and activity feed.
/// Demonstrates SwapController, SwapView, and SwapResponse patterns.
/// </summary>
public class DashboardController : SwapController
{
    private readonly DebtorsDbContext _db;
    private readonly ICurrencyService _currency;

    public DashboardController(DebtorsDbContext db, ICurrencyService currency)
    {
        _db = db;
        _currency = currency;
    }

    /// <summary>
    /// Main dashboard page - full page load
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var stats = new StatsModel
        {
            TotalDebtors = await _db.Debtors.CountAsync(),
            TotalDebts = await _db.Debts.CountAsync(),
            TotalAmount = await _db.Debts.Where(d => !d.IsPaid).SumAsync(d => d.Amount),
            ActiveDebts = await _db.Debts.CountAsync(d => !d.IsPaid)
        };

        var recentDebts = await _db.Debts
            .Include(d => d.Debtor)
            .OrderByDescending(d => d.CreatedAt)
            .Take(10)
            .ToListAsync();

        var debtors = await _db.Debtors
            .Include(d => d.Debts)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var rates = await _currency.GetRatesAsync();

        ViewData["Stats"] = stats;
        ViewData["RecentDebts"] = recentDebts;
        ViewData["Debtors"] = debtors;
        ViewData["Rates"] = rates;

        return SwapView();
    }

    /// <summary>
    /// Stats partial - returns just the stats section
    /// </summary>
    public async Task<IActionResult> Stats()
    {
        var stats = new StatsModel
        {
            TotalDebtors = await _db.Debtors.CountAsync(),
            TotalDebts = await _db.Debts.CountAsync(),
            TotalAmount = await _db.Debts.Where(d => !d.IsPaid).SumAsync(d => d.Amount),
            ActiveDebts = await _db.Debts.CountAsync(d => !d.IsPaid)
        };
        return SwapView("Dashboard/_Stats", stats);
    }

    /// <summary>
    /// Currency rates partial with conversion
    /// </summary>
    public async Task<IActionResult> Rates(string baseCurrency = "USD")
    {
        var rates = await _currency.GetRatesAsync(baseCurrency);
        return SwapView("Dashboard/_Rates", rates);
    }

    /// <summary>
    /// Refresh rates from external API
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RefreshRates()
    {
        await _currency.RefreshRatesAsync();
        var rates = await _currency.GetRatesAsync();

        return SwapResponse()
            .WithView("Dashboard/_Rates", rates)
            .WithSuccessToast("Currency rates updated")
            .Build();
    }
}

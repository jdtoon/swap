using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapDebtors.Data;
using Microsoft.EntityFrameworkCore;

namespace SwapDebtors.Events;

/// <summary>
/// Event configuration for debt-related events.
/// Defines what happens when debt actions occur - which partials to update.
/// </summary>
public class DebtEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(DebtEvents.Debt.Created)
            .RefreshPartial("recent-debts", "Dashboard/_RecentDebts", ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return db.Debts.Include(d => d.Debtor).OrderByDescending(d => d.CreatedAt).Take(10).ToList();
            })
            .RefreshPartial("stats", "Dashboard/_Stats", ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return new StatsModel
                {
                    TotalDebtors = db.Debtors.Count(),
                    TotalDebts = db.Debts.Count(),
                    TotalAmount = db.Debts.Where(d => !d.IsPaid).Sum(d => d.Amount),
                    ActiveDebts = db.Debts.Count(d => !d.IsPaid)
                };
            })
            .Toast("Debt recorded", ToastType.Success);

        config.When(DebtEvents.Debt.Paid)
            .RefreshPartial("recent-debts", "Dashboard/_RecentDebts", ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return db.Debts.Include(d => d.Debtor).OrderByDescending(d => d.CreatedAt).Take(10).ToList();
            })
            .RefreshPartial("stats", "Dashboard/_Stats", ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return new StatsModel
                {
                    TotalDebtors = db.Debtors.Count(),
                    TotalDebts = db.Debts.Count(),
                    TotalAmount = db.Debts.Where(d => !d.IsPaid).Sum(d => d.Amount),
                    ActiveDebts = db.Debts.Count(d => !d.IsPaid)
                };
            })
            .Toast("Debt marked as paid!", ToastType.Success);

        config.When(DebtEvents.Debt.Deleted)
            .RefreshPartial("recent-debts", "Dashboard/_RecentDebts", ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return db.Debts.Include(d => d.Debtor).OrderByDescending(d => d.CreatedAt).Take(10).ToList();
            })
            .RefreshPartial("stats", "Dashboard/_Stats", ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return new StatsModel
                {
                    TotalDebtors = db.Debtors.Count(),
                    TotalDebts = db.Debts.Count(),
                    TotalAmount = db.Debts.Where(d => !d.IsPaid).Sum(d => d.Amount),
                    ActiveDebts = db.Debts.Count(d => !d.IsPaid)
                };
            })
            .Toast("Debt deleted", ToastType.Warning);
    }
}

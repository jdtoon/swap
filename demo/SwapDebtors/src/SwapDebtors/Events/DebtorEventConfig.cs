using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.Extensions;
using SwapDebtors.Data;
using Microsoft.EntityFrameworkCore;

namespace SwapDebtors.Events;

/// <summary>
/// Event configuration for debtor-related events.
/// Defines what happens when debtor actions occur - which partials to update.
/// Uses generated SwapViews and SwapElements constants.
/// </summary>
public class DebtorEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions config)
    {
        config.When(DebtorEvents.Debtor.Created)
            .RefreshPartial(SwapElements.DebtorList, SwapViews.Dashboard._DebtorList, ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return db.Debtors.Include(d => d.Debts).OrderByDescending(d => d.CreatedAt).ToList();
            })
            .RefreshPartial(SwapElements.Stats, SwapViews.Dashboard._Stats, ctx =>
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
            .Toast("Debtor created", ToastType.Success);

        config.When(DebtorEvents.Debtor.Updated)
            .RefreshPartial(SwapElements.DebtorList, SwapViews.Dashboard._DebtorList, ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return db.Debtors.Include(d => d.Debts).OrderByDescending(d => d.CreatedAt).ToList();
            })
            .Toast("Debtor updated", ToastType.Info);

        config.When(DebtorEvents.Debtor.Deleted)
            .RefreshPartial(SwapElements.DebtorList, SwapViews.Dashboard._DebtorList, ctx =>
            {
                var db = ctx.RequestServices.GetRequiredService<DebtorsDbContext>();
                return db.Debtors.Include(d => d.Debts).OrderByDescending(d => d.CreatedAt).ToList();
            })
            .RefreshPartial(SwapElements.Stats, SwapViews.Dashboard._Stats, ctx =>
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
            .Toast("Debtor deleted", ToastType.Warning);
    }
}

/// <summary>
/// Stats model for dashboard stats partial
/// </summary>
public record StatsModel
{
    public int TotalDebtors { get; init; }
    public int TotalDebts { get; init; }
    public decimal TotalAmount { get; init; }
    public int ActiveDebts { get; init; }
}

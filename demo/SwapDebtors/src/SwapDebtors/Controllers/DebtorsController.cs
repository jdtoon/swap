using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using Swap.Htmx.State;
using SwapDebtors.Data;
using SwapDebtors.Events;
using SwapDebtors.Handlers;
using SwapDebtors.Models;

namespace SwapDebtors.Controllers;

/// <summary>
/// Debtors controller - CRUD operations with SwapState for filtering/pagination.
/// Demonstrates [FromSwapState] binding pattern.
/// </summary>
public class DebtorsController : SwapController
{
    private readonly DebtorsDbContext _db;

    public DebtorsController(DebtorsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all debtors - full page with SwapState
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var state = new DebtorFilterState();
        var (debtors, totalCount) = await FilterDebtorsAsync(state);

        return SwapView(new DebtorListViewModel
        {
            State = state,
            Debtors = debtors,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Filter debtors - called via HTMX with SwapState
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Filter([FromSwapState] DebtorFilterState state)
    {
        Console.WriteLine($"[Debtors/Filter] Search={state.Search}, Page={state.Page}, SortBy={state.SortBy}, SortDesc={state.SortDesc}");

        var (debtors, totalCount) = await FilterDebtorsAsync(state);

        return PartialView("_FilterContent", new DebtorListViewModel
        {
            State = state,
            Debtors = debtors,
            TotalCount = totalCount
        });
    }

    private async Task<(List<Debtor> Items, int TotalCount)> FilterDebtorsAsync(DebtorFilterState state)
    {
        var query = _db.Debtors.Include(d => d.Debts).AsQueryable();

        // Search filter
        if (!string.IsNullOrEmpty(state.Search))
        {
            query = query.Where(d => d.Name.Contains(state.Search) ||
                                     (d.Email != null && d.Email.Contains(state.Search)));
        }

        // Sorting
        query = (state.SortBy, state.SortDesc) switch
        {
            ("name", false) => query.OrderBy(d => d.Name),
            ("name", true) => query.OrderByDescending(d => d.Name),
            ("total", false) => query.OrderBy(d => d.Debts.Where(debt => !debt.IsPaid).Sum(debt => debt.Amount)),
            ("total", true) => query.OrderByDescending(d => d.Debts.Where(debt => !debt.IsPaid).Sum(debt => debt.Amount)),
            ("date", false) => query.OrderBy(d => d.CreatedAt),
            ("date", true) => query.OrderByDescending(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((state.Page - 1) * state.PageSize)
            .Take(state.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Debtor list partial for Dashboard use
    /// </summary>
    public async Task<IActionResult> List(string? search = null)
    {
        var query = _db.Debtors.Include(d => d.Debts).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => d.Name.Contains(search) ||
                                     (d.Email != null && d.Email.Contains(search)));
        }

        var debtors = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return SwapView(SwapViews.Debtors._List, debtors);
    }

    /// <summary>
    /// Debtor details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var debtor = await _db.Debtors.Include(d => d.Debts).FirstOrDefaultAsync(d => d.Id == id);
        if (debtor == null) return NotFound();
        return SwapView(debtor);
    }

    /// <summary>
    /// Create form - returns modal content
    /// </summary>
    public IActionResult Create()
    {
        return SwapView(SwapViews.Debtors._CreateForm, new Debtor());
    }

    /// <summary>
    /// Create debtor - triggers DebtorCreated event
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] Debtor debtor)
    {
        if (!ModelState.IsValid)
        {
            return SwapResponse()
                .WithView(SwapViews.Debtors._CreateForm, debtor)
                .WithErrorToast("Please fix the errors")
                .Build();
        }

        debtor.CreatedAt = DateTime.UtcNow;
        _db.Debtors.Add(debtor);
        await _db.SaveChangesAsync();

        // Trigger the event - event chain handles all UI updates
        return (await SwapEventAsync(
            DebtorEvents.Debtor.Created,
            new DebtorCreatedEvent { Id = debtor.Id, Name = debtor.Name }))
            .WithClientAction("closeModal")
            .Build();
    }

    /// <summary>
    /// Edit form - returns form partial
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var debtor = await _db.Debtors.FindAsync(id);
        if (debtor == null) return NotFound();
        return SwapView(SwapViews.Debtors._EditForm, debtor);
    }

    /// <summary>
    /// Update debtor - triggers DebtorUpdated event
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [FromForm] Debtor updated)
    {
        var debtor = await _db.Debtors.FindAsync(id);
        if (debtor == null) return NotFound();

        if (!ModelState.IsValid)
        {
            return SwapResponse()
                .WithView(SwapViews.Debtors._EditForm, updated)
                .WithErrorToast("Please fix the errors")
                .Build();
        }

        debtor.Name = updated.Name;
        debtor.Email = updated.Email;
        debtor.Phone = updated.Phone;
        debtor.Notes = updated.Notes;
        debtor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtorEvents.Debtor.Updated,
            new DebtorUpdatedEvent { Id = debtor.Id, Name = debtor.Name }))
            .WithClientAction("closeModal")
            .Build();
    }

    /// <summary>
    /// Delete confirmation modal
    /// </summary>
    public async Task<IActionResult> DeleteConfirm(int id)
    {
        var debtor = await _db.Debtors.FindAsync(id);
        if (debtor == null) return NotFound();
        return SwapView(SwapViews.Debtors._DeleteConfirm, debtor);
    }

    /// <summary>
    /// Delete debtor - triggers DebtorDeleted event
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var debtor = await _db.Debtors.FindAsync(id);
        if (debtor == null) return NotFound();

        var name = debtor.Name;
        _db.Debtors.Remove(debtor);
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtorEvents.Debtor.Deleted,
            new DebtorDeletedEvent { Id = id, Name = name }))
            .WithClientAction("closeModal")
            .Build();
    }

    /// <summary>
    /// Quick add form - inline form in list
    /// </summary>
    public IActionResult QuickAdd()
    {
        return SwapView(SwapViews.Debtors._QuickAddForm);
    }

    /// <summary>
    /// Quick add - inline creation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> QuickAdd([FromForm] string name, [FromForm] string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SwapResponse()
                .WithErrorToast("Name is required")
                .Build();
        }

        var debtor = new Debtor
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _db.Debtors.Add(debtor);
        await _db.SaveChangesAsync();

        return (await SwapEventAsync(
            DebtorEvents.Debtor.Created,
            new DebtorCreatedEvent { Id = debtor.Id, Name = debtor.Name }))
            .Build();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using SwapDebtors.Data;
using SwapDebtors.Events;
using SwapDebtors.Handlers;
using SwapDebtors.Models;

namespace SwapDebtors.Controllers;

/// <summary>
/// Debtors controller - CRUD operations for debtors.
/// Demonstrates SwapEvent pattern with event chains.
/// </summary>
public class DebtorsController : SwapController
{
    private readonly DebtorsDbContext _db;

    public DebtorsController(DebtorsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all debtors - full page
    /// </summary>
    public async Task<IActionResult> Index(string? search = null, int page = 1, int pageSize = 10)
    {
        var query = _db.Debtors.Include(d => d.Debts).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => d.Name.Contains(search) || 
                                     (d.Email != null && d.Email.Contains(search)));
        }

        var total = await query.CountAsync();
        var debtors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Search"] = search;
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);

        return SwapView(debtors);
    }

    /// <summary>
    /// Debtor list partial for search/filter updates
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
        return SwapView("Debtors/_List", debtors);
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
        return SwapView("Debtors/_CreateForm", new Debtor());
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
                .WithView("Debtors/_CreateForm", debtor)
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
        return SwapView("Debtors/_EditForm", debtor);
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
                .WithView("Debtors/_EditForm", updated)
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
        return SwapView("Debtors/_DeleteConfirm", debtor);
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
        return SwapView("Debtors/_QuickAddForm");
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

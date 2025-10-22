using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using Audit.Contracts.Dtos;
using Audit.Contracts.Services;

namespace Audit.Controllers;

public class AuditEntryController : Controller
{
    private readonly IAuditEntryService _service;

    public AuditEntryController(IAuditEntryService service)
    {
        _service = service;
    }

    // GET: /AuditEntry
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    // GET: /AuditEntry/List (HTMX)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();
        return PartialView("_List", items);
    }

    // GET: /AuditEntry/Create (HTMX)
    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_Form", new CreateAuditEntryDto());
    }

    // POST: /AuditEntry/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(CreateAuditEntryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        var created = await _service.CreateAsync(dto);
        
        // Trigger type-safe event
        this.HxTrigger(NetMX.Events.Events.AuditEntry.Created, new { id = created.Id });
        
        return await List();
    }

    // GET: /AuditEntry/Edit/{id} (HTMX)
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        var dto = new UpdateAuditEntryDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive
        };

        return PartialView("_Form", dto);
    }

    // POST: /AuditEntry/Edit (HTMX)
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateAuditEntryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);
        
        // Trigger type-safe event
        this.HxTrigger(NetMX.Events.Events.AuditEntry.Updated, new { id = dto.Id });
        
        return await List();
    }

    // DELETE: /AuditEntry/Delete/{id} (HTMX)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        // Trigger type-safe event
        this.HxTrigger(NetMX.Events.Events.AuditEntry.Deleted, new { id });
        
        // Tell HTMX to remove the row
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
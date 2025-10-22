using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using Audit.Contracts.Dtos;
using Audit.Contracts.Services;

namespace Audit.Controllers;

public class AuditLogController : Controller
{
    private readonly IAuditLogService _service;

    public AuditLogController(IAuditLogService service)
    {
        _service = service;
    }

    // GET: /AuditLog
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    // GET: /AuditLog/List (HTMX)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();
        return PartialView("_List", items);
    }

    // GET: /AuditLog/Create (HTMX)
    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_Form", new CreateAuditLogDto());
    }

    // POST: /AuditLog/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(CreateAuditLogDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        var created = await _service.CreateAsync(dto);
        
        // Trigger type-safe event
        this.HxTrigger(NetMX.Events.Events.AuditLog.Created, new { id = created.Id });
        
        return await List();
    }

    // GET: /AuditLog/Edit/{id} (HTMX)
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        var dto = new UpdateAuditLogDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive
        };

        return PartialView("_Form", dto);
    }

    // POST: /AuditLog/Edit (HTMX)
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateAuditLogDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);
        
        // Trigger type-safe event
        this.HxTrigger(NetMX.Events.Events.AuditLog.Updated, new { id = dto.Id });
        
        return await List();
    }

    // DELETE: /AuditLog/Delete/{id} (HTMX)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        // Trigger type-safe event
        this.HxTrigger(NetMX.Events.Events.AuditLog.Deleted, new { id });
        
        // Tell HTMX to remove the row
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
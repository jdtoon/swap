using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using Authorization.Contracts.Dtos;
using Authorization.Contracts.Services;

namespace Authorization.Controllers;

public class PermissionController : Controller
{
    private readonly IPermissionService _service;

    public PermissionController(IPermissionService service)
    {
        _service = service;
    }

    // GET: /Permission
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    // GET: /Permission/List (HTMX)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();
        return PartialView("_List", items);
    }

    // GET: /Permission/Create (HTMX)
    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_Form", new CreatePermissionDto());
    }

    // POST: /Permission/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(CreatePermissionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        var created = await _service.CreateAsync(dto);
        
        // TODO: Add domain event when DomainEvents.Permission is implemented
        // this.HxTrigger(DomainEvents.Permission.Created, new { id = created.Id });
        
        return await List();
    }

    // GET: /Permission/Edit/{id} (HTMX)
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        var dto = new UpdatePermissionDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive
        };

        return PartialView("_Form", dto);
    }

    // POST: /Permission/Edit (HTMX)
    [HttpPost]
    public async Task<IActionResult> Edit(UpdatePermissionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);
        
        // TODO: Add domain event when DomainEvents.Permission is implemented
        // this.HxTrigger(DomainEvents.Permission.Updated, new { id = dto.Id });
        
        return await List();
    }

    // DELETE: /Permission/Delete/{id} (HTMX)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        // TODO: Add domain event when DomainEvents.Permission is implemented
        // this.HxTrigger(DomainEvents.Permission.Deleted, new { id });
        
        // Tell HTMX to remove the row
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
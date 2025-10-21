using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using Authorization.Contracts.Dtos;
using Authorization.Contracts.Services;

namespace Authorization.Controllers;

public class RoleController : Controller
{
    private readonly IRoleService _service;

    public RoleController(IRoleService service)
    {
        _service = service;
    }

    // GET: /Role
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    // GET: /Role/List (HTMX)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();
        return PartialView("_List", items);
    }

    // GET: /Role/Create (HTMX)
    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_Form", new CreateRoleDto());
    }

    // POST: /Role/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        var created = await _service.CreateAsync(dto);
        
        // TODO: Add domain event when DomainEvents.Role is implemented
        // this.HxTrigger(DomainEvents.Role.Created, new { id = created.Id });
        
        return await List();
    }

    // GET: /Role/Edit/{id} (HTMX)
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        var dto = new UpdateRoleDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive
        };

        return PartialView("_Form", dto);
    }

    // POST: /Role/Edit (HTMX)
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateRoleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);
        
        // TODO: Add domain event when DomainEvents.Role is implemented
        // this.HxTrigger(DomainEvents.Role.Updated, new { id = dto.Id });
        
        return await List();
    }

    // DELETE: /Role/Delete/{id} (HTMX)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        // TODO: Add domain event when DomainEvents.Role is implemented
        // this.HxTrigger(DomainEvents.Role.Deleted, new { id });
        
        // Tell HTMX to remove the row
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
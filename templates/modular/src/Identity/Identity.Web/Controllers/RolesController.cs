using Identity.Application.Contracts.Roles;
using Identity.Application.Roles;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.Controllers;

/// <summary>
/// HTMX-first controller for role management.
/// All actions return partial views for seamless page updates.
/// </summary>
public class RolesController : Controller
{
    private readonly RoleAppService _roleAppService;

    public RolesController(RoleAppService roleAppService)
    {
        _roleAppService = roleAppService;
    }

    // GET: /Roles
    public async Task<IActionResult> Index()
    {
        var roles = await _roleAppService.GetListAsync();
        
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_RoleList", roles);
        }
        
        return View(roles);
    }

    // GET: /Roles/List
    public async Task<IActionResult> List()
    {
        var roles = await _roleAppService.GetListAsync();
        return PartialView("_RoleList", roles);
    }

    // GET: /Roles/New
    public IActionResult New()
    {
        return PartialView("_RoleForm", new CreateRoleDto());
    }

    // GET: /Roles/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var role = await _roleAppService.GetAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var updateDto = new UpdateRoleDto
        {
            Name = role.Name,
            Description = role.Description
        };

        ViewBag.RoleId = id;
        ViewBag.IsSystemRole = role.IsSystemRole;
        return PartialView("_RoleEditForm", updateDto);
    }

    // POST: /Roles/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateRoleDto input)
    {
        if (!ModelState.IsValid)
        {
            Response.Headers.Add("HX-Retarget", "#role-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_RoleForm", input);
        }

        try
        {
            await _roleAppService.CreateAsync(input);
            
            var roles = await _roleAppService.GetListAsync();
            
            Response.Headers.Add("HX-Retarget", "#role-list-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            
            return PartialView("_RoleList", roles);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            Response.Headers.Add("HX-Retarget", "#role-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_RoleForm", input);
        }
    }

    // POST: /Roles/Update/5
    [HttpPost]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateRoleDto input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RoleId = id;
            Response.Headers.Add("HX-Retarget", "#role-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_RoleEditForm", input);
        }

        try
        {
            var role = await _roleAppService.UpdateAsync(id, input);
            
            Response.Headers.Add("HX-Retarget", $"#role-row-{id}");
            Response.Headers.Add("HX-Reswap", "outerHTML");
            
            return PartialView("_RoleRow", role);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.RoleId = id;
            Response.Headers.Add("HX-Retarget", "#role-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_RoleEditForm", input);
        }
    }

    // DELETE: /Roles/Delete/5
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _roleAppService.DeleteAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

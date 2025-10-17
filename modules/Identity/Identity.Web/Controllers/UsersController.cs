using Identity.Application.Contracts.Users;
using Identity.Application.Users;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.Controllers;

/// <summary>
/// HTMX-first controller for user management.
/// All actions return partial views for seamless page updates.
/// </summary>
public class UsersController : Controller
{
    private readonly UserAppService _userAppService;

    public UsersController(UserAppService userAppService)
    {
        _userAppService = userAppService;
    }

    // GET: /Users
    // Returns full page on initial load, partial on HTMX requests
    public async Task<IActionResult> Index()
    {
        var users = await _userAppService.GetListAsync();
        
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_UserList", users);
        }
        
        return View(users);
    }

    // GET: /Users/List
    // Returns user list partial (for HTMX refreshes)
    public async Task<IActionResult> List()
    {
        var users = await _userAppService.GetListAsync();
        return PartialView("_UserList", users);
    }

    // GET: /Users/New
    // Returns empty form partial
    public IActionResult New()
    {
        return PartialView("_UserForm", new CreateUserDto());
    }

    // GET: /Users/Edit/5
    // Returns populated form partial
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userAppService.GetAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var updateDto = new UpdateUserDto
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed
        };

        ViewBag.UserId = id;
        ViewBag.Email = user.Email;
        return PartialView("_UserEditForm", updateDto);
    }

    // POST: /Users/Create
    // Creates user and returns updated list
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateUserDto input)
    {
        if (!ModelState.IsValid)
        {
            Response.Headers.Add("HX-Retarget", "#user-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_UserForm", input);
        }

        try
        {
            await _userAppService.CreateAsync(input);
            
            // Return updated user list
            var users = await _userAppService.GetListAsync();
            
            // Tell HTMX to target the user list container
            Response.Headers.Add("HX-Retarget", "#user-list-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            
            return PartialView("_UserList", users);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            Response.Headers.Add("HX-Retarget", "#user-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_UserForm", input);
        }
    }

    // POST: /Users/Update/5
    // Updates user and returns updated row
    [HttpPost]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateUserDto input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UserId = id;
            Response.Headers.Add("HX-Retarget", "#user-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_UserEditForm", input);
        }

        try
        {
            var user = await _userAppService.UpdateAsync(id, input);
            
            // Return updated user row
            Response.Headers.Add("HX-Retarget", $"#user-row-{id}");
            Response.Headers.Add("HX-Reswap", "outerHTML");
            
            return PartialView("_UserRow", user);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.UserId = id;
            Response.Headers.Add("HX-Retarget", "#user-form-container");
            Response.Headers.Add("HX-Reswap", "innerHTML");
            return PartialView("_UserEditForm", input);
        }
    }

    // DELETE: /Users/Delete/5
    // Deletes user and returns empty response (HTMX removes row)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _userAppService.DeleteAsync(id);
            return Ok(); // HTMX will remove the row via hx-swap="delete"
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: /Users/Activate/5
    // Activates user and returns updated row
    [HttpPost]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            await _userAppService.ActivateAsync(id);
            var user = await _userAppService.GetAsync(id);
            
            Response.Headers.Add("HX-Retarget", $"#user-row-{id}");
            Response.Headers.Add("HX-Reswap", "outerHTML");
            
            return PartialView("_UserRow", user);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: /Users/Deactivate/5
    // Deactivates user and returns updated row
    [HttpPost]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            await _userAppService.DeactivateAsync(id);
            var user = await _userAppService.GetAsync(id);
            
            Response.Headers.Add("HX-Retarget", $"#user-row-{id}");
            Response.Headers.Add("HX-Reswap", "outerHTML");
            
            return PartialView("_UserRow", user);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

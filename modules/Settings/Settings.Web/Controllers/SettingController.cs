namespace Settings.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using Settings.Contracts.Dtos;
using Settings.Contracts.Services;

/// <summary>
/// Controller for Setting CRUD operations with HTMX support
/// </summary>
public class SettingController : Controller
{
    private readonly ISettingService _service;

    public SettingController(ISettingService service)
    {
        _service = service;
    }

    /// <summary>
    /// Displays the Setting list page
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Gets Setting list (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();

        return PartialView("_List", items);
    }

    /// <summary>
    /// Shows create Setting form (HTMX partial)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateSettingDto();
        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Creates a new Setting (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateSettingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger(Events.Setting.Created);

        return Ok();
    }

    /// <summary>
    /// Shows edit Setting form (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        var dto = new UpdateSettingDto
        {
            Id = entity.Id,
        };

        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Updates an existing Setting (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateSettingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger(Events.Setting.Updated);

        return Ok();
    }

    /// <summary>
    /// Deletes a Setting (HTMX)
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        // Trigger HTMX event to refresh list
        this.HxTrigger(Events.Setting.Deleted);

        // Use HTMX swap to remove the row
        this.HxReswap(HtmxSwap.Delete);

        return Ok();
    }
}

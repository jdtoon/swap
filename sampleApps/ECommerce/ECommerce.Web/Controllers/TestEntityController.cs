namespace ECommerce.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using ECommerce.Web.Dtos;
using ECommerce.Web.Services;
using NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Controller for TestEntity CRUD operations with HTMX support
/// </summary>
public class TestEntityController : Controller
{
    private readonly ITestEntityService _service;

    public TestEntityController(ITestEntityService service)
    {
        _service = service;
    }

    /// <summary>
    /// Displays the TestEntity list page
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Gets TestEntity list (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();

        return PartialView("_List", items);
    }

    /// <summary>
    /// Shows create TestEntity form (HTMX partial)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateTestEntityDto();
        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Creates a new TestEntity (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateTestEntityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("testentity-created");

        return Ok();
    }

    /// <summary>
    /// Shows edit TestEntity form (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        var dto = new UpdateTestEntityDto
        {
            Id = entity.Id,
        };

        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Updates an existing TestEntity (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateTestEntityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("testentity-updated");

        return Ok();
    }

    /// <summary>
    /// Deletes a TestEntity (HTMX)
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        // Trigger HTMX event to refresh list
        this.HxTrigger("testentity-deleted");

        // Use HTMX swap to remove the row
        this.HxReswap(HtmxSwap.Delete);

        return Ok();
    }
}

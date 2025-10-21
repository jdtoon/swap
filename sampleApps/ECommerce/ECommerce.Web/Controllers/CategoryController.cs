namespace ECommerce.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using ECommerce.Web.Dtos;
using ECommerce.Web.Services;
using NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Controller for Category CRUD operations with HTMX support
/// </summary>
public class CategoryController : Controller
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Displays the Category list page
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Gets Category list (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();

        return PartialView("_List", items);
    }

    /// <summary>
    /// Shows create Category form (HTMX partial)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateCategoryDto();
        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Creates a new Category (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("category-created");

        return Ok();
    }

    /// <summary>
    /// Shows edit Category form (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        var dto = new UpdateCategoryDto
        {
            Id = entity.Id,
        };

        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Updates an existing Category (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("category-updated");

        return Ok();
    }

    /// <summary>
    /// Deletes a Category (HTMX)
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        // Trigger HTMX event to refresh list
        this.HxTrigger("category-deleted");

        // Use HTMX swap to remove the row
        this.HxReswap(HtmxSwap.Delete);

        return Ok();
    }
}

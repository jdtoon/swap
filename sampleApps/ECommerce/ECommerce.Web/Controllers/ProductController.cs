namespace ECommerce.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using ECommerce.Web.Dtos;
using ECommerce.Web.Services;
using NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Controller for Product CRUD operations with HTMX support
/// </summary>
public class ProductController : Controller
{
    private readonly IProductService _service;

    public ProductController(IProductService service)
    {
        _service = service;
    }

    /// <summary>
    /// Displays the Product list page
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Gets Product list (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();

        return PartialView("_List", items);
    }

    /// <summary>
    /// Shows create Product form (HTMX partial)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateProductDto();
        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Creates a new Product (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("product-created");

        return Ok();
    }

    /// <summary>
    /// Shows edit Product form (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        var dto = new UpdateProductDto
        {
            Id = entity.Id,
        };

        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Updates an existing Product (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("product-updated");

        return Ok();
    }

    /// <summary>
    /// Deletes a Product (HTMX)
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        // Trigger HTMX event to refresh list
        this.HxTrigger("product-deleted");

        // Use HTMX swap to remove the row
        this.HxReswap(HtmxSwap.Delete);

        return Ok();
    }
}

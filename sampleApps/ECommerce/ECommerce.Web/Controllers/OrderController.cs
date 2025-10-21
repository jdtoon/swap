namespace ECommerce.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using ECommerce.Web.Dtos;
using ECommerce.Web.Services;
using NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Controller for Order CRUD operations with HTMX support
/// </summary>
public class OrderController : Controller
{
    private readonly IOrderService _service;

    public OrderController(IOrderService service)
    {
        _service = service;
    }

    /// <summary>
    /// Displays the Order list page
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Gets Order list (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();

        return PartialView("_List", items);
    }

    /// <summary>
    /// Shows create Order form (HTMX partial)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateOrderDto();
        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Creates a new Order (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("order-created");

        return Ok();
    }

    /// <summary>
    /// Shows edit Order form (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        var dto = new UpdateOrderDto
        {
            Id = entity.Id,
        };

        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Updates an existing Order (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger("order-updated");

        return Ok();
    }

    /// <summary>
    /// Deletes a Order (HTMX)
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        // Trigger HTMX event to refresh list
        this.HxTrigger("order-deleted");

        // Use HTMX swap to remove the row
        this.HxReswap(HtmxSwap.Delete);

        return Ok();
    }
}

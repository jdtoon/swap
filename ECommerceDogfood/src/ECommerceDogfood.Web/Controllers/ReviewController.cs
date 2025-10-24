namespace ECommerceDogfood.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using ECommerceDogfood.Web.Dtos;
using ECommerceDogfood.Web.Services;

/// <summary>
/// Controller for Review CRUD operations with HTMX support
/// </summary>
public class ReviewController : Controller
{
    private readonly IReviewService _service;

    public ReviewController(IReviewService service)
    {
        _service = service;
    }

    /// <summary>
    /// Displays the Review list page
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Gets Review list (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();

        return PartialView("_List", items);
    }

    /// <summary>
    /// Shows create Review form (HTMX partial)
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateReviewDto();
        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Creates a new Review (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger(Events.Review.Created);

        return Ok();
    }

    /// <summary>
    /// Shows edit Review form (HTMX partial)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        var dto = new UpdateReviewDto
        {
            Id = entity.Id,
        };

        return PartialView("_Form", dto);
    }

    /// <summary>
    /// Updates an existing Review (HTMX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateReviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);

        // Trigger HTMX event to refresh list and close modal
        this.HxTrigger(Events.Review.Updated);

        return Ok();
    }

    /// <summary>
    /// Deletes a Review (HTMX)
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        // Trigger HTMX event to refresh list
        this.HxTrigger(Events.Review.Deleted);

        // Use HTMX swap to remove the row
        this.HxReswap(HtmxSwap.Delete);

        return Ok();
    }
}

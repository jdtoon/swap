using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMXApp.Dtos;
using NetMXApp.Services;

namespace NetMXApp.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _service;

    public ProductController(IProductService service)
    {
        _service = service;
    }

    // GET: /Product
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    // GET: /Product/List (HTMX)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();
        return PartialView("_List", items);
    }

    // GET: /Product/Create (HTMX)
    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_Form", new CreateProductDto());
    }

    // POST: /Product/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.CreateAsync(dto);
        
        // Trigger success event and reload list
        this.HxTrigger("product-created");
        
        return await List();
    }

    // GET: /Product/Edit/{id} (HTMX)
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        var dto = new UpdateProductDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive
        };

        return PartialView("_Form", dto);
    }

    // POST: /Product/Edit (HTMX)
    [HttpPost]
    public async Task<IActionResult> Edit(UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);
        
        // Trigger success event and reload list
        this.HxTrigger("product-updated");
        
        return await List();
    }

    // DELETE: /Product/Delete/{id} (HTMX)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        // Tell HTMX to remove the row
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
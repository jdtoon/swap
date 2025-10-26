using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swap.CLI.Data;
using Swap.CLI.Models;
using Swap.CLI.ViewModels;

namespace Swap.CLI.Controllers;

public class ProductController : Controller
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Index action with pagination, search, sorting, and filtering support
    /// </summary>
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? sortBy = null, string? sortOrder = "asc", bool? inStock = null)
    {
        var isHtmxRequest = Request.Headers.ContainsKey("HX-Request");

        // Build query with search filter
        var query = _context.Products.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm.ToLower()) || x.SKU.ToLower().Contains(searchTerm.ToLower()));
        }

        // Apply filters
        query = ApplyFilters(query, inStock);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query = ApplySorting(query, sortBy, sortOrder ?? "asc");
        }

        // Get total count for pagination
        var totalItems = await query.CountAsync();
        
        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Build view model with pagination
        var viewModel = new ProductListViewModel
        {
            Items = items,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Filters = new Dictionary<string, string?>
            {
                { "inStock", inStock?.ToString().ToLower() }
            },
            Pagination = new PaginationDto
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                HxGetUrl = Url.Action("Index"),
                HxTarget = "#product-list",
                HxSwap = "innerHTML"
            }
        };

        if (isHtmxRequest)
        {
            // Return partial view for HTMX requests
            return PartialView("_ProductList", viewModel);
        }

        // Return full page for initial load
        return View(viewModel);
    }

    /// <summary>
    /// GET: Display create modal
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new Product
        {
            CreatedDate = DateTime.Now
        };
        
        return PartialView("_ProductCreateModal", model);
    }

    /// <summary>
    /// POST: Create new entity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(Product model)
    {
        if (!ModelState.IsValid)
        {
            // Return form with validation errors
            Response.Headers.Append("HX-Retarget", "#modal-container");
            Response.Headers.Append("HX-Reswap", "innerHTML");
            return PartialView("_ProductCreateModal", model);
        }

        _context.Products.Add(model);
        await _context.SaveChangesAsync();

        // Trigger list refresh and success toast
        Response.Headers.Append("HX-Trigger", "{\"refreshProductList\": null, \"showToast\": {\"type\": \"success\", \"message\": \"Product created successfully!\"}}");
        
        return Content("");
    }

    /// <summary>
    /// GET: Display edit modal
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _context.Products.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        return PartialView("_ProductEditModal", entity);
    }

    /// <summary>
    /// POST: Update existing entity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Product model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            // Return form with validation errors
            Response.Headers.Append("HX-Retarget", "#modal-container");
            Response.Headers.Append("HX-Reswap", "innerHTML");
            return PartialView("_ProductEditModal", model);
        }

        try
        {
            _context.Update(model);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(model.Id))
            {
                return NotFound();
            }
            throw;
        }

        // Trigger list refresh and success toast
        Response.Headers.Append("HX-Trigger", "{\"refreshProductList\": null, \"showToast\": {\"type\": \"success\", \"message\": \"Product updated successfully!\"}}");
        
        return Content("");
    }

    /// <summary>
    /// GET: Display details modal
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var entity = await _context.Products.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        return PartialView("_ProductDetails", entity);
    }

    /// <summary>
    /// DELETE: Remove entity
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Products.FindAsync(id);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Products.Remove(entity);
        await _context.SaveChangesAsync();

        // Trigger list refresh and success toast
        Response.Headers.Append("HX-Trigger", "{\"refreshProductList\": null, \"showToast\": {\"type\": \"success\", \"message\": \"Product deleted successfully!\"}}");
        
        return Content("");
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    private IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "name" => isDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "instock" => isDescending ? query.OrderByDescending(x => x.InStock) : query.OrderBy(x => x.InStock),
            "createddate" => isDescending ? query.OrderByDescending(x => x.CreatedDate) : query.OrderBy(x => x.CreatedDate),
            _ => query
        };
    }

    private IQueryable<Product> ApplyFilters(IQueryable<Product> query, bool? inStock = null)
    {
        if (inStock.HasValue)
        {
            query = query.Where(x => x.InStock == inStock.Value);
        }
        
        return query;
    }
}

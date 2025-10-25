using EcomApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomApp.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    
    public ProductsController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }
    
    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        var products = !string.IsNullOrEmpty(search)
            ? await _productService.SearchAsync(search)
            : categoryId.HasValue
                ? await _productService.GetByCategoryAsync(categoryId.Value)
                : await _productService.GetAllAsync();
        
        var categories = await _categoryService.GetAllAsync();
        ViewBag.Categories = categories;
        ViewBag.CurrentCategory = categoryId;
        ViewBag.SearchQuery = search;
        
        // HTMX partial response
        if (Request.Headers["HX-Request"] == "true")
        {
            return PartialView("_ProductList", products);
        }
        
        return View(products);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        
        return View(product);
    }
    
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var products = await _productService.SearchAsync(q);
        return PartialView("_ProductList", products);
    }
}

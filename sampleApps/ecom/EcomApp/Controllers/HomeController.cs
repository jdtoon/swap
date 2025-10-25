using EcomApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcomApp.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    
    public HomeController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }
    
    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();
        var categories = await _categoryService.GetAllAsync();
        
        ViewBag.Categories = categories;
        return View(products);
    }
}

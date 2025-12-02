using Microsoft.AspNetCore.Mvc;
using SwapStateDemo.Models;

namespace SwapStateDemo.Controllers;

public class HomeController : Controller
{
    private static readonly List<Product> Products = new()
    {
        new(1, "Laptop Pro", "Electronics", 1299.99m, 15),
        new(2, "Wireless Mouse", "Electronics", 49.99m, 50),
        new(3, "Office Chair", "Furniture", 299.99m, 8),
        new(4, "USB-C Hub", "Electronics", 79.99m, 25),
        new(5, "Standing Desk", "Furniture", 599.99m, 3),
        new(6, "Monitor 27\"", "Electronics", 449.99m, 12),
        new(7, "Keyboard", "Electronics", 149.99m, 30),
        new(8, "Webcam HD", "Electronics", 89.99m, 20),
        new(9, "Desk Lamp", "Furniture", 45.99m, 40),
        new(10, "Cable Kit", "Accessories", 29.99m, 100),
    };

    public IActionResult Index() => View();

    /// <summary>
    /// Simple product search demonstrating swap-hidden for state preservation.
    /// Hidden fields are colocated with the content they track.
    /// </summary>
    [HttpGet]
    public IActionResult ProductSearch(
        string? search = null, 
        string sortBy = "name", 
        int page = 1)
    {
        var query = Products.AsEnumerable();
        
        // Filter by search
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        // Sort
        query = sortBy switch
        {
            "price" => query.OrderBy(p => p.Price),
            "stock" => query.OrderBy(p => p.Stock),
            _ => query.OrderBy(p => p.Name)
        };

        // Paginate (5 per page)
        var total = query.Count();
        var totalPages = (int)Math.Ceiling(total / 5.0);
        var items = query.Skip((page - 1) * 5).Take(5).ToList();

        ViewBag.Search = search ?? "";
        ViewBag.SortBy = sortBy;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        return PartialView("_ProductSearch", items);
    }
}

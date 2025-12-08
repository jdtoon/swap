using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.State;
using SwapStateDemo.Models;

namespace SwapStateDemo.Controllers;

public class HomeController : SwapController
{
    private static readonly List<Product> Products =
    [
        new(1, "Laptop", "Electronics", 999.99m),
        new(2, "Mouse", "Electronics", 29.99m),
        new(3, "Keyboard", "Electronics", 79.99m),
        new(4, "Chair", "Furniture", 299.99m),
        new(5, "Desk", "Furniture", 499.99m),
        new(6, "Lamp", "Furniture", 49.99m),
        new(7, "Notebook", "Office", 4.99m),
        new(8, "Pen", "Office", 1.99m),
    ];

    public IActionResult Index()
    {
        var state = new ProductFilterState();
        var (products, totalCount) = FilterProducts(state);
        
        return SwapView(new ProductViewModel
        {
            State = state,
            Products = products,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// TEST 1: Does [FromSwapState] bind the state from hidden fields?
    /// </summary>
    [HttpGet]
    public IActionResult Filter([FromSwapState] ProductFilterState state)
    {
        // Log what we received to prove binding works
        Console.WriteLine($"[FromSwapState] Category={state.Category}, Page={state.Page}, PageSize={state.PageSize}, Search={state.Search}");
        
        var (products, totalCount) = FilterProducts(state);
        
        // TEST 2: Does .WithState() send the OOB update?
        return this.SwapResponse()
            .WithView("_ProductGrid", new ProductViewModel
            {
                State = state,
                Products = products,
                TotalCount = totalCount
            })
            .WithState(state)
            .Build();
    }

    private (List<Product> Items, int TotalCount) FilterProducts(ProductFilterState state)
    {
        var query = Products.AsEnumerable();
        
        if (state.Category != "all")
            query = query.Where(p => p.Category.Equals(state.Category, StringComparison.OrdinalIgnoreCase));
        
        if (!string.IsNullOrEmpty(state.Search))
            query = query.Where(p => p.Name.Contains(state.Search, StringComparison.OrdinalIgnoreCase));
        
        var totalCount = query.Count();
        var items = query.Skip((state.Page - 1) * state.PageSize).Take(state.PageSize).ToList();
        
        return (items, totalCount);
    }
}

using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using SwapExpenses.Events;
using SwapExpenses.Models;
using SwapExpenses.Services;

namespace SwapExpenses.Controllers;

public class ExpenseController : Controller
{
    private readonly ExpenseService _service;

    public ExpenseController(ExpenseService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var expenses = _service.GetAll();
        return this.SwapView(expenses);
    }

    [HttpPost]
    public IActionResult Create(Expense expense)
    {
        if (ModelState.IsValid)
        {
            _service.Add(expense);
            
            // Use the generated event key
            return this.SwapResponse()
                .WithTrigger(TrackerEvents.Expense.Added)
                .WithTrigger(TrackerEvents.Total.Updated)
                .Build();
        }
        
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        return BadRequest(string.Join(", ", errors));
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        _service.Delete(id);
        
        return this.SwapResponse()
            .WithTrigger(TrackerEvents.Expense.Deleted)
            .WithTrigger(TrackerEvents.Total.Updated)
            .Build();
    }
    
    [HttpGet]
    public IActionResult List()
    {
        return PartialView("_ExpenseTable", _service.GetAll());
    }
    
    [HttpGet]
    public IActionResult Total()
    {
        return Content(_service.GetTotal().ToString("C"));
    }
}

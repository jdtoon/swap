using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ECommerceDogfood.Web.Models;

namespace ECommerceDogfood.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// HTMX demo endpoint - returns HTML fragment
    /// </summary>
    [HttpGet]
    public IActionResult GetMessage()
    {
        return Content("✅ HTMX is working! This message was loaded from the server without a full page refresh.");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

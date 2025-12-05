using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace SwapNavDemo.Controllers;

/// <summary>
/// Demo controller showcasing the &lt;swap-nav&gt; tag helper and navigation features.
/// Note: With AutoSuppressLayout = true, we don't need _ViewStart.cshtml!
/// The SwapLayoutFilter automatically returns partial views for HTMX requests.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Main page - full page with shell layout
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    #region Basic Navigation Pages

    /// <summary>
    /// Dashboard page - demonstrates basic swap-nav navigation
    /// </summary>
    [HttpGet]
    public IActionResult Dashboard()
    {
        // With AutoSuppressLayout = true, this returns:
        // - Full view with layout for normal requests (browser refresh)
        // - Partial view without layout for HTMX requests (swap-nav clicks)
        return View("_Dashboard");
    }

    /// <summary>
    /// Products page - demonstrates navigation with query params
    /// </summary>
    [HttpGet]
    public IActionResult Products(string? category = null)
    {
        return View("_Products", category);
    }

    /// <summary>
    /// Orders page
    /// </summary>
    [HttpGet]
    public IActionResult Orders()
    {
        return View("_Orders");
    }

    /// <summary>
    /// Settings page
    /// </summary>
    [HttpGet]
    public IActionResult Settings()
    {
        return View("_Settings");
    }

    #endregion

    #region Navigation with Parameters

    /// <summary>
    /// Search page - demonstrates hx-vals parameter passing
    /// </summary>
    [HttpGet]
    public IActionResult Search(string? q = null)
    {
        return View("_Search", q);
    }

    /// <summary>
    /// Featured products - demonstrates styled swap-nav (class="btn btn-primary")
    /// </summary>
    [HttpGet]
    public IActionResult Featured()
    {
        return View("_Featured");
    }

    /// <summary>
    /// Sale page - demonstrates styled swap-nav (class="btn btn-success")
    /// </summary>
    [HttpGet]
    public IActionResult Sale()
    {
        return View("_Sale");
    }

    #endregion

    #region Advanced Navigation

    /// <summary>
    /// Quick view panel - demonstrates custom hx-target override
    /// </summary>
    [HttpGet]
    public IActionResult QuickView()
    {
        return View("_QuickView");
    }

    /// <summary>
    /// Modal content - demonstrates push-url="false"
    /// </summary>
    [HttpGet]
    public IActionResult ModalContent()
    {
        return View("_ModalContent");
    }

    #endregion
}

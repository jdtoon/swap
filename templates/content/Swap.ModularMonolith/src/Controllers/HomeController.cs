using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace SwapModularMonolith.Controllers;

public class HomeController : SwapController
{
    public IActionResult Index()
    {
        return SwapView();
    }
}

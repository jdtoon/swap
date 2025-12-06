using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace SwapSmallPartials.Controllers;

public class HomeController : SwapController
{
    public IActionResult Index()
    {
        return SwapView();
    }
}

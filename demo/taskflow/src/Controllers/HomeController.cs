using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace TaskFlow.Controllers;

public class HomeController : SwapController
{
    public IActionResult Index()
    {
        return SwapView();
    }
}

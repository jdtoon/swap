using Microsoft.AspNetCore.Mvc;

namespace ModularMonolithDemo.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}

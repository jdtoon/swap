using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swap.Htmx;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapRedirectToActionTests
{
    [Fact]
    public async Task SwapRedirectToAction_InvokesAction_WithConvertedRouteValues()
    {
        var controller = TestController.Create();

        var result = await controller.CallSwapRedirectToAction(nameof(TestController.Details), new { id = "123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(123, ok.Value);
    }

    [Fact]
    public async Task SwapRedirectToAction_Throws_WhenRequiredValueTypeMissing()
    {
        var controller = TestController.Create();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.CallSwapRedirectToAction(nameof(TestController.Details)));

        Assert.Contains("No overload", ex.Message);
    }

    [Fact]
    public async Task SwapRedirectToAction_Throws_WhenActionOverloadIsAmbiguous()
    {
        var controller = TestController.Create();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.CallSwapRedirectToAction(nameof(TestController.Overloaded), new { id = 1, slug = "x" }));

        Assert.Contains("Ambiguous", ex.Message);
    }

    private sealed class TestController : SwapController
    {
        public static TestController Create()
        {
            var controller = new TestController();
            controller.ControllerContext = new ControllerContext
            {
                ActionDescriptor = new ControllerActionDescriptor { ControllerName = nameof(TestController) },
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        public Task<IActionResult> CallSwapRedirectToAction(string actionName, object? routeValues = null)
        {
    #pragma warning disable CS0618 // Intentionally testing obsolete helper
            return SwapRedirectToAction(actionName, routeValues);
    #pragma warning restore CS0618
        }

        public IActionResult Details(int id) => Ok(id);

        public IActionResult Overloaded(int id) => Ok(id);
        public IActionResult Overloaded(string slug) => Ok(slug);
    }
}

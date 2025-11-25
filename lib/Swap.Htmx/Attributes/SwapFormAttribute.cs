using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Results;

namespace Swap.Htmx.Attributes;

/// <summary>
/// Applies automatic model validation for HTMX requests.
/// If ModelState is invalid, returns a SwapValidationErrors response.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SwapFormAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Check if it's an HTMX request
            var isHtmx = context.HttpContext.Request.Headers.ContainsKey("HX-Request");
            if (isHtmx && context.Controller is ControllerBase controller)
            {
                context.Result = controller.SwapValidationErrors(context.ModelState).Build();
            }
        }
    }
}
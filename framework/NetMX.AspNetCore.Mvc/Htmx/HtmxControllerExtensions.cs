using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace NetMX.AspNetCore.Mvc.Htmx;

/// <summary>
/// Extension methods for controllers to work with HTMX responses.
/// </summary>
public static class HtmxControllerExtensions
{
    /// <summary>
    /// Returns a partial view result configured for HTMX.
    /// </summary>
    /// <param name="controller">The controller.</param>
    /// <param name="viewName">The name of the view to render.</param>
    /// <param name="model">The model for the view.</param>
    /// <returns>A <see cref="PartialViewResult"/> configured for HTMX.</returns>
    public static PartialViewResult HtmxPartial(this Controller controller, string viewName, object? model)
    {
        return new PartialViewResult
        {
            ViewName = viewName,
            ViewData = new ViewDataDictionary(controller.ViewData)
            {
                Model = model
            }
        };
    }

    /// <summary>
    /// Adds an HX-Trigger header to trigger an HTMX event.
    /// </summary>
    /// <param name="result">The partial view result.</param>
    /// <param name="eventName">The name of the event to trigger.</param>
    /// <returns>The partial view result with the trigger header.</returns>
    public static PartialViewResult WithTrigger(this PartialViewResult result, string eventName)
    {
        result.ViewData[HtmxResponseHeaders.Trigger] = eventName;
        return result;
    }

    // We can add more fluent methods here later: .WithRetarget("#my-div"), .WithRefresh(), etc.
}
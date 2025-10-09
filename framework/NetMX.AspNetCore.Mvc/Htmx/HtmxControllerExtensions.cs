using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace NetMX.AspNetCore.Mvc.Htmx;

public static class HtmxControllerExtensions
{
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

    public static PartialViewResult WithTrigger(this PartialViewResult result, string eventName)
    {
        result.ViewData[HtmxResponseHeaders.Trigger] = eventName;
        return result;
    }

    // We can add more fluent methods here later: .WithRetarget("#my-div"), .WithRefresh(), etc.
}
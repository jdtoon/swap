using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for handling validation errors in Swap.Htmx.
/// </summary>
public static class SwapValidationExtensions
{
    /// <summary>
    /// Creates a SwapResponseBuilder pre-configured for a validation failure.
    /// Adds a warning toast and a "validationFailed" trigger with error details.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="modelState">The ModelStateDictionary containing validation errors.</param>
    /// <param name="message">Optional toast message. Defaults to "Please correct the errors below."</param>
    /// <returns>A SwapResponseBuilder to chain further responses (like re-rendering the form).</returns>
    public static SwapResponseBuilder SwapValidationErrors(this ControllerBase controller, ModelStateDictionary modelState, string message = "Please correct the errors below.")
    {
        var builder = controller.SwapResponse();
        
        builder.WithWarningToast(message);

        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                k => k.Key,
                v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        builder.WithTrigger("validationFailed", errors);
        
        return builder;
    }
}

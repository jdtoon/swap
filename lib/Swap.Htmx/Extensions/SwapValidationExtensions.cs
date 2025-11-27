using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Swap.Htmx.Models;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for handling validation errors in Swap.Htmx.
/// </summary>
public static class SwapValidationExtensions
{
    /// <summary>
    /// Default ID prefix for validation message elements.
    /// </summary>
    public const string DefaultValidationIdPrefix = "swap-validation-";

    /// <summary>
    /// Creates a SwapResponseBuilder pre-configured for a validation failure.
    /// Adds a warning toast and a "validationFailed" trigger with error details.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="modelState">The ModelStateDictionary containing validation errors.</param>
    /// <param name="message">Optional toast message. Defaults to "Please correct the errors below."</param>
    /// <returns>A SwapResponseBuilder to chain further responses (like re-rendering the form).</returns>
    public static SwapResponseBuilder SwapValidationErrors(
        this ControllerBase controller, 
        ModelStateDictionary modelState, 
        string message = "Please correct the errors below.")
    {
        var builder = controller.SwapResponse();
        return ConfigureValidationResponse(builder, modelState, message);
    }

    /// <summary>
    /// Creates a SwapResponseBuilder pre-configured for a validation failure (Razor Pages).
    /// Adds a warning toast and a "validationFailed" trigger with error details.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    /// <param name="modelState">The ModelStateDictionary containing validation errors.</param>
    /// <param name="message">Optional toast message. Defaults to "Please correct the errors below."</param>
    /// <returns>A SwapResponseBuilder to chain further responses (like re-rendering the form).</returns>
    public static SwapResponseBuilder SwapValidationErrors(
        this PageModel pageModel, 
        ModelStateDictionary modelState, 
        string message = "Please correct the errors below.")
    {
        var builder = pageModel.SwapResponse();
        return ConfigureValidationResponse(builder, modelState, message);
    }

    /// <summary>
    /// Creates a SwapResponseBuilder with OOB swaps for each validation error.
    /// Each error is sent as an OOB swap to update the corresponding swap-validation element.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="modelState">The ModelStateDictionary containing validation errors.</param>
    /// <param name="message">Optional toast message. Defaults to "Please correct the errors below."</param>
    /// <param name="idPrefix">Optional ID prefix for validation elements. Defaults to "swap-validation-".</param>
    /// <returns>A SwapResponseBuilder to chain further responses (like re-rendering the form).</returns>
    /// <remarks>
    /// Use this with &lt;swap-validation for="PropertyName" /&gt; tag helpers in your views.
    /// Each validation error will be sent as an OOB swap to update the corresponding element.
    /// 
    /// Example:
    /// <code>
    /// if (!ModelState.IsValid)
    /// {
    ///     return this.SwapValidationErrorsOob(ModelState)
    ///         .WithView("_Form", model)
    ///         .Build();
    /// }
    /// </code>
    /// </remarks>
    public static SwapResponseBuilder SwapValidationErrorsOob(
        this ControllerBase controller,
        ModelStateDictionary modelState,
        string message = "Please correct the errors below.",
        string idPrefix = DefaultValidationIdPrefix)
    {
        var builder = controller.SwapResponse();
        return ConfigureValidationOobResponse(builder, modelState, message, idPrefix);
    }

    /// <summary>
    /// Creates a SwapResponseBuilder with OOB swaps for each validation error (Razor Pages).
    /// Each error is sent as an OOB swap to update the corresponding swap-validation element.
    /// </summary>
    /// <param name="pageModel">The page model instance.</param>
    /// <param name="modelState">The ModelStateDictionary containing validation errors.</param>
    /// <param name="message">Optional toast message. Defaults to "Please correct the errors below."</param>
    /// <param name="idPrefix">Optional ID prefix for validation elements. Defaults to "swap-validation-".</param>
    /// <returns>A SwapResponseBuilder to chain further responses (like re-rendering the form).</returns>
    public static SwapResponseBuilder SwapValidationErrorsOob(
        this PageModel pageModel,
        ModelStateDictionary modelState,
        string message = "Please correct the errors below.",
        string idPrefix = DefaultValidationIdPrefix)
    {
        var builder = pageModel.SwapResponse();
        return ConfigureValidationOobResponse(builder, modelState, message, idPrefix);
    }

    /// <summary>
    /// Adds OOB swaps for validation errors to an existing builder.
    /// </summary>
    /// <param name="builder">The SwapResponseBuilder to add validation errors to.</param>
    /// <param name="modelState">The ModelStateDictionary containing validation errors.</param>
    /// <param name="idPrefix">Optional ID prefix for validation elements. Defaults to "swap-validation-".</param>
    /// <returns>The builder for chaining.</returns>
    public static SwapResponseBuilder WithValidationErrors(
        this SwapResponseBuilder builder,
        ModelStateDictionary modelState,
        string idPrefix = DefaultValidationIdPrefix)
    {
        var errors = GetValidationErrors(modelState);
        
        foreach (var (fieldName, messages) in errors)
        {
            var targetId = $"{idPrefix}{fieldName}";
            var errorMessage = messages.FirstOrDefault() ?? "";
            
            // Use AlsoUpdateIfExists so missing targets don't cause issues
            builder.AlsoUpdateIfExists(
                targetId,
                "_SwapValidationError",
                new ValidationErrorModel(fieldName, errorMessage, messages));
        }

        builder.WithTrigger("validationFailed", errors);
        
        return builder;
    }

    /// <summary>
    /// Clears validation error displays for specified fields.
    /// Useful when revalidating a field and it passes.
    /// </summary>
    /// <param name="builder">The SwapResponseBuilder.</param>
    /// <param name="fieldNames">The field names to clear validation errors for.</param>
    /// <param name="idPrefix">Optional ID prefix for validation elements.</param>
    /// <returns>The builder for chaining.</returns>
    public static SwapResponseBuilder ClearValidationErrors(
        this SwapResponseBuilder builder,
        IEnumerable<string> fieldNames,
        string idPrefix = DefaultValidationIdPrefix)
    {
        foreach (var fieldName in fieldNames)
        {
            var targetId = $"{idPrefix}{fieldName}";
            builder.AlsoUpdateIfExists(targetId, "_SwapValidationClear", fieldName);
        }
        
        return builder;
    }

    private static SwapResponseBuilder ConfigureValidationResponse(
        SwapResponseBuilder builder,
        ModelStateDictionary modelState,
        string message)
    {
        builder.WithWarningToast(message);

        var errors = GetValidationErrors(modelState);
        builder.WithTrigger("validationFailed", errors);
        
        return builder;
    }

    private static SwapResponseBuilder ConfigureValidationOobResponse(
        SwapResponseBuilder builder,
        ModelStateDictionary modelState,
        string message,
        string idPrefix)
    {
        builder.WithWarningToast(message);
        builder.WithValidationErrors(modelState, idPrefix);
        
        return builder;
    }

    private static Dictionary<string, string[]> GetValidationErrors(ModelStateDictionary modelState)
    {
        return modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                k => k.Key,
                v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }
}

/// <summary>
/// Model for rendering validation error partial views.
/// </summary>
public record ValidationErrorModel(string FieldName, string Message, string[] AllMessages);

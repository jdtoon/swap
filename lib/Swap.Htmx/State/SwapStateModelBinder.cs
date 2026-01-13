using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace Swap.Htmx.State;

/// <summary>
/// Model binder that binds SwapState-derived classes from form/query data.
/// </summary>
public sealed class SwapStateModelBinder : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelType = bindingContext.ModelType;
        
        if (!typeof(SwapState).IsAssignableFrom(modelType))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        // Create instance of the state class
        var state = (SwapState?)Activator.CreateInstance(modelType);
        if (state == null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        // Get prefix from attribute if specified
        var prefix = GetPrefix(bindingContext);

        // Collect values from form and query
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var valueProvider = bindingContext.ValueProvider;
        var protectionProvider = bindingContext.HttpContext.RequestServices.GetService<IDataProtectionProvider>();

        // Get all bindable properties
        var properties = modelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.Name != nameof(SwapState.ContainerId) 
                     && p.Name != nameof(SwapState.ChangedProperties)
                     && p.Name != nameof(SwapState.HasChanges));

        foreach (var prop in properties)
        {
            var fieldName = string.IsNullOrEmpty(prefix) 
                ? prop.Name 
                : $"{prefix}.{prop.Name}";

            var result = valueProvider.GetValue(fieldName);
            
            if (result != ValueProviderResult.None && result.Values.Count > 0)
            {
                // Use FIRST value when duplicates exist (URL params come first, then hx-include)
                // This ensures explicit URL overrides take precedence, even if empty
                var firstValue = result.FirstValue;

                if (protectionProvider != null && !string.IsNullOrEmpty(firstValue) && SwapStateRenderer.IsPropertyProtected(state, prop.Name))
                {
                    try
                    {
                        var protector = protectionProvider.CreateProtector("SwapState", state.ContainerId, prop.Name);
                        firstValue = protector.Unprotect(firstValue);
                    }
                    catch
                    {
                        // INVALID STATE - Tampering detected
                        // We fail binding for this property or even the whole model
                        // For now, let's treat it as null/invalid and NOT bind it
                        // Optional: Add model error
                        // bindingContext.ModelState.TryAddModelError(fieldName, "Invalid state signature.");
                        continue;
                    }
                }

                values[prop.Name] = firstValue;
            }
        }

        // Apply values to state object
        if (values.Count > 0)
        {
            state.SetStateValues(values);
        }

        // Also read from query string if UrlSync is enabled
        // Note: FromQueryString now handles its own decryption via provider
        if (state.UrlSync)
        {
            state.FromQueryString(bindingContext.HttpContext.Request.Query, protectionProvider);
        }

        // Clear change tracking since this is initial load
        state.AcceptChanges();

        bindingContext.Result = ModelBindingResult.Success(state);
        return Task.CompletedTask;
    }

    private static string? GetPrefix(ModelBindingContext bindingContext)
    {
        // Check for prefix in FromSwapStateAttribute
        var parameter = bindingContext.ActionContext.ActionDescriptor.Parameters
            .FirstOrDefault(p => p.Name == bindingContext.FieldName);

        if (parameter?.BindingInfo?.BinderType == typeof(SwapStateModelBinder))
        {
            // Try to get attribute from method parameter
            var controllerType = bindingContext.ActionContext.ActionDescriptor
                .GetType().GetProperty("ControllerTypeInfo")?
                .GetValue(bindingContext.ActionContext.ActionDescriptor) as TypeInfo;

            // For simplicity, use the model name as prefix if present
            return string.IsNullOrEmpty(bindingContext.ModelName) ? null : bindingContext.ModelName;
        }

        return null;
    }
}

/// <summary>
/// Model binder provider for SwapState-derived types.
/// </summary>
public sealed class SwapStateModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (typeof(SwapState).IsAssignableFrom(context.Metadata.ModelType))
        {
            return new SwapStateModelBinder();
        }

        return null;
    }
}

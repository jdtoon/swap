using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SwapSmallPartials.Infrastructure;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (bindingContext.ModelType == typeof(decimal?))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }

        // Try parsing with invariant culture first, then current culture
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue) ||
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out decimalValue))
        {
            bindingContext.Result = ModelBindingResult.Success(decimalValue);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid decimal value.");
        }

        return Task.CompletedTask;
    }
}

public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
        {
            return new InvariantDecimalModelBinder();
        }

        return null;
    }
}

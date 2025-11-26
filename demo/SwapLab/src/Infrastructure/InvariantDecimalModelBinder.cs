using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SwapLab.Infrastructure;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var targetType = bindingContext.ModelType;
        if (targetType != typeof(decimal) && targetType != typeof(decimal?))
        {
            return Task.CompletedTask;
        }

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (targetType == typeof(decimal?))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            return Task.CompletedTask;
        }

        // Try multiple parsing strategies: current culture, invariant, comma/dot normalization
        if (TryParseDecimal(value, CultureInfo.CurrentCulture, out var result) ||
            TryParseDecimal(value, CultureInfo.InvariantCulture, out result) ||
            TryParseDecimal(value.Replace(',', '.'), CultureInfo.InvariantCulture, out result) ||
            TryParseDecimal(value.Replace('.', ','), new CultureInfo("fr-FR"), out result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
            $"The value '{value}' is not valid for {bindingContext.ModelMetadata.DisplayName ?? bindingContext.ModelName}.");
        return Task.CompletedTask;
    }

    private static bool TryParseDecimal(string input, CultureInfo culture, out decimal value)
    {
        return decimal.TryParse(input, NumberStyles.Number, culture, out value);
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

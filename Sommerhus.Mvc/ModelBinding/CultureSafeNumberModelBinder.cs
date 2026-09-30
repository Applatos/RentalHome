using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sommerhus.Mvc.ModelBinding;

/// <summary>
/// Binds decimal, double and float values (nullable or not) from form, query and route values
/// with <see cref="CultureSafeNumber"/>, so "800.5" posted under da-DK stays 800.5.
/// </summary>
/// <remarks>Mirrors MVC's own <c>DecimalModelBinder</c>: same model-state entries and messages.</remarks>
public sealed class CultureSafeNumberModelBinder(Type numberType) : IModelBinder
{
    private readonly Type numberType = CultureSafeNumber.IsSupported(numberType)
        ? numberType
        : throw new ArgumentException($"{numberType} is not a supported number type.", nameof(numberType));

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            // An empty field is "no value": fine for decimal?, an error for decimal.
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    modelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                        valueProviderResult.ToString()));
            }

            return Task.CompletedTask;
        }

        // The current culture, not valueProviderResult.Culture: query and route values report the
        // invariant culture, and the rule is the same wherever the value comes from.
        if (CultureSafeNumber.TryParse(value, numberType, CultureInfo.CurrentCulture, out var model))
        {
            bindingContext.Result = ModelBindingResult.Success(model);
        }
        else
        {
            // A FormatException yields MVC's usual "The value 'x' is not valid for Y." message.
            bindingContext.ModelState.TryAddModelError(modelName, new FormatException(), bindingContext.ModelMetadata);
        }

        return Task.CompletedTask;
    }
}

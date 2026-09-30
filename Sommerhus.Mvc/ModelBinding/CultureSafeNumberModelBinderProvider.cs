using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sommerhus.Mvc.ModelBinding;

/// <summary>
/// Supplies <see cref="CultureSafeNumberModelBinder"/> for decimal, double and float values read
/// from form, query or route values. Register it first so it wins over MVC's own number binders.
/// </summary>
public sealed class CultureSafeNumberModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var type = context.Metadata.UnderlyingOrModelType;
        if (!CultureSafeNumber.IsSupported(type) || !ReadsValueProviders(context.BindingInfo.BindingSource))
            return null;

        return new CultureSafeNumberModelBinder(type);
    }

    // No binding source means the default form/query/route lookup. Body, header and service
    // bindings keep their own binders.
    private static bool ReadsValueProviders(BindingSource? source)
        => source is null
           || source == BindingSource.ModelBinding
           || source == BindingSource.Form
           || source == BindingSource.Query
           || source == BindingSource.Path;
}

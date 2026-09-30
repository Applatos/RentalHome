using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Sommerhus.Mvc.ModelBinding;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>
/// The binder against the value providers MVC gives it: form values carry the request culture,
/// query and route values the invariant culture.
/// </summary>
public sealed class CultureSafeNumberModelBinderTests
{
    private const string Name = "price";
    private static readonly IModelMetadataProvider Metadata = new EmptyModelMetadataProvider();

    public enum Source { Form, Query, Route }

    [Theory]
    [InlineData(Source.Form, "da-DK", "800.5", 800.5)]
    [InlineData(Source.Form, "da-DK", "1250.00", 1250)]
    [InlineData(Source.Form, "da-DK", "800,5", 800.5)]
    [InlineData(Source.Form, "da-DK", "1.250,50", 1250.5)]
    [InlineData(Source.Form, "en-GB", "800.5", 800.5)]
    [InlineData(Source.Form, "en-GB", "800,5", 800.5)]
    [InlineData(Source.Form, "en-GB", "1,250.50", 1250.5)]
    [InlineData(Source.Query, "da-DK", "700.5", 700.5)]
    [InlineData(Source.Query, "da-DK", "700,5", 700.5)]
    [InlineData(Source.Query, "en-GB", "700.5", 700.5)]
    [InlineData(Source.Route, "da-DK", "12.5", 12.5)]
    public async Task Decimal_BindsTheNumberThatWasMeant(Source source, string culture, string raw, double expected)
    {
        var (result, state) = await BindInCultureAsync(typeof(decimal), source, culture, raw);

        result.IsModelSet.Should().BeTrue();
        result.Model.Should().Be((decimal)expected);
        state.ErrorCount.Should().Be(0);
        state[Name]!.AttemptedValue.Should().Be(raw);
    }

    [Theory]
    [InlineData(typeof(decimal?), "da-DK", "800.5", 800.5)]
    [InlineData(typeof(double), "da-DK", "800.5", 800.5)]
    [InlineData(typeof(double?), "en-GB", "800,5", 800.5)]
    [InlineData(typeof(float), "da-DK", "0.5", 0.5)]
    [InlineData(typeof(float?), "en-GB", "0,5", 0.5)]
    public async Task OtherFloatingPointTypes_BindTheSameWay(Type type, string culture, string raw, double expected)
    {
        var (result, state) = await BindInCultureAsync(type, Source.Form, culture, raw);

        result.IsModelSet.Should().BeTrue();
        Convert.ToDouble(result.Model, CultureInfo.InvariantCulture).Should().Be(expected);
        result.Model!.GetType().Should().Be(Nullable.GetUnderlyingType(type) ?? type);
        state.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task EmptyValue_IsNullForNullable_AndAnErrorOtherwise()
    {
        var (nullable, nullableState) = await BindInCultureAsync(typeof(decimal?), Source.Form, "da-DK", "");
        var (required, requiredState) = await BindInCultureAsync(typeof(decimal), Source.Form, "da-DK", "");

        nullable.IsModelSet.Should().BeTrue();
        nullable.Model.Should().BeNull();
        nullableState.ErrorCount.Should().Be(0);

        required.IsModelSet.Should().BeFalse();
        requiredState[Name]!.Errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,250.50")]
    public async Task InvalidValue_IsAModelError_WithMvcsUsualMessage(string raw)
    {
        var (result, state) = await BindInCultureAsync(typeof(decimal), Source.Form, "da-DK", raw);

        result.IsModelSet.Should().BeFalse();
        state[Name]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be($"The value '{raw}' is not valid.");
    }

    [Fact]
    public async Task MissingValue_LeavesTheModelUnbound()
    {
        var context = CreateContext(typeof(decimal), new QueryStringValueProvider(
            BindingSource.Query, new QueryCollection(), CultureInfo.InvariantCulture));

        await new CultureSafeNumberModelBinder(typeof(decimal)).BindModelAsync(context);

        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_RejectsNonFloatingPointTypes()
    {
        var act = () => new CultureSafeNumberModelBinder(typeof(int));

        act.Should().Throw<ArgumentException>();
    }

    private static async Task<(ModelBindingResult Result, ModelStateDictionary State)> BindInCultureAsync(
        Type type, Source source, string culture, string raw)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            // What the request localization middleware does for every request.
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var values = new Dictionary<string, StringValues> { [Name] = raw };
            IValueProvider provider = source switch
            {
                Source.Form => new FormValueProvider(BindingSource.Form, new FormCollection(values), CultureInfo.CurrentCulture),
                Source.Query => new QueryStringValueProvider(BindingSource.Query, new QueryCollection(values), CultureInfo.InvariantCulture),
                _ => new RouteValueProvider(BindingSource.Path, new RouteValueDictionary { [Name] = raw })
            };

            var context = CreateContext(type, provider);
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            await new CultureSafeNumberModelBinder(underlying).BindModelAsync(context);
            return (context.Result, context.ModelState);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static ModelBindingContext CreateContext(Type type, IValueProvider provider)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        return DefaultModelBindingContext.CreateBindingContext(
            actionContext, provider, Metadata.GetMetadataForType(type), bindingInfo: null, modelName: Name);
    }
}

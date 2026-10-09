using Microsoft.Extensions.Localization;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>
/// Localized labels for the enum values people see (booking status, house status, image kind and
/// so on). The text lives in the resource files under <c>Enums.{EnumType}.{Value}</c>, for example
/// <c>Enums.BookingStatus.Pending</c>. A value without an entry shows its own name, so a new enum
/// member never turns into a bare resource key on the page.
/// </summary>
public static class EnumLabels
{
    public static string Label<TEnum>(this IStringLocalizer localizer, TEnum value)
        where TEnum : struct, Enum
    {
        var text = localizer[$"Enums.{typeof(TEnum).Name}.{value}"];
        return text.ResourceNotFound ? value.ToString() : text.Value;
    }

    /// <summary>
    /// The label for an enum value the API sends as text, such as an image kind or an audit action.
    /// Text that names no member (or nothing at all) is returned as it is.
    /// </summary>
    public static string LabelOrText<TEnum>(this IStringLocalizer localizer, string? text)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) && Enum.IsDefined(value)
            ? localizer.Label(value)
            : text ?? string.Empty;

    /// <summary>The label for a role name from <c>AppRoles</c>; a role without an entry shows its own name.</summary>
    public static string RoleLabel(this IStringLocalizer localizer, string? role)
    {
        var text = localizer[$"Enums.Role.{role}"];
        return string.IsNullOrWhiteSpace(role) || text.ResourceNotFound ? role ?? string.Empty : text.Value;
    }
}

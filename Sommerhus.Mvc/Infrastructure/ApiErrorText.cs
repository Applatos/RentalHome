using System.Text.Json;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>
/// Readable text for a failed admin API call. Admin pages show the API's own validation messages,
/// but never a raw response body: a problem document is reduced to its detail or title, and
/// anything else that is not plain text falls back to the caller's message.
/// </summary>
public static class ApiErrorText
{
    private const int MaxPlainMessageLength = 300;

    public static string Describe<T>(ApiResponse<T> response, string fallback)
    {
        if (response.HasValidationErrors)
        {
            var messages = response.Errors
                .SelectMany(e => e.Value ?? [])
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (messages.Count > 0)
            {
                return string.Join(" ", messages);
            }
        }

        var message = response.Message?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            return fallback;
        }

        if (message.StartsWith('{'))
        {
            return FromProblemDocument(message) ?? fallback;
        }

        return message.StartsWith('<') || message.Length > MaxPlainMessageLength ? fallback : message;
    }

    private static string? FromProblemDocument(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            foreach (var name in new[] { "detail", "title" })
            {
                if (document.RootElement.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    // The API joins several messages with line breaks.
                    return value.GetString()!.Trim().ReplaceLineEndings(" ");
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON after all; the caller falls back to its own text.
        }

        return null;
    }
}

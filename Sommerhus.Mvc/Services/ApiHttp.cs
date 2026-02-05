using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace Sommerhus.Mvc.Services;

internal static class ApiHttp
{
    internal static Task<ApiResponse<T?>> GetAsync<T>(HttpClient http, string uri, CancellationToken ct)
        => SendAsync<T>(http, (client, token) => client.GetAsync(uri, token), ct);

    internal static Task<ApiResponse<TResponse?>> PostAsync<TRequest, TResponse>(HttpClient http, string uri, TRequest payload, CancellationToken ct)
        => SendAsync<TResponse>(http, (client, token) => client.PostAsJsonAsync(uri, payload, token), ct);

    internal static Task<ApiResponse<TResponse?>> PutAsync<TRequest, TResponse>(HttpClient http, string uri, TRequest payload, CancellationToken ct)
        => SendAsync<TResponse>(http, (client, token) => client.PutAsJsonAsync(uri, payload, token), ct);

    internal static Task<ApiResponse<object?>> DeleteAsync(HttpClient http, string uri, CancellationToken ct)
        => SendAsync<object>(http, (client, token) => client.DeleteAsync(uri, token), ct);

    internal static async Task<ApiResponse<T?>> SendAsync<T>(HttpClient http, Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> sender, CancellationToken ct)
    {
        try
        {
            using var response = await sender(http, ct);
            return await HandleResponseAsync<T>(response, ct);
        }
        catch (TaskCanceledException ex)
        {
            return ApiResponse<T?>.Failure(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T?>.Failure($"Network error: {ex.Message}");
        }
    }

    private static async Task<ApiResponse<T?>> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return ApiResponse<T?>.Success(default, response.StatusCode);
        }

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return ApiResponse<T?>.Success(payload, response.StatusCode);
            }
            catch (Exception ex)
            {
                return ApiResponse<T?>.Failure($"Parse error: {ex.Message}", response.StatusCode);
            }
        }


        // Handle validation/problem payloads robustly (ValidationProblemDetails, ProblemDetails, or raw dictionary)
        if (response.StatusCode == HttpStatusCode.BadRequest || (int)response.StatusCode == 422)
        {
            // Try ValidationProblemDetails first (has .Errors)
            try
            {
                var validation = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: ct);
                if (validation is not null && validation.Errors is not null && validation.Errors.Count > 0)
                {
                    // copy to IReadOnlyDictionary<string,string[]>
                    return ApiResponse<T?>.Validation((IReadOnlyDictionary<string, string[]>?)validation.Errors, response.StatusCode);
                }

                // If there are no 'errors' property, fall through to try other shapes
            }
            catch
            {
                // ignore and try other formats
            }

            // Try plain dictionary format { field: ["msg"] }
            try
            {
                var errors = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>(cancellationToken: ct);
                if (errors is not null)
                {
                    return ApiResponse<T?>.Validation(errors, response.StatusCode);
                }
            }
            catch
            {
                // ignore and try ProblemDetails
            }

            // Try ProblemDetails (has title/detail)
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
                if (problem is not null)
                {
                    var msg = !string.IsNullOrWhiteSpace(problem.Title) ? problem.Title
                            : !string.IsNullOrWhiteSpace(problem.Detail) ? problem.Detail
                            : response.ReasonPhrase ?? "Bad request";
                    return ApiResponse<T?>.Failure(msg, response.StatusCode);
                }
            }
            catch
            {
                // ignore
            }

            // Fallback: raw text
            var text = await response.Content.ReadAsStringAsync(ct);
            return ApiResponse<T?>.Failure(string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase ?? "Bad request" : text, response.StatusCode);
        }
        var message = await response.Content.ReadAsStringAsync(ct);
        return ApiResponse<T?>.Failure(string.IsNullOrWhiteSpace(message) ? response.ReasonPhrase ?? "Request failed" : message, response.StatusCode);
    }
}

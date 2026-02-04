using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Infrastructure;

public static class HttpResponseMessageExtensions
{
    public static async Task<string> DumpIfError(this HttpResponseMessage response, ITestOutputHelper output)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            output.WriteLine("===== RAW API RESPONSE =====");
            output.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
            output.WriteLine("Headers: " + response.Content?.Headers?.ToString());
            output.WriteLine(body);
            output.WriteLine("============================");
        }
        return body;
    }

    public static async Task<T?> ReadJsonOrDump<T>(this HttpResponseMessage response, ITestOutputHelper output)
    {
        var body = await response.DumpIfError(output);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch
        {
            output.WriteLine($"⚠️ Could not parse JSON as {typeof(T).Name}");
            return default;
        }
    }

    public static async Task<ProblemDetails?> ReadProblem(this HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<ProblemDetails>();
}

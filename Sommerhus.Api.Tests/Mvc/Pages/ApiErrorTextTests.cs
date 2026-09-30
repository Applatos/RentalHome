using System.Net;
using FluentAssertions;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class ApiErrorTextTests
{
    private const string Fallback = "Could not save prices.";

    [Fact]
    public void Describe_ValidationErrors_JoinsTheApiMessages()
    {
        var response = ApiResponse<object?>.Validation(new Dictionary<string, string[]>
        {
            ["SeasonPrices[0].NightlyPrice"] = ["Price must be greater than zero."],
            ["code"] = ["Duplicate season code.", "Price must be greater than zero."]
        }, HttpStatusCode.BadRequest);

        ApiErrorText.Describe(response, Fallback).Should().Be("Price must be greater than zero. Duplicate season code.");
    }

    [Fact]
    public void Describe_ProblemDocument_ShowsItsDetailNotTheJson()
    {
        var response = ApiResponse<object?>.Failure(
            "{\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.5.10\",\"title\":\"Conflict\",\"status\":409,\"detail\":\"spans: Season spans overlap.\\nspans: Check the dates.\"}",
            HttpStatusCode.Conflict);

        ApiErrorText.Describe(response, Fallback).Should().Be("spans: Season spans overlap. spans: Check the dates.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<!DOCTYPE html><html><body>Internal Server Error</body></html>")]
    [InlineData("{not json")]
    public void Describe_NothingReadable_UsesTheFallback(string? message)
    {
        var response = message is null
            ? ApiResponse<object?>.Validation(null, HttpStatusCode.BadRequest)
            : ApiResponse<object?>.Failure(message, HttpStatusCode.InternalServerError);

        ApiErrorText.Describe(response, Fallback).Should().Be(Fallback);
    }

    [Fact]
    public void Describe_PlainMessage_IsShown()
        => ApiErrorText.Describe(ApiResponse<object?>.Failure("Network error: connection refused"), Fallback)
            .Should().Be("Network error: connection refused");
}

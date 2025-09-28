using System.Net;

namespace Sommerhus.Mvc.Services;

public sealed class ApiResult<T>
{
    private ApiResult(bool ok, T? payload, IReadOnlyDictionary<string, string[]>? errors, string? errorMessage, HttpStatusCode statusCode)
    {
        Ok = ok;
        Payload = payload;
        Errors = errors;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    public bool Ok { get; }
    public T? Payload { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
    public string? ErrorMessage { get; }
    public HttpStatusCode StatusCode { get; }
    public bool HasValidationErrors => Errors is { Count: > 0 };

    public static ApiResult<T> Success(T? payload, HttpStatusCode statusCode)
        => new(true, payload, null, null, statusCode);

    public static ApiResult<T> Validation(HttpStatusCode statusCode, IReadOnlyDictionary<string, string[]> errors)
        => new(false, default, errors, null, statusCode);

    public static ApiResult<T> Failure(HttpStatusCode statusCode, string message)
        => new(false, default, null, message, statusCode);
}

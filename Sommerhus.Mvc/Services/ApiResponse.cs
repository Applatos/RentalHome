using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Sommerhus.Mvc.Services;

public sealed class ApiResponse<T>
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

    private ApiResponse() { }

    public T? Data { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public bool Ok { get; init; }
    public string? Message { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = EmptyErrors;
    public bool HasValidationErrors => Errors.Count > 0;

    public static ApiResponse<T> Success(T? data, HttpStatusCode? status = null)
        => new() { Ok = true, Data = data, StatusCode = status };

    public static ApiResponse<T> Validation(IReadOnlyDictionary<string, string[]>? errors, HttpStatusCode? status = null)
        => new()
        {
            Ok = false,
            Errors = errors is null || errors.Count == 0 ? EmptyErrors : errors,
            StatusCode = status
        };

    public static ApiResponse<T> Failure(string message, HttpStatusCode? status = null)
        => new() { Ok = false, Message = message, StatusCode = status };
}

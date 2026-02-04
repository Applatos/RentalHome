using System.Collections.ObjectModel;

namespace Sommerhus.Core.Common;

public enum ServiceResultStatus
{
    Success,
    NotFound,
    Invalid,
    Conflict,
    Unavailable
}

public class ServiceResult
{
    public static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal));

    protected ServiceResult(ServiceResultStatus status, IReadOnlyDictionary<string, string[]> errors)
    {
        Status = status;
        Errors = errors;
    }

    public ServiceResultStatus Status { get; }

    public bool IsSuccess => Status == ServiceResultStatus.Success;

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static ServiceResult Success() => new(ServiceResultStatus.Success, EmptyErrors);

    public static ServiceResult NotFound() => new(ServiceResultStatus.NotFound, EmptyErrors);

    public static ServiceResult Invalid(string field, string message)
        => Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = new[] { message }
        });

    public static ServiceResult Invalid(IDictionary<string, string[]> errors)
    {
        var copy = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            copy[pair.Key] = pair.Value?.ToArray() ?? Array.Empty<string>();
        }

        return new(ServiceResultStatus.Invalid, new ReadOnlyDictionary<string, string[]>(copy));
    }

    public static ServiceResult Conflict(string field, string message)
        => new(ServiceResultStatus.Conflict, new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = new[] { message }
        }));

    public static ServiceResult Unavailable(string message)
        => new(ServiceResultStatus.Unavailable, new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [string.Empty] = new[] { message }
        }));
}

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(ServiceResultStatus status, T? value, IReadOnlyDictionary<string, string[]> errors)
        : base(status, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ServiceResult<T> Success(T value)
        => new(ServiceResultStatus.Success, value, EmptyErrors);

    public new static ServiceResult<T> NotFound()
        => new(ServiceResultStatus.NotFound, default, EmptyErrors);

    public new static ServiceResult<T> Invalid(string field, string message)
        => Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = new[] { message }
        });

    public new static ServiceResult<T> Invalid(IDictionary<string, string[]> errors)
    {
        var copy = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            copy[pair.Key] = pair.Value?.ToArray() ?? Array.Empty<string>();
        }

        return new(ServiceResultStatus.Invalid, default, new ReadOnlyDictionary<string, string[]>(copy));
    }

    public new static ServiceResult<T> Conflict(string field, string message)
        => new(ServiceResultStatus.Conflict, default, new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = new[] { message }
        }));

    public new static ServiceResult<T> Unavailable(string message)
        => new(ServiceResultStatus.Unavailable, default, new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [string.Empty] = new[] { message }
        }));
}

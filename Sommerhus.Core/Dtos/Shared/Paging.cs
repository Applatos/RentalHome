namespace Sommerhus.Core.Dtos.Shared;

public sealed record PageResult<T>
{
    public string? Query { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<T> Items { get; init; } = [];
}

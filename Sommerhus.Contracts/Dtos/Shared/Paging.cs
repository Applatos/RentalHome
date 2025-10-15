namespace Sommerhus.Contracts.Dtos.Shared;

public class PageResult<T>
{
    public string? Query { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public List<T> Items { get; init; } = new();
}

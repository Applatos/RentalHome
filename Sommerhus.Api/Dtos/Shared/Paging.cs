namespace Sommerhus.Api.Dtos.Shared;

public record PageRequest(int Page = 1, int PageSize = 20, string? Query = null);

public class PageResult<T>
{
    public string? Query { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public List<T> Items { get; init; } = new();
}

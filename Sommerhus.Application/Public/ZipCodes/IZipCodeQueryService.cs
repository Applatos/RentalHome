namespace Sommerhus.Application.Public.ZipCodes;

public interface IZipCodeQueryService
{
    Task<IEnumerable<string>> FindAsync(string query, CancellationToken ct);
}

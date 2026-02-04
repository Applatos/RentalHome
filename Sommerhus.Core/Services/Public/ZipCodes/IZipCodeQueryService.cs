namespace Sommerhus.Core.Services.Public.ZipCodes;

public interface IZipCodeQueryService
{
    Task<IEnumerable<string>> FindAsync(string query, CancellationToken ct);
}

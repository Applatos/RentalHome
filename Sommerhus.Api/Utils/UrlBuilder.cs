namespace Sommerhus.Api.Utils;

public static class UrlBuilder
{
    public static string AreaImageWebPath(Guid areaId, string fileName)
        => $"/uploads/areas/{areaId}/{fileName}";

    public static string HouseImageWebPath(Guid houseId, string fileName)
        => $"/uploads/houses/{houseId}/{fileName}";

    public static string ToAbsolute(HttpRequest req, string relative)
        => $"{req.Scheme}://{req.Host}{req.PathBase}{relative}";
}
